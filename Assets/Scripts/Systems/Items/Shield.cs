using System.Collections;
using UnityEngine;

public class Shield : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField] private float scale = 1.2f;
    [SerializeField] private float spin = 120f;
    #endregion

    #region 능력
    [Header("Ability")]
    private Player player;

    private bool isOrigin = true;
    [SerializeField] private int count = 2;
    [SerializeField] private int countBonus = 1;
    [SerializeField] private float gap = 2f;
    private Vector3 offset;
    [Space]
    private bool isFired = false;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float durationBonus = 5f;
    [SerializeField] private float speed = 10f;
    #endregion

    protected override void Update()
    {
        base.Update();

        if (isActive)
        {
            transform.Rotate(0f, 0f, -spin * Time.deltaTime);
            GrowScale(scale);
        }
    }

    private void LateUpdate()
    {
        if (isActive && !isFired)
            transform.position = player.transform.position + offset;
    }

    protected override void OnBecameInvisible()
    {
        if (isFired)
            EntityManager.Instance.DespawnItem(this, 0f, true);
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        rb.bodyType = RigidbodyType2D.Kinematic;
        player = EntityManager.Instance?.GetPlayer();

        if (isOrigin)
        {
            CopySelf();
            SoundManager.Instance?.PlaySFX(this.name);
            EntityManager.Instance?.DespawnItem(this, 0f, true);
        }
        else StartCoroutine(ShootCoroutine());
    }

    private void CopySelf()
    {
        float diag = gap;
        float ortho = gap * 0.6f;

        Vector3[] offs = new Vector3[]
        {
            diag * Vector3.up,
            ortho * (Vector3.up    + Vector3.right),
            ortho * (Vector3.up    + Vector3.left),
            diag * Vector3.down,
            ortho * (Vector3.down  + Vector3.right),
            ortho * (Vector3.down  + Vector3.left),
            diag * Vector3.right,
            diag * Vector3.left,
        };

        for (int i = 0; i < count + countBonus * bonusStat; i++)
        {
            Shield copy = EntityManager.Instance?.SpawnItem(data.ID, player.transform.position + offs[i])
                .GetComponent<Shield>();

            copy.SetClone();
            copy.SetOffset(offs[i]);
            copy.UseItem();
        }
    }

    private IEnumerator ShootCoroutine()
    {
        yield return new WaitForSeconds(duration + durationBonus * bonusStat);
        isFired = true;
        Move(Vector2.up * speed);
    }

    #region SET
    public void SetClone() => isOrigin = false;
    public void SetOffset(Vector3 _off) => offset = _off;
    #endregion
}

