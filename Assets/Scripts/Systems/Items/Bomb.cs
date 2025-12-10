using UnityEngine;

public class Bomb : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField][Min(0f)] private float scale = 4f;
    [SerializeField][Min(0f)] private float spin = 30f;
    #endregion

    #region 능력
    [Header("Ability")]
    [SerializeField][Min(0f)] private float duration = 10f;
    [SerializeField][Min(0f)] private float durationBonus = 5f;
    #endregion

    protected override void Update()
    {
        base.Update();

        if (isActive)
        {
            transform.Rotate(0f, 0f, -spin * Time.deltaTime);
            GrowScale(scale);
        }
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        Stop();
        SoundManager.Instance?.PlaySFX(this.name);
        EntityManager.Instance?.DespawnItem(this, duration + durationBonus * bonusStat);
    }
}
