using UnityEngine;

public class Bomb : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField] private float scale = 4f;
    [SerializeField] private float spin = -30f;
    #endregion

    #region 능력
    [Header("Ability")]
    [SerializeField] private float duration = 10f;
    [SerializeField] private float durationBonus = 5f;
    #endregion

    protected override void Update()
    {
        base.Update();

        if (isActive)
            transform.Rotate(0f, 0f, spin * Time.deltaTime);
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        transform.localScale *= scale;

        Stop();
        EntityManager.Instance?.RemoveItem(this, duration + durationBonus * bonus);
    }
}
