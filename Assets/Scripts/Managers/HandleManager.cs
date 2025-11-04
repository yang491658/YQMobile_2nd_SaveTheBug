using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandleManager : MonoBehaviour
{
    static public HandleManager Instance { private set; get; }

    private Camera cam => Camera.main;
    private float time;

    [Header("Entity")]
    [SerializeField] private Player player;

    [Header("Click")]
    private const float doubleClick = 0.25f;
    private bool isDoubleClick;

    [Header("Drag")]
    [SerializeField][Min(0f)] private float maxDrag = 5f;
    private const float drag = 0.15f;
    private bool isDragging;
    private Vector3 dragStart;
    private Vector3 dragCurrent;
    private bool isOverUI;

    [Header("Sensitivity")]
    [SerializeField][Min(0.5f)] private float sens = 1f;
    [SerializeField][Min(0.5f)] private float minSens = 0.5f;
    [SerializeField][Min(0.5f)] private float maxSens = 3f;

    [Header("Aim")]
    [SerializeField] private bool aimVisible = false;
    [SerializeField] private Transform ring;
    [SerializeField] private Transform handle;

#if UNITY_EDITOR
    [Header("Mark")]
    [SerializeField] private float markDuration = 1f;
    [SerializeField] private float markRadius = 0.5f;
    [SerializeField] private int markSegment = 24;
    private readonly List<Vector3> marks = new();
    private readonly List<float> markTimes = new();
    private readonly List<Color> markColors = new();

    private readonly List<Vector3> dragPath = new();
#endif

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ring == null) ring = GameObject.Find("AimRing")?.transform;
        if (handle == null) handle = GameObject.Find("AimCircle")?.transform;
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

        SetHandle();
    }

    private void Update()
    {
        if (GameManager.Instance.IsPaused) return;

#if UNITY_EDITOR
        HandleMouse();
        DrawDebug();
#else
        HandleTouch();
#endif
    }

#if UNITY_EDITOR
    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0)) HandleBegin(Input.mousePosition);
        else if (Input.GetMouseButton(0)) HandleMove(Input.mousePosition);
        else if (Input.GetMouseButtonUp(0)) HandleEnd(Input.mousePosition);

        if (Input.GetMouseButtonDown(1)) OnRightClick(ScreenToWorld(Input.mousePosition));
        if (Input.GetMouseButtonDown(2)) OnMiddleClick(ScreenToWorld(Input.mousePosition));
    }
