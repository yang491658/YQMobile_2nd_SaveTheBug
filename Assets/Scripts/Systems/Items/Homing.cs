using System.Collections;
using UnityEngine;

public class Homing : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField] private float scale = 2.8f;
    [SerializeField] private float spin = 360f;
    #endregion

    #region 능력
    [Header("Ability")]
    private Player player;
    private Enemy target;

    private bool isOrigin = true;
    private bool isMoving = true;
    private bool isHoming = false;

    [SerializeField] private int count = 3;
    [SerializeField] private float countBonus = 0.4f;
    [SerializeField] private float angle = 60f;
    [SerializeField] private float speed = 10f;
    private Vector3 direction = Vector3.up;
    [Space]
    private Vector3 basePos;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float duration = 3f;
    [SerializeField] private float durationBonus = 0.5f;
    #endregion

    protected override void Update()
    {
        base.Update();

        if (isActive)
        {
            transform.Rotate(0f, 0f, -spin * Time.deltaTime);
            if (!isMoving) GrowScale(scale);
        }
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("Enemy") && isActive && !isOrigin && isMoving)
        {
            if ((transform.position - basePos).sqrMagnitude > (distance * distance))
            {
                sr.sortingOrder = 1;
                spin *= scale;
                isMoving = false;

                Stop();
                sfxLoop = SoundManager.Instance?.PlaySFXLoop(this.name + "2", transform);
                EntityManager.Instance?.DespawnItem(this, duration + durationBonus * bonusStat);
            }
        }
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        basePos = transform.position;

        if (isOrigin)
        {
            CopySelf();
            SoundManager.Instance?.PlaySFX(this.name);
            EntityManager.Instance?.DespawnItem(this, 0f, true);
        }
        else StartCoroutine(ChaseCoroutine());
    }

    private void CopySelf()
    {
        player = EntityManager.Instance?.GetPlayer();

        int totalCount = count + (int)(countBonus * bonusStat);
        float start = -angle * 0.5f;
        float step = (totalCount - 1) > 0 ? angle / (totalCount - 1) : 0f;

        Vector3 baseDir = SetRotate(direction, start);

        for (int i = 0; i < totalCount; i++)
        {
            float deg = start + step * i;
            Vector3 dir = SetRotate(baseDir, deg - start);

            Homing copy = EntityManager.Instance?.SpawnItem(data.ID, player.transform.position)
                .GetComponent<Homing>();

            copy.SetClone();
            copy.SetDirection(dir);
            copy.UseItem();
        }
    }

    private IEnumerator ChaseCoroutine()
    {
        while (isMoving)
        {
            if (!isHoming)
                if ((transform.position - basePos).sqrMagnitude >= (distance * distance))
                    isHoming = true;

            if (isHoming)
            {
                if (target == null)
                    target = EntityManager.Instance?.GetEnemyClosest(transform.position);

                if (target != null)
                    direction = (target.transform.position - transform.position).normalized;
            }

            if (direction == Vector3.zero)
                direction = Vector3.up;

            Move(direction * speed);

            yield return null;
        }
    }

    #region SET
    public void SetClone() => isOrigin = false;
    private Vector3 SetRotate(Vector3 _dir, float _deg)
    {
        float r = _deg * Mathf.Deg2Rad;
        float cs = Mathf.Cos(r);
        float sn = Mathf.Sin(r);
        return new Vector3(_dir.x * cs - _dir.y * sn, _dir.x * sn + _dir.y * cs, 0f).normalized;
    }
    public void SetDirection(Vector3 _dir)
    {
        transform.up = _dir;
        direction = _dir;
    }
    #endregion
}
