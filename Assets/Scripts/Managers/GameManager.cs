using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { private set; get; }

    [Header("Speed")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private float minSpeed = 0.5f;
    [SerializeField] private float maxSpeed = 2f;
    public event System.Action<float> OnChangeSpeed;

    [Header("Score")]
    [SerializeField] private int score = 0;
    private float scoreAdd = 0f;
    public event System.Action<int> OnChangeScore;

    [Header("Exp")]
    [SerializeField][Min(0)] private int currentExp = 0;
    [SerializeField][Min(0)] private int nextExp = 0;
    [SerializeField][Min(1)] private int nextExpUp = 10;
    public event System.Action<int> OnChangeExp;

    [Header("Level")]
    [SerializeField][Min(0)] private int level = 0;
    [SerializeField][Min(1)] private int maxLevel = 40;
    public event System.Action<int> OnChangeLevel;

    [Header("Point")]
    [SerializeField][Min(0)] private int point = 0;
    public event System.Action<int> OnChangePoint;

    public bool IsPaused { private set; get; } = false;
    public bool IsGameOver { private set; get; } = false;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void GameOverReact();
    [DllImport("__Internal")] private static extern void ReplayReact();
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
        minSpeed = Mathf.Clamp(minSpeed, 0.05f, 1f);
        maxSpeed = Mathf.Clamp(maxSpeed, 1f, 100f);
        if (minSpeed > maxSpeed) minSpeed = maxSpeed;
    }
#endif

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
            ScoreUp(add);
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

        SoundManager.Instance?.PlayBGM("Default");

        EntityManager.Instance?.ResetEntity();
        EntityManager.Instance?.SetEntity();
        EntityManager.Instance?.ToggleSpawn(true);

        UIManager.Instance?.ResetUI();
        UIManager.Instance?.OpenUI(false);

        HandleManager.Instance?.SetHandle();

        if (level == 0) LevelUp();
    }

    #region 점수
    public void ScoreUp(int _score = 1)
    {
        score += _score;

        OnChangeScore?.Invoke(score);

        ExpUp(_score);
    }

    public void ResetScore()
    {
        score = 0;

        OnChangeScore?.Invoke(score);
    }
    #endregion

    #region 경험치
    public void ExpUp(int _exp = 1)
    {
        currentExp += _exp;
        while (nextExp > 0 && currentExp >= nextExp)
        {
            currentExp -= nextExp;
            LevelUp();
        }

        OnChangeExp?.Invoke(currentExp);
    }

    public void ResetExp()
    {
        currentExp = 0;
        nextExp = 0;

        OnChangeExp?.Invoke(currentExp);
    }
    #endregion

    #region 레벨
    public void LevelUp(int _level = 1)
    {
        if (IsMaxLevel()) return;

        int prev = level;
        level = Mathf.Min(level + _level, maxLevel);

        OnChangeLevel?.Invoke(level);

        int up = level - prev;
        if (!IsMaxLevel())
        {
            if (nextExp <= 0) nextExp = nextExpUp * level;
            else nextExp += nextExpUp * up;
        }
        else nextExp = 0;

        OnChangeExp?.Invoke(currentExp);

        if (up > 0) PointUp(up);
        if (prev != 0) SoundManager.Instance?.PlaySFX("LevelUp");
    }

    public void ResetLevel()
    {
        level = 0;

        OnChangeLevel?.Invoke(level);

        ResetExp();
        ResetPoint();
    }
    #endregion

    #region 포인트
    public void PointUp(int _point = 1)
    {
        point += _point;

        OnChangePoint?.Invoke(point);
    }

    public void UsePoint()
    {
        point--;

        OnChangePoint?.Invoke(point);
    }

    public void ResetPoint()
    {
        point = 0;

        OnChangePoint?.Invoke(point);
    }
    #endregion

    #region 진행
    public void Pause(bool _pause)
    {
        if (IsPaused == _pause) return;

        IsPaused = _pause;
        Time.timeScale = _pause ? 0f : speed;
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

    #region SET
    public void SetSpeed(float _speed)
    {
        speed = Mathf.Clamp(_speed, minSpeed, maxSpeed);
        if (!IsPaused) Time.timeScale = speed;
        OnChangeSpeed?.Invoke(speed);
    }
    #endregion

    #region GET
    public float GetSpeed() => speed;
    public float GetMinSpeed() => minSpeed;
    public float GetMaxSpeed() => maxSpeed;
    public int GetScore() => score;
    public int GetCurrentExp() => currentExp;
    public int GetNextExp() => nextExp;
    public int GetLevel() => level;
    public bool IsMaxLevel() => level >= maxLevel;
    public int GetPoint() => point;
    #endregion
}
