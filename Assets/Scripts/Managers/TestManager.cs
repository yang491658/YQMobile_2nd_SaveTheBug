using System.Collections;
using UnityEngine;

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField] private int testCount = 1;
    [SerializeField] private bool isAuto = false;
    [SerializeField][Min(1f)] private float autoReplay = 1f;
    private Coroutine autoRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        AutoPlay();
    }

    private void Update()
    {
        #region 게임 테스트
        if (Input.GetKeyDown(KeyCode.P))
            GameManager.Instance?.Pause(!GameManager.Instance.IsPaused);
        if (Input.GetKeyDown(KeyCode.G))
            GameManager.Instance?.GameOver();
        if (Input.GetKeyDown(KeyCode.R))
            GameManager.Instance?.Replay();
        if (Input.GetKeyDown(KeyCode.Q))
            GameManager.Instance?.Quit();

        if (Input.GetKeyDown(KeyCode.O))
            AutoPlay();
        if (isAuto)
        {
            MoveItem();
            if (!GameManager.Instance.IsPaused) RandomStatUp();

            if (GameManager.Instance.IsGameOver && autoRoutine == null)
                autoRoutine = StartCoroutine(AutoReplay());
        }

        if (Input.GetKeyDown(KeyCode.L))
            GameManager.Instance?.LevelUp();
        #endregion

        #region 사운드 테스트
        if (Input.GetKeyDown(KeyCode.B))
        {
            bgmPause = !bgmPause;
            SoundManager.Instance?.PauseSound(bgmPause);
        }
        if (Input.GetKeyDown(KeyCode.M))
            SoundManager.Instance?.ToggleBGM();
        if (Input.GetKeyDown(KeyCode.N))
            SoundManager.Instance?.ToggleSFX();
        #endregion

        #region 엔티티 테스트
        for (int i = 1; i <= 10; i++)
        {
            KeyCode key = (i == 10) ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha0 + i);
            if (Input.GetKeyDown(key))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    ItemData item = EntityManager.Instance?.SearchItem(i);

                    if (item.Level > GameManager.Instance?.GetLevel())
                        GameManager.Instance?.LevelUp(item.Level - GameManager.Instance.GetLevel());
                    if (GameManager.Instance?.GetPoint() <= 0)
                        GameManager.Instance?.LevelUp();

                    item.StatUp();
                    break;
                }
                else
                {
                    Vector3 p = EntityManager.Instance.GetPlayer().transform.position;
                    EntityManager.Instance?.SpawnItem(i, p);
                    break;
                }

            }
        }

        if (Input.GetKey(KeyCode.E))
            EntityManager.Instance?.SpawnEnemy();
        if (Input.GetKey(KeyCode.T))
            EntityManager.Instance?.SpawnItem();
        if (Input.GetKeyDown(KeyCode.Delete))
            EntityManager.Instance?.DespawnAll();
        #endregion

        #region UI 테스트
        if (Input.GetKeyDown(KeyCode.Z))
            UIManager.Instance?.OpenSetting(!UIManager.Instance.GetOnSetting());
        if (Input.GetKeyDown(KeyCode.X))
            UIManager.Instance?.OpenStat(!UIManager.Instance.GetOnStat());
        if (Input.GetKeyDown(KeyCode.C))
            UIManager.Instance?.OpenConfirm(!UIManager.Instance.GetOnConfirm());
        if (Input.GetKeyDown(KeyCode.V))
            UIManager.Instance?.OpenResult(!UIManager.Instance.GetOnResult());
        #endregion
    }

    private void AutoPlay()
    {
        isAuto = !isAuto;

        GameManager.Instance?.SetSpeed(isAuto ? GameManager.Instance.GetMaxSpeed() : 1f);
        SoundManager.Instance?.ToggleBGM();
        SoundManager.Instance?.ToggleSFX();
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoReplay);
        if (GameManager.Instance.IsGameOver)
        {
            testCount++;
            GameManager.Instance?.Replay();
        }
        autoRoutine = null;
    }

    private void MoveItem()
    {
        var items = EntityManager.Instance?.GetItems();
        Player player = EntityManager.Instance?.GetPlayer();
        Vector3 targetPos = player.transform.position;

        for (int i = 0; i < items.Count; i++)
        {
            Item item = items[i];
            if (!item.isActive)
                item.Move((targetPos - item.transform.position).normalized * 3.5f);
        }
    }

    private void RandomStatUp()
    {
        if (GameManager.Instance?.GetPoint() <= 0)
            return;

        var datas = EntityManager.Instance?.GetItemDatas();
        if (datas.Count == 0)
            return;

        int index = Random.Range(0, datas.Count);
        ItemData item = datas[index];
        item.StatUp();
    }
}
