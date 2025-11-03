using UnityEngine;

public class Item : Entity
{
    public bool isActive { private set; get; } = false;

    private Collider2D backCol;
    private float timer = 0f;

    [Header("Stat")]
    [SerializeField] private float speed = 3.5f;
    [SerializeField] protected int stat;
    [SerializeField] private float delay = 15f;

    protected override void Awake()
    {
        base.Awake();

        backCol = transform.Find("Background")?.GetComponent<Collider2D>();
    }

    protected override void Start()
    {
        base.Start();

        if (!isActive)
        {
            float angle = Random.Range(-15f, 15f);
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * (-transform.position);
            Move(dir.normalized * speed);
        }
    }

    protected override void Update()
    {
        timer += Time.deltaTime;
        if (timer > delay && backCol != null)
            backCol.isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D _collision)
    {
        if (_collision.CompareTag("Enemy") && isActive)
        {
            GameManager.Instance?.ScoreUp(5);
            GameManager.Instance?.ExpUp(5);
            EntityManager.Instance?.RemoveEnemy(_collision.GetComponent<Enemy>());
        }
    }

    private void OnBecameInvisible()
    {
        EntityManager.Instance?.RemoveItem(this);
    }

    public virtual void UseItem()
    {
        if (isActive) return;

        sr.sortingOrder = ((ItemData)data).Sort;
        col.isTrigger = true;
        isActive = true;

        if (backCol != null) Destroy(backCol.gameObject);
    }

    #region SET
    public override void SetData(EntityData _data)
    {
        base.SetData(_data);

        stat = ((ItemData)_data).Stat;
    }
    #endregion
}
