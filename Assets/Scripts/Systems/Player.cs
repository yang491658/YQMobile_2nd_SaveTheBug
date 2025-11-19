using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Player : Entity
{
#if UNITY_EDITOR
    private void OnValidate()
    {
        data = AssetDatabase.LoadAssetAtPath<EntityData>("Assets/Datas/Player.asset");
    }
#endif

    protected override void Awake()
    {
        base.Awake();

        SetData(data.Clone());
    }

    private void OnTriggerStay2D(Collider2D _collision)
    {
        if (_collision.CompareTag("Background"))
        {
            ColliderDistance2D d = col.Distance(_collision);
            if (d.isOverlapped)
                rb.position += d.normal * d.distance;
        }
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("Enemy"))
            GameManager.Instance?.GameOver();

        if (_collision.CompareTag("Item"))
            _collision.GetComponent<Item>().UseItem();
    }

    #region GET
    public float GetSpeed() => rb.linearVelocity.magnitude;
    #endregion
}
