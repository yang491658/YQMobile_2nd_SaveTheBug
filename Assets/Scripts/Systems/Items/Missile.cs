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
    [SerializeField] private float speedBobus = 1f;
    #endregion

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        transform.localScale *= scale;
        Fire();
    }

    private void Fire()
        => Move(Vector3.up * (speed - speedBobus * bonus));
}
