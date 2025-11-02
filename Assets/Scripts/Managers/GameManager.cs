using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { private set; get; }

    [Header("Score")]
    [SerializeField] private int score = 0;
    private float scoreAdd = 0f;
    public event System.Action<int> OnChangeScore;

    [Header("Level")]
    [SerializeField] private int level = 0;
    [SerializeField] private int currentEXP = 0;
    [SerializeField] private int nextEXP = 0;

    public bool IsPaused { private set; get; } = false;
    public bool IsGameOver { private set; get; } = false;

    [DllImport("__Internal")] private static extern void GameOverReact();
    [DllImport("__Internal")] private static extern void ReplayReact();

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

    private void Update()
    {
        if (IsPaused || IsGameOver) return;

        scoreAdd += Time.deltaTime;

        if (scoreAdd >= 1f)
        {
            int add = Mathf.FloorToInt(scoreAdd);
            scoreAdd -= add;
            AddScore(add);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += LoadGame;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= LoadGame;
    }

    private void LoadGame(Scene _scene, LoadSceneMode _mode)
    {
        Pause(false);
        IsGameOver = false;
        ResetScore();

        ResetLevel();
        AddLevel();

        SoundManager.Instance?.PlayBGM("Default");

        UIManager.Instance?.ResetPlayTime();
        UIManager.Instance?.OpenUI(false);

        EntityManager.Instance?.SetEntity();
        EntityManager.Instance?.ToggleSpawn(true);

        HandleManager.Instance?.SetHandle();
    }

    #region 점수
    public void AddScore(int _score = 1)
    {
        score += _score;
        OnChangeScore?.Invoke(score);
        AddEXP(_score);
    }

    public void ResetScore()
    {
        score = 0;
        OnChangeScore?.Invoke(score);
    }
    #endregion

    #region 레벨
    public void AddEXP(int _exp)
    {
        currentEXP += _exp;
        while (nextEXP > 0 && currentEXP >= nextEXP)
        {
            currentEXP -= nextEXP;
            AddLevel();
        }
    }

    private void AddLevel()
    {
        level++;
        if (nextEXP <= 0) nextEXP = 10;
        else nextEXP += 10;
    }

    public void ResetLevel()
    {
        level = 0;
        currentEXP = 0;
        nextEXP = 0;
    }
    #endregion

    #region 진행
    public void Pause(bool _pause)
    {
        if (IsPaused == _pause) return;

        IsPaused = _pause;
        Time.timeScale = _pause ? 0f : 1f;
    }

    private void ActWithReward(System.Action _act)
    {
        if (ADManager.Instance != null) ADManager.Instance.ShowReward(_act);
        else _act?.Invoke();
    }

    public void Replay()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        ReplayReact();
#else
        ActWithReward(ReplayGame);
#endif
    }
    private void ReplayGame() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

    public void Quit() => ActWithReward(QuitGame);
    private void QuitGame()
    {
        Time.timeScale = 1f;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void GameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;

        Pause(true);
        SoundManager.Instance?.GameOver();
        UIManager.Instance?.OpenResult(true);

#if UNITY_WEBGL && !UNITY_EDITOR
        GameOverReact();
#endif
    }
    #endregion

    #region GET
    public int GetScore() => score;
    #endregion
}
