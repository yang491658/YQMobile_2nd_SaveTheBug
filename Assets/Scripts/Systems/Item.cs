using UnityEngine;

public class Item : Entity
{
    public bool isActive { private set; get; } = false;

    [Header("Stat")]
    [SerializeField][Min(0f)] private float moveSpeed = 3.5f;
    [SerializeField][Min(0f)] private float growSpeed = 10f;
    [SerializeField][Min(0)] protected int bonusStat;
    [SerializeField][Min(0f)] private float bgDuration = 15f;
    private float bgTimer = 0f;
    private Collider2D bgCol;

    protected AudioSource sfxLoop;

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
            SoundManager.Instance?.PlaySFX("Kill");
            EntityManager.Instance?.DespawnEnemy(_collision.GetComponent<Enemy>());
        }
    }

    protected virtual void OnBecameInvisible()
    {
        EntityManager.Instance?.DespawnItem(this, 0f, true);
    }

    protected virtual void OnDestroy()
    {
        if (sfxLoop != null)
            SoundManager.Instance?.StopSFXLoop(sfxLoop);
        sfxLoop = null;
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
        Vector3 target = Vector3.one * _scale;
        Vector3 current = transform.localScale;
        Vector3 next = Vector3.Lerp(
            current,
            target,
            Time.deltaTime * growSpeed
        );

        transform.localScale = next;
        if ((next - target).sqrMagnitude <= 0.0001f)
            transform.localScale = target;
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
