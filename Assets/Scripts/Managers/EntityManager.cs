using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class EntityManager : MonoBehaviour
{
    static public EntityManager Instance { private set; get; }

    private enum SpawnKind { Enemy, Item }

    [Header("Data Setting")]
    [SerializeField] private GameObject enemyBase;
    [SerializeField] private GameObject itemBase;
    [SerializeField] private ItemData[] itemDatas;
    readonly private Dictionary<int, ItemData> itemDic = new Dictionary<int, ItemData>();
    readonly private Dictionary<string, System.Type> itemTypeDic = new Dictionary<string, System.Type>();

    [Header("Spawn Settings")]
    [SerializeField] private int eMinCount = 1;
    [SerializeField] private int eMaxCount = 1;
    [SerializeField][Min(0.05f)] private float eDelay = 5;
    [SerializeField][Min(0.05f)] private float eMinDelay = 0.05f;
    [SerializeField][Min(0.05f)] private float iDelay = 10f;
    [SerializeField][Min(0.05f)] private float iMinDelay = 3f;
    private float eBaseDelay;
    private float iDelayBase;
    private Coroutine spawnRoutine;

    [Header("Entities")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemyTrans;
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();
    [SerializeField] private Transform itemTrans;
    [SerializeField] private List<Item> items = new List<Item>();

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void ResetReact();
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (enemyBase == null)
            enemyBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyBase.prefab");
        if (itemBase == null)
            itemBase = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ItemBase.prefab");

        string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { "Assets/Scripts/ScriptableObjects" });
        var list = new List<ItemData>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (data != null) list.Add(data.Clone());
        }
        itemDatas = list.OrderBy(d => d.ID).ThenBy(d => d.Name).ToArray();
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetItemDic();
        SetEntity();
    }

    #region 적
    public int CalcEnemyCount()
    {
        int n = GameManager.Instance.GetScore() / 1000;
        eMinCount = 1 + n;
        eMaxCount = 1 + 2 * n;
        return Random.Range(eMinCount, eMaxCount + 1);
    }

    public Enemy SpawnEnemy(Vector3? _pos = null)
    {
        Vector3 pos = SpawnPos(SpawnKind.Enemy, _pos);

        Enemy e = Instantiate(enemyBase, pos, Quaternion.identity, enemyTrans)
            .GetComponent<Enemy>();

        enemies.Add(e);

        return e;
    }
    #endregion

    #region 아이템
    public ItemData SearchItem(int _id) => itemDic.TryGetValue(_id, out var _data) ? _data : null;
    public ItemData RandomItem()
    {
        ItemData pick = null;
        int k = 0;

        for (int i = 0; i < itemDatas.Length; i++)
        {
            var d = itemDatas[i];
            if (d != null && d.Level <= GameManager.Instance.GetLevel())
                if (Random.Range(0, ++k) == 0)
                    pick = d;
        }

        return pick;
    }

    public Item SpawnItem(int _id = 0, Vector3? _pos = null)
    {
        ItemData data = (_id == 0)
            ? RandomItem()
            : SearchItem(_id);
        if (data == null) return null;

        Vector3 pos = SpawnPos(SpawnKind.Item, _pos);

        var go = Instantiate(itemBase, pos, Quaternion.identity, itemTrans);

        itemTypeDic.TryGetValue(data.Name, out var _t);
        Item i = _t != null ? (Item)go.AddComponent(_t) : go.AddComponent<Item>();
        i.SetData(data.Clone());
        items.Add(i);

        return i;
    }

    private void ActWithReward(System.Action _act)
    {
        if (ADManager.Instance != null) ADManager.Instance?.ShowReward(_act);
        else _act?.Invoke();
    }

    public void Reset()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ResetReact();
#else
        ActWithReward(ResetItems);
