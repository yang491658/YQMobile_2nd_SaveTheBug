using UnityEngine;

public class Item : Entity
{
    public bool isActive { private set; get; } = false;

    [Header("Stat")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float growSpeed = 10f;
    [SerializeField] protected int bonusStat;
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
            Move(dir.normalized * moveSpeed);
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
            GameManager.Instance?.ScoreUp(3);
            EntityManager.Instance?.RemoveEnemy(_collision.GetComponent<Enemy>());
        }
    }

    protected virtual void OnBecameInvisible()
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

    public virtual void GrowScale(float _scale)
    {
        transform.localScale = Vector3.MoveTowards(
            transform.localScale,
            Vector3.one * _scale,
            Time.deltaTime * growSpeed
        );
    }

    #region SET
    public override void SetData(EntityData _data)
    {
        base.SetData(_data);

        bonusStat = ((ItemData)_data).Stat;
    }
    public void SetScale(float _scale) => transform.localScale = Vector3.one * _scale;
    #endregion
}
