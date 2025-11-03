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
    [SerializeField] private float speedBonus = 1f;
    #endregion

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        transform.localScale *= scale;
        Fire();
    }

    private void Fire()
        => Move(Vector3.up * (speed - speedBonus * bonus));
}
