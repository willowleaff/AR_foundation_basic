using UnityEngine;
using System.Collections;

public class SphereInteraction : MonoBehaviour
{
    [Header("点击反馈设置")]
    public Color clickColor = Color.red;
    public float clickScaleMultiplier = 1.2f;
    public float clickAnimationDuration = 0.3f;

    [Header("拖拽反馈设置")]
    public Color dragColor = Color.blue;

    [Header("音频反馈")]
    public AudioClip clickSound;
    public AudioClip dragStartSound;
    public AudioClip dragEndSound;

    private Renderer _renderer;
    private Color _originalColor;
    private Vector3 _originalScale;
    private AudioSource _audioSource;
    private Material _materialInstance;
    private bool _isAnimating = false;
    private Coroutine _currentAnimation;

    void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            Debug.LogError("Renderer组件未找到!");
            return;
        }

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        // 创建材质实例（关键修复）
        _materialInstance = new Material(_renderer.material);
        _renderer.material = _materialInstance;

        _originalColor = _materialInstance.color;
        _originalScale = transform.localScale;

        // 设置球体层级
        gameObject.layer = LayerMask.NameToLayer("Sphere");

        Debug.Log("球体交互组件初始化完成");
    }

    public void OnSphereClicked()
    {
        Debug.Log("球体被点击");

        if (_isAnimating)
        {
            if (_currentAnimation != null)
                StopCoroutine(_currentAnimation);
        }

        _currentAnimation = StartCoroutine(ClickAnimation());
    }

    private IEnumerator ClickAnimation()
    {
        _isAnimating = true;

        // 播放点击音效
        if (clickSound != null)
            _audioSource.PlayOneShot(clickSound);

        // 第一阶段：放大并变色
        Vector3 targetScale = _originalScale * clickScaleMultiplier;
        float elapsed = 0f;

        while (elapsed < clickAnimationDuration * 0.5f)
        {
            float progress = elapsed / (clickAnimationDuration * 0.5f);

            // 同时进行缩放和颜色变化
            transform.localScale = Vector3.Lerp(_originalScale, targetScale, progress);
            _materialInstance.color = Color.Lerp(_originalColor, clickColor, progress);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保达到目标状态
        transform.localScale = targetScale;
        _materialInstance.color = clickColor;

        // 短暂停留
        yield return new WaitForSeconds(0.1f);

        // 第二阶段：恢复
        elapsed = 0f;
        while (elapsed < clickAnimationDuration)
        {
            float progress = elapsed / clickAnimationDuration;

            transform.localScale = Vector3.Lerp(targetScale, _originalScale, progress);
            _materialInstance.color = Color.Lerp(clickColor, _originalColor, progress);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 确保恢复原状
        transform.localScale = _originalScale;
        _materialInstance.color = _originalColor;

        _isAnimating = false;
        _currentAnimation = null;
    }

    public void OnDragStart()
    {
        Debug.Log("开始拖拽球体");

        // 停止任何正在进行的动画
        if (_currentAnimation != null)
        {
            StopCoroutine(_currentAnimation);
            _isAnimating = false;
        }

        // 改变颜色表示拖拽状态
        _materialInstance.color = dragColor;

        // 播放拖拽开始音效
        if (dragStartSound != null)
            _audioSource.PlayOneShot(dragStartSound);
    }

    public void OnDragEnd()
    {
        Debug.Log("结束拖拽球体");

        // 恢复原色
        _materialInstance.color = _originalColor;

        // 播放拖拽结束音效
        if (dragEndSound != null)
            _audioSource.PlayOneShot(dragEndSound);
    }

    void OnDestroy()
    {
        if (_currentAnimation != null)
            StopCoroutine(_currentAnimation);

        if (_materialInstance != null)
            Destroy(_materialInstance);
    }
}
