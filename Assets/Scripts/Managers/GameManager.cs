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

    [Header("Exp")]
    [SerializeField] private int currentExp = 0;
    [SerializeField] private int nextExp = 0;
    [SerializeField] private int expUp = 10;
    public event System.Action<int> OnChangeExp;

    [Header("Level")]
    [SerializeField] private int level = 0;
    public event System.Action<int> OnChangeLevel;

    [Header("Pont")]
    [SerializeField] private int point = 0;
    public event System.Action<int> OnChangePoint;

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
        LevelUp();

        SoundManager.Instance?.PlayBGM("Default");

        UIManager.Instance?.ResetPlayTime();
        UIManager.Instance?.OpenUI(false);

        EntityManager.Instance?.ResetDelay();
        EntityManager.Instance?.SetEntity();
        EntityManager.Instance?.ToggleSpawn(true);

        HandleManager.Instance?.SetHandle();
    }

    #region 점수
    public void AddScore(int _score = 1)
    {
        score += _score;
        OnChangeScore?.Invoke(score);
    }

    public void ResetScore()
    {
        score = 0;
        OnChangeScore?.Invoke(score);
    }
    #endregion

    #region 레벨
    public void AddExp(int _exp)
    {
        currentExp += _exp;
        while (nextExp > 0 && currentExp >= nextExp)
        {
            currentExp -= nextExp;
            LevelUp();
        }
        OnChangeExp?.Invoke(currentExp);
    }

    public void LevelUp()
    {
        level++;
        point++;

        if (nextExp <= 0) nextExp = expUp;
        else nextExp += expUp;
        OnChangeLevel?.Invoke(level);
        OnChangeExp?.Invoke(currentExp);
        OnChangePoint?.Invoke(point);
    }

    public void ResetLevel()
    {
        level = 0;
        currentExp = 0;
        nextExp = 0;
        point = 0;
        OnChangeLevel?.Invoke(level);
        OnChangeExp?.Invoke(currentExp);
    }
    #endregion

    #region 포인트
    public void UsePoint()
    {
        point--;
        OnChangePoint?.Invoke(point);
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
        if (ADManager.Instance != null) ADManager.Instance?.ShowReward(_act);
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
    public int GetCurrentExp() => currentExp;
    public int GetNextExp() => nextExp;
    public int GetLevel() => level;
    public int GetStatPoint() => point;
    #endregion
}
