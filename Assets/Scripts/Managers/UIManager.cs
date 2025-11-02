using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public struct ItemSlot
{
    public GameObject go;
    public Image image;
    public TextMeshProUGUI stat;
    public Button upBtn;
    public TextMeshProUGUI upBtnText;

    public ItemSlot(GameObject obj)
    {
        go = obj;
        image = null;
        for (int i = 0; i < obj.transform.childCount; i++)
        {
            var img = obj.transform.GetChild(i).GetComponent<Image>();
            if (img != null) { image = img; break; }
        }

        stat = obj.GetComponentInChildren<TextMeshProUGUI>();
        upBtn = obj.GetComponentInChildren<Button>();
        upBtnText = upBtn.GetComponentInChildren<TextMeshProUGUI>();
    }
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { private set; get; }

    public event System.Action<bool> OnOpenUI;

    [Header("InGame UI")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private TextMeshProUGUI playTimeText;
    private float playTime = 0f;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Slider expSlider;

    [Header("Setting UI")]
    [SerializeField] private GameObject settingUI;
    [SerializeField] private TextMeshProUGUI settingScoreText;

    [Header("Sound UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Image bgmIcon;
    [SerializeField] private Image sfxIcon;
    [SerializeField] private List<Sprite> bgmIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> sfxIcons = new List<Sprite>();

    [Header("Confirm UI")]
    [SerializeField] private GameObject confirmUI;
    [SerializeField] private TextMeshProUGUI confirmText;
    private System.Action confirmAction;

    [Header("Stat UI")]
    [SerializeField] private GameObject statUI;
    [SerializeField] private TextMeshProUGUI statLevelText;
    [SerializeField] private TextMeshProUGUI statPointText;
    [SerializeField] private List<ItemSlot> statItems = new List<ItemSlot>();

    [Header("Result UI")]
    [SerializeField] private GameObject resultUI;
    [SerializeField] private TextMeshProUGUI resultScoreText;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (inGameUI == null)
            inGameUI = GameObject.Find("InGameUI");
        if (playTimeText == null)
            playTimeText = GameObject.Find("InGameUI/Score/PlayTimeText")?.GetComponent<TextMeshProUGUI>();
        if (scoreText == null)
            scoreText = GameObject.Find("InGameUI/Score/ScoreText")?.GetComponent<TextMeshProUGUI>();
        if (levelText == null)
            levelText = GameObject.Find("InGameUI/Level/LevelText")?.GetComponent<TextMeshProUGUI>();
        if (expSlider == null)
            expSlider = GameObject.Find("InGameUI/ExpSlider").GetComponentInChildren<Slider>();

        if (settingUI == null)
            settingUI = GameObject.Find("SettingUI");
        if (settingScoreText == null)
            settingScoreText = GameObject.Find("SettingUI/Box/Score/ScoreText")?.GetComponent<TextMeshProUGUI>();

        if (bgmSlider == null)
            bgmSlider = GameObject.Find("BGM/BgmSlider")?.GetComponent<Slider>();
        if (sfxSlider == null)
            sfxSlider = GameObject.Find("SFX/SfxSlider")?.GetComponent<Slider>();
        if (bgmIcon == null)
            bgmIcon = GameObject.Find("BGM/BgmBtn/BgmIcon")?.GetComponent<Image>();
        if (sfxIcon == null)
            sfxIcon = GameObject.Find("SFX/SfxBtn/SfxIcon")?.GetComponent<Image>();

        bgmIcons.Clear();
        LoadSprite(bgmIcons, "White Music");
        LoadSprite(bgmIcons, "White Music Off");
        sfxIcons.Clear();
        LoadSprite(sfxIcons, "White Sound On");
        LoadSprite(sfxIcons, "White Sound Icon");
        LoadSprite(sfxIcons, "White Sound Off 2");

        if (confirmUI == null)
            confirmUI = GameObject.Find("ConfirmUI");
        if (confirmText == null)
            confirmText = GameObject.Find("ConfirmUI/Box/ConfirmText")?.GetComponent<TextMeshProUGUI>();

        if (statUI == null)
            statUI = GameObject.Find("StatUI");
        if (statLevelText == null)
            statLevelText = GameObject.Find("StatUI/Level/LevelText").GetComponent<TextMeshProUGUI>();
        if (statPointText == null)
            statPointText = GameObject.Find("StatUI/Point/PointText").GetComponent<TextMeshProUGUI>();
        if (statItems == null || statItems.Count == 0)
            foreach (Transform child in GameObject.Find("StatUI/Items").transform)
                statItems.Add(new ItemSlot(child.gameObject));

        if (resultUI == null)
            resultUI = GameObject.Find("ResultUI");
        if (resultScoreText == null)
            resultScoreText = GameObject.Find("ResultUI/Score/ScoreText")?.GetComponent<TextMeshProUGUI>();
    }

    private static void LoadSprite(List<Sprite> _list, string _sprite)
    {
        if (string.IsNullOrEmpty(_sprite)) return;
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Imports/Dark UI/Icons" });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var obj in assets)
            {
                var s = obj as Sprite;
                if (s != null && s.name == _sprite)
                {
                    _list.Add(s);
                    return;
                }
            }
        }
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

    private void Start()
    {
        UpdateScore(GameManager.Instance.GetScore());
        UpdateExp(GameManager.Instance.GetCurrentExp());
        UpdateLevel(GameManager.Instance.GetLevel());
        UpdatePoint(GameManager.Instance.GetStatPoint());
    }

    private void Update()
    {
        if (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver) return;

        playTime += Time.deltaTime;
        UpdatePlayTime();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnChangeScore += UpdateScore;
        GameManager.Instance.OnChangeExp += UpdateExp;
        GameManager.Instance.OnChangeLevel += UpdateLevel;
        GameManager.Instance.OnChangePoint += UpdatePoint;

        SoundManager.Instance.OnChangeVolume += UpdateVolume;
        bgmSlider.value = SoundManager.Instance.GetBGMVolume();
        bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.value = SoundManager.Instance.GetSFXVolume();
        sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);

        OnOpenUI += GameManager.Instance.Pause;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnChangeScore -= UpdateScore;
        GameManager.Instance.OnChangeExp -= UpdateExp;
        GameManager.Instance.OnChangeLevel -= UpdateLevel;
        GameManager.Instance.OnChangePoint -= UpdatePoint;

        SoundManager.Instance.OnChangeVolume -= UpdateVolume;
        bgmSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetSFXVolume);

        OnOpenUI -= GameManager.Instance.Pause;
    }

    #region 오픈
    public void OpenUI(bool _on)
    {
        OpenResult(_on);
        OpenStat(_on);
        OpenConfirm(_on);
        OpenSetting(_on);
    }

    public void OpenSetting(bool _on)
    {
        if (settingUI == null) return;

        OnOpenUI?.Invoke(_on);

        inGameUI.SetActive(!_on);

        settingUI.SetActive(_on);
    }

    public void OpenConfirm(bool _on, string _text = null, System.Action _action = null, bool _pass = false)
    {
        if (confirmUI == null) return;

        if (!_pass)
        {
            confirmUI.SetActive(_on);
            confirmText.text = $"{_text}하시겠습니까?";
            confirmAction = _action;
        }

        if (!_on) confirmAction = null;

        if (_pass) _action?.Invoke();
    }

    public void OpenStat(bool _on)
    {
        if (statUI == null) return;

        OnOpenUI?.Invoke(_on);

        inGameUI.SetActive(!_on);
        settingUI.SetActive(!_on);
        confirmUI.SetActive(!_on);

        statUI.SetActive(_on);

        var datas = EntityManager.Instance?.GetDatas();
        for (int i = 0; i < statItems.Count; i++)
        {
            var item = datas[i];

            statItems[i].go.name = item.Name;
            statItems[i].image.sprite = item.Image;
            statItems[i].stat.text = item.Stat.ToString();

            UpdateStat(i, item);

            int idx = i;
            statItems[idx].upBtn.onClick.RemoveAllListeners();
            statItems[idx].upBtn.onClick.AddListener(SoundManager.Instance.Button);
            statItems[idx].upBtn.onClick.AddListener(() => OnClickStatUp(idx));
        }
    }

    public void OpenResult(bool _on)
    {
        if (resultUI == null) return;

        OnOpenUI?.Invoke(_on);

        inGameUI.SetActive(!_on);
        settingUI.SetActive(!_on);
        confirmUI.SetActive(!_on);
        statUI.SetActive(!_on);

        resultUI.SetActive(_on);
    }
    #endregion

    #region 업데이트
    public void ResetPlayTime() => playTime = 0;

    private void UpdatePlayTime()
    {
        int total = Mathf.FloorToInt(playTime);
        string s = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        playTimeText.text = s;
    }

    public void UpdateScore(int _score)
    {
        string s = _score.ToString("0000");
        scoreText.text = s;
        settingScoreText.text = s;
        resultScoreText.text = s;
    }

    public void UpdateExp(int _cur)
    {
        int next = GameManager.Instance.GetNextExp();
        if (next <= 0)
        {
            expSlider.maxValue = 1f;
            expSlider.value = 1f;
            return;
        }

        expSlider.maxValue = next;
        expSlider.value = Mathf.Clamp(next - _cur, 0, next);
    }

    public void UpdateLevel(int _level)
    {
        string l = _level.ToString("'LV.'00");
        levelText.text = l;
        statLevelText.text = l;

        if (statUI.activeSelf)
        {
            var datas = EntityManager.Instance?.GetDatas();
            for (int i = 0; i < statItems.Count; i++)
                UpdateStat(i, datas[i]);
        }
    }

    public void UpdatePoint(int _point)
    {
        string p = _point.ToString("SP : 00");
        statPointText.text = p;

        if (statUI.activeSelf)
        {
            var datas = EntityManager.Instance?.GetDatas();
            for (int i = 0; i < statItems.Count; i++)
                UpdateStat(i, datas[i]);
        }
    }

    private void UpdateVolume(SoundType _type, float _volume)
    {
        switch (_type)
        {
            case SoundType.BGM:
                if (!Mathf.Approximately(bgmSlider.value, _volume))
                    bgmSlider.value = _volume;
                break;

            case SoundType.SFX:
                if (!Mathf.Approximately(sfxSlider.value, _volume))
                    sfxSlider.value = _volume;
                break;

            default:
                return;
        }
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (bgmIcons.Count >= 2)
            bgmIcon.sprite = SoundManager.Instance.IsBGMMuted() ? bgmIcons[1] : bgmIcons[0];

        if (sfxIcons.Count >= 3)
        {
            if (SoundManager.Instance.IsSFXMuted())
                sfxIcon.sprite = sfxIcons[2];
            else if (SoundManager.Instance?.GetSFXVolume() < 0.2f)
                sfxIcon.sprite = sfxIcons[1];
            else
                sfxIcon.sprite = sfxIcons[0];
        }
    }

    private void UpdateStat(int _index, ItemData _item)
    {
        statItems[_index].stat.text = _item.Stat.ToString();

        if (_item.MaxStat > 0 && _item.Stat >= _item.MaxStat)
            statItems[_index].stat.color = Color.blue;
        else
            statItems[_index].stat.color = Color.white;

        bool canUp =
            (GameManager.Instance?.GetLevel() >= _item.Level) &&
            (GameManager.Instance?.GetStatPoint() > 0) &&
            (_item.MaxStat == 0 || _item.Stat < _item.MaxStat);

        statItems[_index].upBtn.interactable = canUp;
        statItems[_index].upBtnText.color = canUp ? Color.blue : new Color(0.5f, 0.5f, 0.5f, 0.5f);
    }
    #endregion

    #region 버튼
    public void OnClickSetting() => OpenSetting(true);
    public void OnClickClose() => OpenUI(false);

    public void OnClickBGM() => SoundManager.Instance?.ToggleBGM();
    public void OnClickSFX() => SoundManager.Instance?.ToggleSFX();

    public void OnClickReplay() => OpenConfirm(true, "다시", GameManager.Instance.Replay);
    public void OnClickQuit() => OpenConfirm(true, "종료", GameManager.Instance.Quit);

    public void OnClickReplayByPass() => OpenConfirm(true, "다시", GameManager.Instance.Replay, true);
    public void OnClickQuitByPass() => OpenConfirm(true, "종료", GameManager.Instance.Quit, true);

    public void OnClickOkay() => confirmAction?.Invoke();
    public void OnClickCancel() => OpenConfirm(false);

    public void OnClickStat() => OpenStat(true);
    private void OnClickStatUp(int _index)
    {
        var item = EntityManager.Instance?.GetDatas()[_index];
        item.StatUp();
        UpdateStat(_index, item);
    }
    #endregion

    #region SET
    public void SetInGameUI(float _margin)
    {
        var rt = inGameUI.GetComponent<RectTransform>();
        rt.offsetMax = new Vector3(rt.offsetMax.x, -_margin);
    }
    #endregion

    #region GET
    public bool GetOnSetting() => settingUI.activeSelf;
    public bool GetOnConfirm() => confirmUI.activeSelf;
    public bool GetOnStat() => statUI.activeSelf;
    public bool GetOnResult() => resultUI.activeSelf;
    #endregion
}
