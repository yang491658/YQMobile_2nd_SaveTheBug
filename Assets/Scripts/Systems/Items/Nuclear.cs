using UnityEngine;

public class Nuclear : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField][Min(0f)] private float scale = 1.2f;
    [SerializeField][Min(0f)] private float spin = 120f;
    #endregion

    #region 능력
    [Header("Ability")]
    private bool isOrigin = true;
    [SerializeField][Min(0)] private int count = 1;
    [SerializeField][Min(0)] private int countBonus = 2;
    [SerializeField][Min(0f)] private float gap = 1.5f;
    [SerializeField][Min(0f)] private float speed = 16f;
    [SerializeField][Min(0f)] private float speedBonus = 2f;
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

        if (isOrigin)
        {
            CopySelf();
            SoundManager.Instance?.PlaySFX(this.name);
            EntityManager.Instance?.DespawnItem(this, 0f, true);
        }
        else Shoot();
    }

    private void CopySelf()
    {
        Vector3 c = new Vector3(AutoCamera.WorldRect.center.x, AutoCamera.WorldRect.yMin, 0f);

        int totalCount = count + countBonus * bonusStat;
        for (int i = 0; i < totalCount; i++)
        {
            int k = i == 0 ? 0 : ((i % 2 == 1) ? (i + 1) / 2 : -i / 2);
            Vector3 pos = new Vector3(c.x + gap * k, c.y, 0f);

            Nuclear copy = EntityManager.Instance?.SpawnItem(data.ID, pos)
                .GetComponent<Nuclear>();

            copy.SetClone();
            copy.UseItem();
        }
    }

    private void Shoot() => Move(Vector3.up * (speed - speedBonus * bonusStat));

    #region SET
    public void SetClone() => isOrigin = false;
    #endregion
}
