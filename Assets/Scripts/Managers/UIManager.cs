using System.Collections;
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
    private static readonly string[] units = { "K", "M", "B", "T" };

    [Header("Age UI")]
    [SerializeField] private GameObject ageUI;
    [SerializeField] private Image ageImage;
    [SerializeField] private Image ageOutLine;
    [SerializeField][Min(0f)] private float fadeDuration = 3f;
    private static bool showAge = false;

    [Header("Count UI")]
    [SerializeField] private TextMeshProUGUI countText;
    private Coroutine countRoutine;
    [SerializeField][Min(0)] private int countStart = 3;
    [SerializeField][Min(0f)] private float countDuration = 1f;
    [SerializeField][Min(0f)] private float countScale = 10f;
    [SerializeField] private bool countSkip = true;

    [Header("InGame UI")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private TextMeshProUGUI playTimeText;
    private bool onPlayTime = false;
    private float playTime = 0f;
    [SerializeField] private TextMeshProUGUI scoreNum;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI itemText;
    [SerializeField] private Slider expSlider;
    [SerializeField] private TextMeshProUGUI expText;

    [Header("Setting UI")]
    [SerializeField] private GameObject settingUI;
    [SerializeField] private TextMeshProUGUI settingScoreNum;
    [SerializeField] private Slider speedSlider;
    [SerializeField] private Slider sensSlider;

    [Header("Sound UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Image bgmIcon;
    [SerializeField] private Image sfxIcon;
    [SerializeField] private List<Sprite> bgmIcons = new List<Sprite>();
    [SerializeField] private List<Sprite> sfxIcons = new List<Sprite>();

    [Header("Stat UI")]
    [SerializeField] private GameObject statUI;
    [SerializeField] private TextMeshProUGUI statPointText;
    [SerializeField][NonReorderable] private List<ItemSlot> statItems = new List<ItemSlot>();
    private int prevPoint;

    [Header("Confirm UI")]
    [SerializeField] private GameObject confirmUI;
    [SerializeField] private TextMeshProUGUI confirmTitle;
    private System.Action confirmAction;

    [Header("Result UI")]
    [SerializeField] private GameObject resultUI;
    [SerializeField] private TextMeshProUGUI resultScoreNum;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ageUI == null)
            ageUI = GameObject.Find("AgeUI");
        if (ageImage == null)
            ageImage = GameObject.Find("AgeUI/AgeImage")?.GetComponent<Image>();
        if (ageOutLine == null)
            ageOutLine = GameObject.Find("AgeUI/AgeImage/OutLine")?.GetComponent<Image>();

        if (countText == null)
            countText = GameObject.Find("CountText")?.GetComponent<TextMeshProUGUI>();

        if (inGameUI == null)
            inGameUI = GameObject.Find("InGameUI");
        if (playTimeText == null)
            playTimeText = GameObject.Find("InGameUI/Score/PlayTimeText")?.GetComponent<TextMeshProUGUI>();
        if (scoreNum == null)
            scoreNum = GameObject.Find("InGameUI/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();
        if (levelText == null)
            levelText = GameObject.Find("InGameUI/Level/LevelText")?.GetComponent<TextMeshProUGUI>();
        if (itemText == null)
            itemText = GameObject.Find("InGameUI/Level/ItemText")?.GetComponent<TextMeshProUGUI>();
        if (expSlider == null)
            expSlider = GameObject.Find("InGameUI/Exp/ExpSlider").GetComponentInChildren<Slider>();
        if (expText == null)
            expText = GameObject.Find("InGameUI/Exp/ExpText")?.GetComponent<TextMeshProUGUI>();

        if (settingUI == null)
            settingUI = GameObject.Find("SettingUI");
        if (settingScoreNum == null)
            settingScoreNum = GameObject.Find("SettingUI/Box/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();
        if (speedSlider == null)
            speedSlider = GameObject.Find("Speed/SpeedSlider")?.GetComponent<Slider>();
        if (sensSlider == null)
            sensSlider = GameObject.Find("Sens/SensSlider")?.GetComponent<Slider>();

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

        if (statUI == null)
            statUI = GameObject.Find("StatUI");
        if (statPointText == null)
            statPointText = GameObject.Find("StatUI/Point/PointText").GetComponent<TextMeshProUGUI>();
        if (statItems == null || statItems.Count == 0)
            foreach (Transform child in GameObject.Find("StatUI/Items").transform)
                statItems.Add(new ItemSlot(child.gameObject));

        if (confirmUI == null)
            confirmUI = GameObject.Find("ConfirmUI");
        if (confirmTitle == null)
            confirmTitle = GameObject.Find("ConfirmUI/Box/ConfirmTitle")?.GetComponent<TextMeshProUGUI>();

        if (resultUI == null)
            resultUI = GameObject.Find("ResultUI");
        if (resultScoreNum == null)
            resultScoreNum = GameObject.Find("ResultUI/Score/ScoreNum")?.GetComponent<TextMeshProUGUI>();
    }

    private void LoadSprite(List<Sprite> _list, string _sprite)
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
        UpdatePoint(GameManager.Instance.GetPoint());

        prevPoint = GameManager.Instance.GetPoint();

        if (!showAge)
        {
            showAge = true;
            ageUI.SetActive(true);
            StartCoroutine(FadeCoroutine());
        }
        else ageUI.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance.IsGameOver) return;

        if (onPlayTime)
            onPlayTime = false;
        else
            playTime += Time.unscaledDeltaTime;

        UpdatePlayTime();
        UpdateItemText();
    }

    private void OnEnable()
    {
        GameManager.Instance.OnChangeSpeed += UpdateSpeed;
        speedSlider.minValue = GameManager.Instance.GetMinSpeed();
        speedSlider.maxValue = GameManager.Instance.GetMaxSpeed();
        speedSlider.wholeNumbers = false;
        speedSlider.value = GameManager.Instance.GetSpeed();
        speedSlider.onValueChanged.AddListener(GameManager.Instance.SetSpeed);

        GameManager.Instance.OnChangeScore += UpdateScore;
        GameManager.Instance.OnChangeExp += UpdateExp;
        GameManager.Instance.OnChangeLevel += UpdateLevel;
        GameManager.Instance.OnChangePoint += UpdatePoint;

        SoundManager.Instance.OnChangeVolume += UpdateVolume;
        bgmSlider.value = SoundManager.Instance.GetBGMVolume();
        bgmSlider.onValueChanged.AddListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.value = SoundManager.Instance.GetSFXVolume();
        sfxSlider.onValueChanged.AddListener(SoundManager.Instance.SetSFXVolume);

        HandleManager.Instance.OnChangeSens += UpdateSens;
        sensSlider.minValue = HandleManager.Instance.GetMinSens();
        sensSlider.maxValue = HandleManager.Instance.GetMaxSens();
        sensSlider.wholeNumbers = false;
        sensSlider.value = HandleManager.Instance.GetSens();
        sensSlider.onValueChanged.AddListener(HandleManager.Instance.SetSens);

        OnOpenUI += GameManager.Instance.Pause;
        OnOpenUI += SoundManager.Instance.PauseSFXLoop;
    }

    private void OnDisable()
    {
        GameManager.Instance.OnChangeSpeed -= UpdateSpeed;
        speedSlider.onValueChanged.RemoveListener(GameManager.Instance.SetSpeed);

        GameManager.Instance.OnChangeScore -= UpdateScore;
        GameManager.Instance.OnChangeExp -= UpdateExp;
        GameManager.Instance.OnChangeLevel -= UpdateLevel;
        GameManager.Instance.OnChangePoint -= UpdatePoint;

        SoundManager.Instance.OnChangeVolume -= UpdateVolume;
        bgmSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetBGMVolume);
        sfxSlider.onValueChanged.RemoveListener(SoundManager.Instance.SetSFXVolume);

        HandleManager.Instance.OnChangeSens -= UpdateSens;
        sensSlider.onValueChanged.RemoveListener(HandleManager.Instance.SetSens);

        OnOpenUI -= GameManager.Instance.Pause;
        OnOpenUI -= SoundManager.Instance.PauseSFXLoop;
    }

    #region 기타
    private IEnumerator FadeCoroutine()
    {
        Color imgColor = ageImage.color;
        Color outlineColor = ageOutLine.color;

        imgColor.a = 1f;
        outlineColor.a = 1f;
        ageImage.color = imgColor;
        ageOutLine.color = outlineColor;

        float time = 0f;
        float duration = fadeDuration;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float a = Mathf.Lerp(1f, 0f, t);

            imgColor.a = a;
            outlineColor.a = a;
            ageImage.color = imgColor;
            ageOutLine.color = outlineColor;

            yield return null;
        }

        imgColor.a = 0f;
        outlineColor.a = 0f;
        ageImage.color = imgColor;
        ageOutLine.color = outlineColor;
        ageUI.SetActive(false);
    }

    public void StartCountdown()
    {
        if (countRoutine != null) StopCoroutine(countRoutine);
        countRoutine = StartCoroutine(CountCoroutine());
    }

    private IEnumerator CountCoroutine()
    {
        if (!countSkip)
        {
            GameManager.Instance?.Pause(true);
            SoundManager.Instance?.StopBGM();
            inGameUI.SetActive(false);

            float duration = countDuration;
            float maxScale = countScale;

            countText.gameObject.SetActive(true);

            for (int i = countStart; i > 0; i--)
            {
                countText.text = i.ToString();
                countText.rectTransform.localScale = Vector3.one;

                SoundManager.Instance?.PlaySFX("Count");

                float start = Time.realtimeSinceStartup;

                while (true)
                {
                    float elapsed = Time.realtimeSinceStartup - start;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float scale = 1f + Mathf.Sin(t * Mathf.PI) * (maxScale - 1f);
                    countText.rectTransform.localScale = Vector3.one * scale;

                    if (elapsed >= duration)
                        break;

                    yield return null;
                }
            }
        }

        countText.gameObject.SetActive(false);
        countText.rectTransform.localScale = Vector3.one;

        GameManager.Instance?.Pause(false);
        SoundManager.Instance?.PlayBGM("Default");
        inGameUI.SetActive(true);

        countRoutine = null;
    }

    private string FormatNumber(int _number, bool _full)
    {
        if (_full && _number < 10000)
            return _number.ToString("0000");

        for (int i = units.Length; i > 0; i--)
        {
            float n = Mathf.Pow(1000f, i);
            if (_number >= n)
            {
                float value = _number / n;

                if (value >= 100f)
                    return ((int)value).ToString() + units[i - 1];

                if (value >= 10f)
                {
                    float v10 = Mathf.Floor(value * 10f) / 10f;
                    return v10.ToString("0.0") + units[i - 1];
                }

                float v100 = Mathf.Floor(value * 100f) / 100f;
                return v100.ToString("0.00") + units[i - 1];
            }
        }

        return _full ? _number.ToString("0000") : _number.ToString();
    }
    #endregion

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

        inGameUI.SetActive(!_on);
        settingUI.SetActive(_on);

        OnOpenUI?.Invoke(_on);
    }

    public void OpenStat(bool _on)
    {
        if (statUI == null) return;

        inGameUI.SetActive(!_on);
        statUI.SetActive(_on);

        OnOpenUI?.Invoke(_on);

        var datas = EntityManager.Instance?.GetItemDatas();
        for (int i = 0; i < statItems.Count; i++)
        {
            var item = datas[i];

            statItems[i].go.name = item.Name;
            statItems[i].image.sprite = item.Image;

            UpdateStat(i, item);

            int idx = i;
            statItems[idx].upBtn.onClick.RemoveAllListeners();
            statItems[idx].upBtn.onClick.AddListener(SoundManager.Instance.Button);
            statItems[idx].upBtn.onClick.AddListener(() => OnClickStatUp(idx));
        }

        if (GameManager.Instance.IsMaxLevel())
            levelText.text = "MAX";
        else
            levelText.text = GameManager.Instance.GetLevel().ToString("'LV.'00");

        itemText.text = string.Empty;
    }

    public void OpenConfirm(bool _on, string _text = null, System.Action _action = null, bool _pass = false)
    {
        if (confirmUI == null) return;

        if (!_pass)
        {
            confirmUI.SetActive(_on);
            if (_on)
            {
                confirmTitle.text = $"{_text}하시겠습니까?";
                confirmAction = _action;
            }
        }

        if (!_on)
        {
            confirmTitle.text = string.Empty;
            confirmAction = null;
        }

        if (_pass) _action?.Invoke();
    }

    public void OpenResult(bool _on)
    {
        if (resultUI == null) return;

        inGameUI.SetActive(!_on);
        resultUI.SetActive(_on);

        OnOpenUI?.Invoke(_on);
    }
    #endregion

    #region 업데이트
    public void ResetUI()
    {
        playTime = 0f;
        onPlayTime = true;
        UpdatePlayTime();
    }

    public void UpdateSpeed(float _speed)
    {
        if (!Mathf.Approximately(speedSlider.value, _speed))
            speedSlider.value = _speed;
    }

    public void UpdateSens(float _sens)
    {
        if (!Mathf.Approximately(sensSlider.value, _sens))
            sensSlider.value = _sens;
    }

    public void UpdatePlayTime()
    {
        int total = Mathf.FloorToInt(playTime);
        string s = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        playTimeText.text = s;
    }

    public void UpdateScore(int _score)
    {
        string s = FormatNumber(_score, true);
        scoreNum.text = s;
        settingScoreNum.text = s;
        resultScoreNum.text = s;
    }

    public void UpdateExp(int _currentExp)
    {
        if (GameManager.Instance.IsMaxLevel())
        {
            expText.gameObject.SetActive(false);
            expSlider.gameObject.SetActive(false);
            return;
        }

        int nextExp = GameManager.Instance.GetNextExp();
        expText.gameObject.SetActive(true);
        expText.text = $"{_currentExp} / {nextExp}";
        expSlider.gameObject.SetActive(true);
        expSlider.maxValue = nextExp;
        expSlider.value = Mathf.Clamp(nextExp - _currentExp, 0, nextExp);
    }

    public void UpdateLevel(int _level)
    {
        bool newItem = false;
        var datas = EntityManager.Instance?.GetItemDatas();
        if (datas != null)
        {
            for (int i = 0; i < datas.Count; i++)
            {
                var d = datas[i];
                if (d != null && d.Level == _level && d.Level != 1)
                {
                    newItem = true;
                    break;
                }
            }
        }

        bool isMax = GameManager.Instance.IsMaxLevel();

        levelText.text = isMax ? "MAX" : _level.ToString("'LV.'00");
        levelText.color = isMax ? Color.green : Color.white;

        itemText.text = newItem ? "NEW" : itemText.text;

        if (statUI.activeSelf)
            for (int i = 0; i < statItems.Count; i++)
                UpdateStat(i, datas[i]);
    }

    public void UpdateItemText()
    {
        float s = Mathf.PingPong(playTime * 4f, 1f);
        if (itemText.text == "NEW")
        {
            Color c = Color.red;
            itemText.color = Color.Lerp(c, Color.white, s);
        }
        else if (itemText.text == "UP")
        {
            Color c = Color.blue;
            itemText.color = Color.Lerp(c, Color.white, s);
        }
    }

    public void UpdatePoint(int _point)
    {
        string p = _point.ToString("SP : 00");
        statPointText.text = p;

        if (statUI.activeSelf)
        {
            var datas = EntityManager.Instance?.GetItemDatas();
            for (int i = 0; i < statItems.Count; i++)
                UpdateStat(i, datas[i]);
        }

        bool pointUp = (_point > prevPoint);

        if (pointUp && itemText.text != "NEW")
            itemText.text = "UP";

        prevPoint = _point;
    }

    public void UpdateVolume(SoundType _type, float _volume)
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
        UpdateSoundIcon();
    }

    public void UpdateSoundIcon()
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

    public void UpdateStat(int _index, ItemData _item)
    {
        if (_item.MaxStat > 0 && _item.Stat >= _item.MaxStat)
        {
            statItems[_index].stat.text = "MAX";
            statItems[_index].stat.color = Color.green;
        }
        else
        {
            statItems[_index].stat.text = _item.Stat.ToString();
            statItems[_index].stat.color = Color.white;
        }

        bool canUp =
            (GameManager.Instance?.GetLevel() >= _item.Level) &&
            (GameManager.Instance?.GetPoint() > 0) &&
            (_item.MaxStat == 0 || _item.Stat < _item.MaxStat);

        statItems[_index].upBtn.interactable = canUp;
        statItems[_index].upBtnText.color = canUp ? Color.blue : new Color32(200, 200, 200, 200);

        statItems[_index].go.SetActive(_item.Level <= GameManager.Instance?.GetLevel());
    }
    #endregion

    #region 버튼
    public void OnClickSetting() => OpenSetting(true);

    public void OnClickClose() => OpenUI(false);
    public void OnClickSpeed()
    {
        if (speedSlider.value != 1f)
            speedSlider.value = 1f;
        else
            speedSlider.value = speedSlider.maxValue;
    }
    public void OnClickSens()
    {
        if (sensSlider.value != 1f)
            sensSlider.value = 1f;
        else
            sensSlider.value = sensSlider.maxValue;
    }
    public void OnClickBGM() => SoundManager.Instance?.ToggleBGM();
    public void OnClickSFX() => SoundManager.Instance?.ToggleSFX();

    public void OnClickStat() => OpenStat(true);
    public void OnClickStatUp(int _index) => EntityManager.Instance?.GetItemDatas()[_index].StatUp();
    public void OnClickReset() => OpenConfirm(true, "초기화", EntityManager.Instance.Reset);

    public void OnClickReplay() => OpenConfirm(true, "다시", GameManager.Instance.Replay);
    public void OnClickQuit() => OpenConfirm(true, "종료", GameManager.Instance.Quit);

    public void OnClickOkay()
    {
        var action = confirmAction;
        OpenConfirm(false);
        action?.Invoke();
    }
    public void OnClickCancel() => OpenConfirm(false);

    public void OnClickReplayDirect() => OpenConfirm(true, "다시", GameManager.Instance.Replay, true);
    public void OnClickQuitDirect() => OpenConfirm(true, "종료", GameManager.Instance.Quit, true);
    #endregion

    #region SET
    public void SetCountdown(bool _skip) => countSkip = _skip;
    public void SetInGameUI(float _margin)
    {
        var rt = inGameUI.GetComponent<RectTransform>();
        rt.offsetMax = new Vector3(rt.offsetMax.x, -_margin);
    }
    #endregion

    #region GET
    public bool GetOnSetting() => settingUI.activeSelf;
    public bool GetOnStat() => statUI.activeSelf;
    public bool GetOnConfirm() => confirmUI.activeSelf;
    public bool GetOnResult() => resultUI.activeSelf;
    #endregion
}
