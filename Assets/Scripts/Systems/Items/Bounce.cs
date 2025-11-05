using UnityEngine;

public class Bounce : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField] private float scale = 3.0f;
    [SerializeField] private float spin = 30f;
    #endregion

    #region 능력
    [Header("Ability")]
    private Player player;

    [SerializeField] private float speedRatio = 5f;
    [SerializeField] private float minSpeed = 5f;
    private Vector3 direction = Vector3.up;

    [SerializeField] private int bounce = 3;
    [SerializeField] private int bounceBonus = 1;
    [SerializeField] private float duration = 10f;
    [SerializeField] private float durationBonus = 10f;
    #endregion

    protected override void Update()
    {
        base.Update();

        if (isActive)
        {
            transform.Rotate(0f, 0f, -spin * rb.linearVelocity.magnitude * Time.deltaTime);
            GrowScale(scale);
        }
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.CompareTag("Background") && bounce > 0)
        {
            var wall = _collision.gameObject.name;
            if (wall.EndsWith("Top") || wall.EndsWith("Bottom"))
                rb.linearVelocityY *= -1;
            else if (wall.EndsWith("Left") || wall.EndsWith("Right"))
                rb.linearVelocityX *= -1;

            bounce--;
        }
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        player = EntityManager.Instance?.GetPlayer();
        bounce += bounceBonus * bonusStat;

        SetDirection(player.transform.up);
        Fire();
        EntityManager.Instance?.RemoveItem(this, duration + durationBonus * bonusStat);
    }

    private void Fire()
        => Move(direction * Mathf.Max(player.GetSpeed() * speedRatio, minSpeed));

    #region SET
    private void SetDirection(Vector3 _dir)
    {
        transform.up = _dir;
        direction = _dir;
    }
    #endregion
}
