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
    private readonly Dictionary<int, ItemData> itemDic = new Dictionary<int, ItemData>();
    private readonly Dictionary<string, System.Type> itemTypeDic = new Dictionary<string, System.Type>();

    [Header("Spawn Settings")]
    [SerializeField][Min(0.05f)] private float eDelay = 5;
    [SerializeField][Min(0.05f)] private float eDelayMin = 0.05f;
    [SerializeField][Min(0.05f)] private float iDelay = 10f;
    [SerializeField][Min(0.05f)] private float iDelayMin = 3f;
    private float eDelayBase;
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

        System.Type t = null;
        itemTypeDic.TryGetValue(data.Name, out t);

        Item i = t != null ? (Item)go.AddComponent(t) : go.AddComponent<Item>();
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

        float minX = r.xMin, maxX = r.xMax;
        float minY = r.yMin, maxY = r.yMax, midY = r.center.y;

        bool enemy = _kind == SpawnKind.Enemy;
        int edge = Random.Range(0, enemy ? 3 : 4);
        float x = Random.Range(minX, maxX);
        float y = enemy ? Random.Range(midY, maxY) : Random.Range(minY, maxY);

        if (enemy)
            return edge == 0 ? new Vector3(x, maxY)
                 : edge == 1 ? new Vector3(minX, y)
                 : new Vector3(maxX, y);

        float p = 1.5f;
        float ix = Random.Range(minX + p, maxX - p);
        float iy = Random.Range(minY + p, maxY - p);

        return edge == 0 ? new Vector3(ix, maxY) + Vector3.down
             : edge == 1 ? new Vector3(ix, minY) + Vector3.up
             : edge == 2 ? new Vector3(minX, iy) + Vector3.right
             : new Vector3(maxX, iy) + Vector3.left;
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

            eDelay = Mathf.Max(eDelay - dt / 50f, eDelayMin);
            iDelay = Mathf.Max(iDelay - dt / 35f, iDelayMin);

            int cnt = 0;
            while (eTimer >= eDelay && cnt++ < 4)
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

            cnt = 0;
            while (iTimer >= iDelay && cnt++ < 4)
            {
                var item = SpawnItem();
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

    public void RemoveItem(Item _item, float _duration = 0f)
    {
        if (_item == null) return;

        if (_duration <= 0f)
        {
            items.Remove(_item);
            Destroy(_item.gameObject);
            return;
        }

        _item.StartCoroutine(RemoveCoroutine(_item, _duration));
    }

    static private IEnumerator RemoveCoroutine(Item _item, float _duration)
    {
        yield return new WaitForSeconds(_duration);

        Instance?.items.Remove(_item);
        if (_item != null) Destroy(_item.gameObject);
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
        eDelayBase = eDelay;
        iDelayBase = iDelay;
    }

    public void ResetEntity()
    {
        eDelay = eDelayBase;
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
