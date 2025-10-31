using UnityEngine;
using System.Collections;

public class ModelScaler : MonoBehaviour
{
    [Header("��������")]
    [SerializeField]
    [Tooltip("��С���ű���")]
    private float minScale = 0.1f;

    [SerializeField]
    [Tooltip("������ű���")]
    private float maxScale = 3.0f;

    [SerializeField]
    [Tooltip("Ĭ�����ű���")]
    private float defaultScale = 1.0f;

    [SerializeField]
    [Tooltip("�����ٶ�")]
    private float scaleSpeed = 2.0f;

    [SerializeField]
    [Tooltip("�Ƿ�������������")]
    private bool enablePinchToZoom = true;

    [SerializeField]
    [Tooltip("�Ƿ�����ƽ������")]
    private bool smoothScaling = true;

    [SerializeField]
    [Tooltip("ƽ������ʱ��")]
    private float smoothTime = 0.2f;

    [Header("UI���ƣ���ѡ��")]
    [SerializeField]
    [Tooltip("�Ŵ�ť����ѡ��")]
    private UnityEngine.UI.Button zoomInButton;

    [SerializeField]
    [Tooltip("��С��ť����ѡ��")]
    private UnityEngine.UI.Button zoomOutButton;

    [SerializeField]
    [Tooltip("���ð�ť����ѡ��")]
    private UnityEngine.UI.Button resetButton;

    private Vector3 originalScale;
    private float currentScale;
    private Vector3 targetScale;
    private Coroutine scaleCoroutine;

    void Start()
    {
        // ����ԭʼ����
        originalScale = transform.localScale;
        currentScale = defaultScale;
        targetScale = originalScale * currentScale;

        // Ӧ�ó�ʼ����
        if (smoothScaling)
            transform.localScale = targetScale;
        else
            transform.localScale = originalScale * defaultScale;

        // ���ð�ť�¼�
        SetupButtonEvents();
    }

    void Update()
    {
        // �������ż��
        if (enablePinchToZoom)
        {
            HandlePinchToZoom();
        }

        // ƽ������
        if (smoothScaling && transform.localScale != targetScale)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime / smoothTime);
        }
    }

    /// <summary>
    /// ����˫ָ��������
    /// </summary>
    void HandlePinchToZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // �ҵ���������ǰһ֡�͵�ǰ֡��λ��
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // ����ǰһ֡�͵�ǰ֡������֮��ľ���
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            // ����������
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            // Ӧ������
            ScaleModel(deltaMagnitudeDiff * 0.01f * scaleSpeed);
        }
    }

    /// <summary>
    /// ���ð�ť�¼�
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
    /// ����ģ��
    /// </summary>
    /// <param name="scaleDelta">���ű仯��</param>
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
    /// �Ŵ�ģ��
    /// </summary>
    public void ZoomIn()
    {
        ScaleModel(0.1f * scaleSpeed);
    }

    /// <summary>
    /// ��Сģ��
    /// </summary>
    public void ZoomOut()
    {
        ScaleModel(-0.1f * scaleSpeed);
    }

    /// <summary>
    /// ��������
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
    /// �����ض����ű���
    /// </summary>
    /// <param name="scale">Ŀ�����ű���</param>
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
    /// ƽ�����ŵ�Ŀ��ߴ��Э��
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
    /// ��ȡ��ǰ���ű���
    /// </summary>
    public float GetCurrentScale()
    {
        return currentScale;
    }

    /// <summary>
    /// ��ȡ���Űٷֱȣ�0-1��
    /// </summary>
    public float GetScalePercentage()
    {
        return (currentScale - minScale) / (maxScale - minScale);
    }

    /// <summary>
    /// ����/������������
    /// </summary>
    public void SetPinchToZoomEnabled(bool enabled)
    {
        enablePinchToZoom = enabled;
    }

    /// <summary>
    /// ������������
    /// </summary>
    public void SetScaleLimits(float min, float max)
    {
        minScale = Mathf.Max(0.01f, min);
        maxScale = Mathf.Max(minScale + 0.1f, max);
        currentScale = Mathf.Clamp(currentScale, minScale, maxScale);
        targetScale = originalScale * currentScale;
    }

    // ��������
    public float MinScale { get => minScale; }
    public float MaxScale { get => maxScale; }
    public float DefaultScale { get => defaultScale; }
}