#endif

    private void HandleTouch()
    {
        if (Input.touchCount == 0) return;
        Touch t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began && !IsOverUI(t.fingerId))
            HandleBegin(t.position, t.fingerId);
        else if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            HandleMove(t.position);
        else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            HandleEnd(t.position);
    }

    private bool IsOverUI(int _fingerID = -1)
        => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(_fingerID);

    private Vector3 ScreenToWorld(Vector3 _screenPos)
    {
        var p = _screenPos;
        p.z = -cam.transform.position.z;
        return cam.ScreenToWorldPoint(p);
    }

    #region 구분
    private void HandleBegin(Vector3 _pos, int _fingerID = -1)
    {
        if (IsOverUI(_fingerID))
        {
            isOverUI = true;
            return;
        }
        else isOverUI = false;

        Vector3 worldPos = ScreenToWorld(_pos);

        isDragging = false;
        dragStart = worldPos;
        dragCurrent = dragStart;
#if UNITY_EDITOR
        dragPath.Clear();
        dragPath.Add(dragStart);
#endif
    }

    private void HandleMove(Vector3 _pos)
    {
        if (isOverUI) return;

        Vector3 worldPos = ScreenToWorld(_pos);
        float distance = Vector3.Distance(dragStart, worldPos);

        if (!isDragging && distance >= drag)
        {
            isDragging = true;
            OnDragBegin(dragStart);
        }

        if (isDragging)
        {
            dragCurrent = ClampDrag(dragStart, worldPos);
            OnDragMove(dragStart, dragCurrent);
#if UNITY_EDITOR
            dragPath.Add(dragCurrent);
#endif
        }
    }

    private void HandleEnd(Vector3 _pos)
    {
        if (isOverUI)
        {
            isOverUI = false;
            return;
        }

        Vector3 worldPos = ScreenToWorld(_pos);

        if (isDragging)
        {
            worldPos = ClampDrag(dragStart, worldPos);
            float distance = Vector3.Distance(dragStart, worldPos);
            if (distance >= drag)
            {
                isDragging = false;
                OnDragEnd(dragStart, worldPos);
#if UNITY_EDITOR
                dragPath.Add(worldPos);
                dragPath.Clear();
#endif
                return;
            }
        }

        if (Time.time - time < doubleClick)
        {
            isDoubleClick = false;
            time = 0;
            OnDouble(worldPos);
        }
        else
        {
            isDoubleClick = true;
            time = Time.time;
            StartCoroutine(ClickCoroutine(worldPos));
        }

        isDragging = false;
#if UNITY_EDITOR
        dragPath.Clear();
#endif
    }

    private Vector3 ClampDrag(Vector3 _start, Vector3 _current)
    {
        if (maxDrag <= 0f) return _current;
        Vector3 delta = _current - _start;
        return _start + Vector3.ClampMagnitude(delta, maxDrag);
    }

    private IEnumerator ClickCoroutine(Vector3 _pos)
    {
        yield return new WaitForSeconds(doubleClick);
        if (isDoubleClick)
        {
            isDoubleClick = false;
            OnSingle(_pos);
        }
    }
    #endregion

    #region 동작
    private void OnSingle(Vector3 _pos)
    {
        Debug.Log($"단순 터치 : {_pos}"); // TODO 단순 터치 동작
#if UNITY_EDITOR
        AddClick(_pos, Color.cyan);
#endif
    }

    private void OnDouble(Vector3 _pos)
    {
        Debug.Log($"더블 터치 : {_pos}"); // TODO 더블 터치 동작
#if UNITY_EDITOR
        AddClick(_pos, Color.blue);
#endif
    }

    private void OnDragBegin(Vector3 _pos)
    {
        if (aimVisible)
        {
            ring.gameObject.SetActive(true);
            ring.position = _pos;
            ring.localScale = Vector3.zero;

            handle.gameObject.SetActive(true);
            handle.position = _pos;
        }
    }

    private void OnDragMove(Vector3 _start, Vector3 _current)
    {
        if (aimVisible)
        {
            float scale = 2f * Vector3.Distance(_start, _current);
            ring.localScale = new Vector3(scale, scale, scale) + handle.localScale;
            handle.position = _current;
        }

        player.Move((_current - _start) * sens);
    }

    private void OnDragEnd(Vector3 _start, Vector3 _end)
    {
        if (aimVisible)
        {
            ring.gameObject.SetActive(false);
            handle.gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnRightClick(Vector3 _pos)
    {
        Debug.Log($"우클릭 : {_pos}"); // TODO 우클릭 동작
        AddClick(_pos, Color.yellow);

        Collider2D hit = Physics2D.OverlapPoint(_pos, LayerMask.GetMask("Item"));
        if (hit != null)
        {
            Item item = hit.GetComponent<Item>();
            if (item != null) item.UseItem();
        }
    }

    private void OnMiddleClick(Vector3 _pos)
    {
        Debug.Log($"휠클릭 : {_pos}"); // TODO 휠클릭 동작
        AddClick(_pos, Color.red);
    }

    private void AddClick(Vector3 _pos, Color _color)
    {
        marks.Add(_pos);
        markTimes.Add(Time.time + markDuration);
        markColors.Add(_color);
    }

    private void DrawDebug()
    {
        for (int i = markTimes.Count - 1; i >= 0; i--)
        {
            if (Time.time > markTimes[i])
            {
                int last = markTimes.Count - 1;
                (markTimes[i], markTimes[last]) = (markTimes[last], markTimes[i]);
                (marks[i], marks[last]) = (marks[last], marks[i]);
                (markColors[i], markColors[last]) = (markColors[last], markColors[i]);
                markTimes.RemoveAt(last);
                marks.RemoveAt(last);
                markColors.RemoveAt(last);
                continue;
            }

            Vector3 center = marks[i];
            Color c = markColors[i];
            for (int s = 0; s < markSegment; s++)
            {
                float a0 = (Mathf.PI * 2f) * s / markSegment;
                float a1 = (Mathf.PI * 2f) * (s + 1) / markSegment;
                Vector3 p0 = center + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0)) * markRadius;
                Vector3 p1 = center + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1)) * markRadius;
                Debug.DrawLine(p0, p1, c);
            }
        }

        if (isDragging)
        {
            Debug.DrawLine(dragStart, dragCurrent, Color.green);

            for (int i = 1; i < dragPath.Count; i++)
                Debug.DrawLine(dragPath[i - 1], dragPath[i], Color.magenta);
        }
    }
#endif
    #endregion

    #region SET
    public void SetHandle()
    {
        if (player == null)
            player = EntityManager.Instance?.GetPlayer();

        ring.gameObject.SetActive(false);
        handle.gameObject.SetActive(false);
    }
    public void SetSens(float _value)
        => sens = Mathf.Clamp(_value, minSens, maxSens);
    #endregion

    #region GET
    public float GetSens() => sens;
    public float GetMinSens() => minSens;
    public float GetMaxSens() => maxSens;
    #endregion
}
