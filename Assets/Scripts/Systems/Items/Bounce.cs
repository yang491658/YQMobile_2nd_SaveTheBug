using UnityEngine;

public class Bounce : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField][Min(0f)] private float scale = 3.0f;
    [SerializeField][Min(0f)] private float spin = 30f;
    #endregion

    #region 능력
    [Header("Ability")]
    private Player player;

    [SerializeField][Min(0f)] private float speedRatio = 5f;
    [SerializeField][Min(0f)] private float minSpeed = 5f;
    private Vector3 direction = Vector3.up;
    [Space]
    [SerializeField][Min(0)] private int bounce = 3;
    [SerializeField][Min(0)] private int bounceBonus = 1;
    [SerializeField][Min(0f)] private float duration = 10f;
    [SerializeField][Min(0f)] private float durationBonus = 10f;
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
            SoundManager.Instance?.PlaySFX(this.name);
        }
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        player = EntityManager.Instance?.GetPlayer();
        bounce += bounceBonus * bonusStat;

        SetDirection(player.transform.up);
        Shoot();
        SoundManager.Instance?.PlaySFX(this.name);
        EntityManager.Instance?.DespawnItem(this, duration + durationBonus * bonusStat);

    }

    private void Shoot()
        => Move(direction * Mathf.Max(player.GetSpeed() * speedRatio, minSpeed));

    #region SET
    private void SetDirection(Vector3 _dir)
    {
        transform.up = _dir;
        direction = _dir;
    }
    #endregion
}
