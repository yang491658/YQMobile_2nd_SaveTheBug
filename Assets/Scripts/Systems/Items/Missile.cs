using UnityEngine;

public class Missile : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField][Min(0f)] private float scale = 3.0f;
    #endregion

    #region 능력
    [Header("Ability")]
    [SerializeField][Min(0f)] private float speed = 10f;
    [SerializeField][Min(0f)] private float speedBonus = 1f;
    [SerializeField][Min(0f)] private float minSpeed = 0.5f;
    #endregion

    protected override void Update()
    {
        base.Update();

        if (isActive)
            GrowScale(scale);
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        Shoot();
        SoundManager.Instance?.PlaySFXLoop(this.name, transform);
    }

    private void Shoot()
        => Move(Vector3.up * Mathf.Max(speed - speedBonus * bonusStat, minSpeed));
}
