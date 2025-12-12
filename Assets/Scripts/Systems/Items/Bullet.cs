using System.Collections;
using UnityEngine;

public class Bullet : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField][Min(0f)] private float scale = 0.8f;
    #endregion

    #region 능력
    [Header("Ability")]
    private Player player;

    private bool isOrigin = true;
    [SerializeField][Min(0)] private int count = 10;
    [SerializeField][Min(0)] private int countBonus = 2;
    [SerializeField][Min(0f)] private float speedRatio = 3f;
    [SerializeField][Min(0f)] private float minSpeed = 1f;
    private Vector3 direction = Vector3.up;
    #endregion

    private void LateUpdate()
    {
        if (isActive && isOrigin && player != null)
            transform.position = player.transform.position;
    }

    public override void UseItem()
    {
        if (isActive) return;
        base.UseItem();

        player = EntityManager.Instance?.GetPlayer();

        if (isOrigin)
        {
            transform.position = player.transform.position;
            sr.color = new Color32(255, 255, 255, 0);
            StartCoroutine(CopySelfCoroutine());
        }
        else
        {
            SetScale(scale);
            Shoot();
            SoundManager.Instance?.PlaySFX(this.name);
        }
    }

    private IEnumerator CopySelfCoroutine()
    {
        int totalCount = count + countBonus * bonusStat;
        for (int i = 0; i < totalCount; i++)
        {
            Bullet copy = EntityManager.Instance?.SpawnItem(data.ID, player.transform.position)
                .GetComponent<Bullet>();

            Enemy enemy = EntityManager.Instance?.GetEnemy(i + 1);
            Vector3 dir = player.transform.up;
            if (enemy != null)
                dir = (enemy.transform.position - player.transform.position).normalized;

            copy.SetClone();
            copy.SetDirection(dir);
            copy.UseItem();

            while ((copy.transform.position - player.transform.position).sqrMagnitude < scale * scale / 2f)
                yield return null;
        }

        EntityManager.Instance?.DespawnItem(this, 0f, true);
    }


    private void Shoot()
        => Move(direction * Mathf.Max(player.GetSpeed() * speedRatio, minSpeed));

    #region SET
    public void SetClone() => isOrigin = false;
    public void SetDirection(Vector3 _dir)
    {
        transform.up = _dir;
        direction = _dir;
    }
    #endregion
}
