using UnityEngine;
using System.Collections;

public class ModelScaler : MonoBehaviour
{
    [Header("缩放设置")]
    [SerializeField]
    [Tooltip("最小缩放比例")]
    private float minScale = 0.1f;

    [SerializeField]
    [Tooltip("最大缩放比例")]
    private float maxScale = 3.0f;

    [SerializeField]
    [Tooltip("默认缩放比例")]
    private float defaultScale = 1.0f;

    [SerializeField]
    [Tooltip("缩放速度")]
    private float scaleSpeed = 2.0f;

    [SerializeField]
    [Tooltip("是否启用手势缩放")]
    private bool enablePinchToZoom = true;

    [SerializeField]
    [Tooltip("是否启用平滑缩放")]
    private bool smoothScaling = true;

    [SerializeField]
    [Tooltip("平滑缩放时间")]
    private float smoothTime = 0.2f;

    [Header("UI控制（可选）")]
    [SerializeField]
    [Tooltip("放大按钮（可选）")]
    private UnityEngine.UI.Button zoomInButton;

    [SerializeField]
    [Tooltip("缩小按钮（可选）")]
    private UnityEngine.UI.Button zoomOutButton;

    [SerializeField]
    [Tooltip("重置按钮（可选）")]
    private UnityEngine.UI.Button resetButton;

    private Vector3 originalScale;
    private float currentScale;
    private Vector3 targetScale;
    private Coroutine scaleCoroutine;

    void Start()
    {
        // 保存原始缩放
        originalScale = transform.localScale;
        currentScale = defaultScale;
        targetScale = originalScale * currentScale;

        // 应用初始缩放
        if (smoothScaling)
            transform.localScale = targetScale;
        else
            transform.localScale = originalScale * defaultScale;

        // 设置按钮事件
        SetupButtonEvents();
    }

    void Update()
    {
        // 手势缩放检测
        if (enablePinchToZoom)
        {
            HandlePinchToZoom();
        }

        // 平滑缩放
        if (smoothScaling && transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime / smoothTime);
        }
    }

    /// <summary>
    /// 处理双指缩放手势
    /// </summary>
    void HandlePinchToZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // 找到触摸点在前一帧和当前帧的位置
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // 计算前一帧和当前帧触摸点之间的距离
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // 计算距离差异
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            // 应用缩放
            ScaleModel(deltaMagnitudeDiff * 0.01f * scaleSpeed);
        }
    }

    /// <summary>
    /// 设置按钮事件
    /// </summary>
    void SetupButtonEvents()
    {
        if (zoomInButton != null)
            zoomInButton.onClick.AddListener(ZoomIn);

        if (zoomOutButton != null)
            zoomOutButton.onClick.AddListener(ZoomOut);

        if (resetButton != null)
            resetButton.onClick.AddListener(ResetScale);
    }

    /// <summary>
    /// 缩放模型
    /// </summary>
    /// <param name="scaleDelta">缩放变化量</param>
    public void ScaleModel(float scaleDelta)
    {
        float newScale = Mathf.Clamp(currentScale + scaleDelta, minScale, maxScale);

        if (newScale != currentScale)
        {
            currentScale = newScale;
            targetScale = originalScale * currentScale;

            if (!smoothScaling)
            {
                transform.localScale = targetScale;
            }
        }
    }

    /// <summary>
    /// 放大模型
    /// </summary>
    public void ZoomIn()
    {
        ScaleModel(0.1f * scaleSpeed);
    }

    /// <summary>
    /// 缩小模型
    /// </summary>
    public void ZoomOut()
    {
        ScaleModel(-0.1f * scaleSpeed);
    }

    /// <summary>
    /// 重置缩放
    /// </summary>
    public void ResetScale()
    {
        currentScale = defaultScale;
        targetScale = originalScale * defaultScale;

        if (smoothScaling)
        {
            if (scaleCoroutine != null)
                StopCoroutine(scaleCoroutine);
            scaleCoroutine = StartCoroutine(SmoothScaleToTarget());
        }
        else
        {
            transform.localScale = targetScale;
        }
    }

    /// <summary>
    /// 设置特定缩放比例
    /// </summary>
    /// <param name="scale">目标缩放比例</param>
    public void SetScale(float scale)
    {
        float newScale = Mathf.Clamp(scale, minScale, maxScale);
        currentScale = newScale;
        targetScale = originalScale * newScale;

        if (!smoothScaling)
        {
            transform.localScale = targetScale;
        }
    }

    /// <summary>
    /// 平滑缩放到目标尺寸的协程
    /// </summary>
    IEnumerator SmoothScaleToTarget()
    {
        float elapsedTime = 0;
        Vector3 startingScale = transform.localScale;

        while (elapsedTime < smoothTime)
        {
            transform.localScale = Vector3.Lerp(startingScale, targetScale, elapsedTime / smoothTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;
    }

    /// <summary>
    /// 获取当前缩放比例
    /// </summary>
    public float GetCurrentScale()
    {
        return currentScale;
    }

    /// <summary>
    /// 获取缩放百分比（0-1）
    /// </summary>
    public float GetScalePercentage()
    {
        return (currentScale - minScale) / (maxScale - minScale);
    }

    /// <summary>
    /// 启用/禁用手势缩放
    /// </summary>
    public void SetPinchToZoomEnabled(bool enabled)
    {
        enablePinchToZoom = enabled;
    }

    /// <summary>
    /// 设置缩放限制
    /// </summary>
    public void SetScaleLimits(float min, float max)
    {
        minScale = Mathf.Max(0.01f, min);
        maxScale = Mathf.Max(minScale + 0.1f, max);
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
        targetScale = originalScale * currentScale;
    }

    // 公共属性
    public float MinScale { get => minScale; }
    public float MaxScale { get => maxScale; }
    public float DefaultScale { get => defaultScale; }
}