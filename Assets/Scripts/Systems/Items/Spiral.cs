using System.Collections;
using UnityEngine;

public class Spiral : Item
{
    #region 스케일
    [Header("Scale")]
    [SerializeField] private float scale = 1f;
    #endregion

    #region 능력
    [Header("Ability")]
    private Player player;

    private bool isOrigin = true;
    [SerializeField] private int count = 12;
    [SerializeField] private float angle = 30f;
    [SerializeField] private float speed = 8f;
    private Vector3 direction = Vector3.up;
    [SerializeField] private float delay = 0.05f;
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
            StartCoroutine(CopySelf());
        }
        else
        {
            transform.localScale *= scale;
            Fire();
        }
    }

    private IEnumerator CopySelf()
    {
        Vector3 baseDir = player.transform.up;

        for (int i = 0; i < count; i++)
        {
            Vector3 dir = Quaternion.Euler(0f, 0f, -angle * i) * baseDir;

            Spiral copy = EntityManager.Instance?.SpawnItem(data.ID, player.transform.position)
                .GetComponent<Spiral>();

            copy.SetClone();
            copy.SetDirection(dir);
            copy.UseItem();

            yield return new WaitForSeconds(delay);
        }

        EntityManager.Instance?.RemoveItem(this);
    }

    private void Fire() => Move(direction * speed);

    #region SET
    public void SetClone() => isOrigin = false;
    public void SetDirection(Vector3 _dir)
    {
        transform.up = _dir;
        direction = _dir;
    }
    #endregion
}

