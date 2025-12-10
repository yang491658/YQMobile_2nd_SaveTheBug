using UnityEngine;

public class Clone : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField][Min(0f)] private float scale = 1f;
    #endregion

    #region 능력
    [Header("Ability")]
    [SerializeField][Min(0f)] private float speed = 9f;
    [SerializeField][Min(0f)] private float speedBonus = 2f;
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
        SoundManager.Instance?.PlaySFX(this.name);
    }

    private void Shoot()
        => Move(Vector3.up * (speed - speedBonus * bonusStat));
}
