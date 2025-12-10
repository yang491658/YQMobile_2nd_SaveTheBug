using System.Collections;
using UnityEngine;

public class Spiral : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField][Min(0f)] private float scale = 1f;
    #endregion

    #region 능력
    [Header("Ability")]
    private Player player;

    private bool isOrigin = true;
    [SerializeField][Min(0)] private int count = 1;
    [SerializeField][Min(0)] private int countBonus = 12;
    [SerializeField][Min(0f)] private float angle = 30f;
    [SerializeField][Min(0f)] private float speed = 8f;
    private Vector3 direction = Vector3.up;
    [SerializeField][Min(0f)] private float delay = 0.05f;
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
        Vector3 baseDir = player.transform.up;

        int totalCount = count + countBonus * bonusStat;
        for (int i = 0; i < totalCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, 0f, -angle * i) * baseDir;

            Spiral copy = EntityManager.Instance?.SpawnItem(data.ID, player.transform.position)
                .GetComponent<Spiral>();

            copy.SetClone();
            copy.SetDirection(dir);
            copy.UseItem();

            yield return new WaitForSeconds(delay);
        }

        EntityManager.Instance?.DespawnItem(this, 0f, true);
    }

    private void Shoot() => Move(direction * speed);

    #region SET
    public void SetClone() => isOrigin = false;
    public void SetDirection(Vector3 _dir)
    {
        transform.up = _dir;
        direction = _dir;
    }
    #endregion
}

