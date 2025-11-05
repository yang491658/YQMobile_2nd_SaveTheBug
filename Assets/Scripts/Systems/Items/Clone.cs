using UnityEngine;

public class Clone : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField] private float scale = 1f;
    #endregion

    #region 능력
    [Header("Ability")]
    [SerializeField] private float speed = 9f;
    [SerializeField] private float speedBonus = 2f;
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

        Fire();
    }

    private void Fire()
        => Move(Vector3.up * (speed - speedBonus * bonusStat));
}
