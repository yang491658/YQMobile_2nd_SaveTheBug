using UnityEngine;

public class Item : Entity
{
    public bool isActive { private set; get; } = false;

    [Header("Stat")]
    [SerializeField] private float iSpeed = 3.5f;
    [SerializeField] protected int bonus;
    [SerializeField] private float bgDuration = 15f;
    private float bgTimer = 0f;
    private Collider2D bgCol;

    protected override void Awake()
    {
        base.Awake();

        bgCol = transform.Find("Background")?.GetComponent<Collider2D>();
    }

    protected override void Start()
    {
        base.Start();

        if (!isActive)
        {
            float angle = Random.Range(-15f, 15f);
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * (-transform.position);
            Move(dir.normalized * iSpeed);
        }
    }

    protected override void Update()
    {
        bgTimer += Time.deltaTime;
        if (bgTimer > bgDuration && bgCol != null)
            bgCol.isTrigger = true;
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

        if (bgCol != null) Destroy(bgCol.gameObject);
    }

    #region SET
    public override void SetData(EntityData _data)
    {
        base.SetData(_data);

        bonus = ((ItemData)_data).Stat;
    }
    #endregion
}
