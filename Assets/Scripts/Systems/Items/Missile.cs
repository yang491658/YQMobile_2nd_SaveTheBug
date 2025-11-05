using UnityEngine;

public class Missile : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField] private float scale = 3.0f;
    #endregion

    #region 능력
    [Header("Ability")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float speedBonus = 1f;
    [SerializeField] private float minSpeed = 0.5f;
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
        => Move(Vector3.up * Mathf.Max(speed - speedBonus * bonusStat, minSpeed));
}