#endif
    }
    private void ResetItems()
    {
        foreach (var item in itemDatas)
            item.ResetStat(true);
    }
    #endregion

    #region 공통
    private Vector3 SpawnPos(SpawnKind _kind, Vector3? _pos)
    {
        if (_pos.HasValue) return _pos.Value;

        Rect r = AutoCamera.WorldRect;

        float xMin = r.xMin, xMax = r.xMax;
        float yMin = r.yMin, yMax = r.yMax, yMid = r.center.y;

        bool enemy = _kind == SpawnKind.Enemy;
        int edge = Random.Range(0, enemy ? 3 : 4);
        float x = Random.Range(xMin, xMax);
        float y = enemy ? Random.Range(yMid, yMax) : Random.Range(yMin, yMax);

        if (enemy)
            return edge == 0 ? new Vector3(x, yMax)
                 : edge == 1 ? new Vector3(xMin, y)
                 : new Vector3(xMax, y);

        float p = 1.5f;
        float ix = Random.Range(xMin + p, xMax - p);
        float iy = Random.Range(yMin + p, yMax - p);

        return edge == 0 ? new Vector3(ix, yMax) + Vector3.down
             : edge == 1 ? new Vector3(ix, yMin) + Vector3.up
             : edge == 2 ? new Vector3(xMin, iy) + Vector3.right
             : new Vector3(xMax, iy) + Vector3.left;
    }

    public void ToggleSpawn(bool _on)
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        if (_on)
            spawnRoutine = StartCoroutine(SpawnCoroutine());
        else
            spawnRoutine = null;
    }

    private IEnumerator SpawnCoroutine()
    {
        float eTimer = eDelay;
        float iTimer = iDelay;

        while (true)
        {
            float dt = Time.deltaTime;
            eTimer += dt;
            iTimer += dt;

            eDelay = Mathf.Max(eDelay - dt / 50f, eMinDelay);
            iDelay = Mathf.Max(iDelay - dt / 35f, iMinDelay);

            int cnt = 0;
            int count = CalcEnemyCount();
            while (eTimer >= eDelay && cnt++ < 4)
            {
                for (int i = 0; i < count; i++)
                {
                    var enemy = SpawnEnemy();
                    if (enemy == null)
                    {
                        eTimer = Mathf.Min(eTimer, eDelay);
                        break;
                    }
                    eTimer -= eDelay;
                    yield return new WaitForSeconds(0.01f);
                }
            }

            cnt = 0;
            while (iTimer >= iDelay && cnt++ < 4)
            {
                // 임시
                //var item = SpawnItem();
                var item = SpawnItem(0, player.transform.position);
                if (item == null)
                {
                    iTimer = Mathf.Min(iTimer, iDelay);
                    break;
                }
                iTimer -= iDelay;
                yield return new WaitForSeconds(0.01f);
            }

            yield return null;
        }
    }
    #endregion

    #region 제거
    public void RemoveEnemy(Enemy _enemy)
    {
        if (_enemy == null) return;

        enemies.Remove(_enemy);
        Destroy(_enemy.gameObject);
    }

    public void RemoveItem(Item _item, float _duration = 0f, bool _instant = false)
    {
        if (_item == null) return;

        if (_instant)
        {
            if (Instance != null) Instance.items.Remove(_item);
            Destroy(_item.gameObject);
            return;
        }

        StartCoroutine(RemoveCoroutine(_item, _duration));
    }


    private IEnumerator RemoveCoroutine(Item _item, float _duration)
    {
        if (_duration > 0f) yield return new WaitForSeconds(_duration);
        if (_item == null) yield break;

        float shrink = 0.5f;
        Vector3 from = _item.transform.localScale;
        float timer = 0f;
        while (_item != null && timer < shrink)
        {
            timer += Time.deltaTime;
            _item.transform.localScale = Vector3.Lerp(from, Vector3.zero, timer / shrink);
            yield return null;
        }

        if (_item != null)
        {
            if (Instance != null) Instance.items.Remove(_item);
            Destroy(_item.gameObject);
        }
    }

    public void RemoveAll()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
            RemoveEnemy(enemies[i]);

        for (int i = items.Count - 1; i >= 0; i--)
            RemoveItem(items[i]);
    }
    #endregion

    #region SET
    private void SetItemDic()
    {
        itemDic.Clear();
        for (int i = 0; i < itemDatas.Length; i++)
        {
            var d = itemDatas[i];
            if (d != null && !itemDic.ContainsKey(d.ID))
                itemDic.Add(d.ID, d);
        }

        itemTypeDic.Clear();
        var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
        for (int ai = 0; ai < assemblies.Length; ai++)
        {
            System.Type[] types;
            try { types = assemblies[ai].GetTypes(); }
            catch { continue; }

            for (int ti = 0; ti < types.Length; ti++)
            {
                var ty = types[ti];
                if (ty != null && typeof(Item).IsAssignableFrom(ty) && !ty.IsAbstract)
                {
                    if (!itemTypeDic.ContainsKey(ty.Name))
                        itemTypeDic.Add(ty.Name, ty);
                }
            }
        }
    }

    public void SetEntity()
    {
        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (player == null) player = GameObject.Find("InGame/Player")?.transform;
        if (enemyTrans == null) enemyTrans = GameObject.Find("InGame/Enemies")?.transform;
        if (itemTrans == null) itemTrans = GameObject.Find("InGame/Items")?.transform;

        Vector3 c = new Vector3(AutoCamera.WorldRect.center.x, AutoCamera.WorldRect.yMin * 0.6f, 0f);
        player.transform.localPosition = c;
        eBaseDelay = eDelay;
        iDelayBase = iDelay;
    }

    public void ResetEntity()
    {
        eDelay = eBaseDelay;
        iDelay = iDelayBase;

        foreach (var item in itemDatas)
            item.ResetStat(false);
    }
    #endregion

    #region GET
    public IReadOnlyList<ItemData> GetDatas() => itemDatas;
    public Player GetPlayer() => player.GetComponent<Player>();
    public Item GetClone()
    {
        for (int i = 0; i < items.Count; i++)
        {
            var it = items[i];
            if (it == null)
            {
                items.RemoveAt(i--);
                continue;
            }

            Clone clone = items[i].GetComponent<Clone>();
            if (clone != null && clone.isActive)
                return clone;
        }
        return null;
    }
    public Enemy GetEnemyClosest(Vector3 _pos)
    {
        if (enemies.Count == 0) return null;
        Enemy target = enemies[0];
        float min = (target.transform.position - _pos).sqrMagnitude;
        for (int i = 1; i < enemies.Count; i++)
        {
            Enemy e = enemies[i];
            float d = (e.transform.position - _pos).sqrMagnitude;
            if (d < min) { min = d; target = e; }
        }
        return target;
    }
    #endregion
}
