using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Enemy : Entity
{
    [SerializeField] private float speed = 3f;

#if UNITY_EDITOR
    private void OnValidate()
    {
        data = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Datas/Enemy.asset");
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        SetData(data.Clone());
    }

    protected override void Start()
    {
        base.Start();

        Entity target = EntityManager.Instance?.GetClone() as Entity
                        ?? EntityManager.Instance?.GetPlayer();

        Vector3 dir = (target.transform.position - transform.position).normalized;
        Move(dir * speed);
    }

    private void OnBecameInvisible()
    {
        EntityManager.Instance?.DespawnEnemy(this);
    }
}
