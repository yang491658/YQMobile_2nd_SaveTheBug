using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EntityManager : MonoBehaviour
{
    public static EntityManager Instance { private set; get; }

    private enum SpawnKind { Enemy, Item }

    [Header("Data Setting")]
    [SerializeField] private GameObject enemyBase;
    [SerializeField] private GameObject itemBase;
    [SerializeField] private ItemData[] itemDatas;
    private readonly Dictionary<int, ItemData> itemDic = new Dictionary<int, ItemData>();

    [Header("Spawn Settings")]
    [SerializeField][Min(0.05f)] private float eDelay = 5f;
    [SerializeField][Min(3f)] private float iDelay = 10f;
    private Coroutine spawnRoutine;

    [Header("Entities")]
    [SerializeField] private Transform inGame;
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemyTrans;
    [SerializeField] private List<Enemy> enemies = new List<Enemy>();
    [SerializeField] private Transform itemTrans;
    [SerializeField] private List<Item> items = new List<Item>();

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

        itemDic.Clear();
        for (int i = 0; i < itemDatas.Length; i++)
        {
            var d = itemDatas[i];
            if (d != null && !itemDic.ContainsKey(d.ID))
                itemDic.Add(d.ID, d);
        }

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
    private ItemData SearchItem(int _id) => itemDic.TryGetValue(_id, out var _data) ? _data : null;

    public Item SpawnItem(int _id = 0, Vector3? _pos = null)
    {
        ItemData data = (_id == 0)
            ? itemDatas[Random.Range(0, itemDatas.Length)]
            : SearchItem(_id);
        if (data == null) return null;

        Vector3 pos = SpawnPos(SpawnKind.Item, _pos);

        var go = Instantiate(itemBase, pos, Quaternion.identity, itemTrans);

        System.Type t = null;
        if (data.Script != null) t = data.Script.GetClass();
        Item i = t != null ? (Item)go.AddComponent(t) : go.AddComponent<Item>();

        i.SetData(data.Clone());
        items.Add(i);

        return i;
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

            eDelay = Mathf.Max(0.05f, eDelay - dt / 50f);
            iDelay = Mathf.Max(3f, iDelay - dt / 35f);

            int cnt = 0;
            while (eTimer >= eDelay && cnt++ < 4)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.01f);

                eTimer -= eDelay;
            }

            cnt = 0;
            while (iTimer >= iDelay && cnt++ < 4)
            {
                SpawnItem();
                yield return new WaitForSeconds(0.01f);
                iTimer -= iDelay;
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

    private static IEnumerator RemoveCoroutine(Item _item, float _duration)
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
    public void SetEntity()
    {
        if (inGame == null) inGame = GameObject.Find("InGame")?.transform;
        if (player == null) player = GameObject.Find("InGame/Player")?.transform;
        if (enemyTrans == null) enemyTrans = GameObject.Find("InGame/Enemies")?.transform;
        if (itemTrans == null) itemTrans = GameObject.Find("InGame/Items")?.transform;
    }
    #endregion

    #region GET
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
    public Enemy GetEnemyRandom()
    {
        if (enemies.Count == 0) return null;
        return enemies[Random.Range(0, enemies.Count)];
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
