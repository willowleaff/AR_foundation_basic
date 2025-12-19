using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 优化版：仅点击/触摸模型 触发Falling动画（适配手机+编辑器）
/// </summary>
public class KleFallingClickModel : MonoBehaviour
{
    [Header("核心动画配置")]
    public string fallingTriggerName = "FallingTrigger";
    public string fallingStateName = "Falling";

    [Header("模型点击配置（核心）")]
    public Camera mainCamera; // 拖入场景主相机（若无则自动取MainCamera）
    public LayerMask modelLayer; // 选择模型所在层（避免点击其他物体触发）

    [Header("随机待机动画（可选）")]
    public string[] idleAnimationNames;
    public float minIdleInterval = 5f;
    public float maxIdleInterval = 10f;
    [Range(0.1f, 0.5f)]
    public float idleCrossFadeDuration = 0.2f;

    [Header("点击反馈（可选）")]
    public AudioClip clickSound;
    public ParticleSystem clickEffect;
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    public float highlightDuration = 0.5f;

    // 核心组件
    private Animator _animator;
    private AudioSource _audioSource;
    private Renderer[] _modelRenderers;
    private Color[] _originalColors;

    // 状态控制
    private bool _isFallingPlaying = false;
    private float _nextIdleTime = 0;
    private int _fallingTriggerHash;
    private int _fallingStateHash;

    void Awake()
    {
        // 1. 初始化Animator（复用按钮触发成功的逻辑）
        InitAnimatorMobile();
        // 2. 初始化反馈组件
        InitFeedbackMobile();
        // 3. 初始化待机时间
        InitIdleSwitchTime();
        // 4. 初始化点击检测相机
        InitClickCamera();
    }

    void LateUpdate()
    {
        if (_animator == null || _isFallingPlaying) return;

        // ✅ 仅保留：检测模型点击（PC鼠标/手机触摸都走这里）
        CheckModelClick();
        // ❌ 移除：CheckMobileTouch（屏幕任意触摸触发的逻辑）
        HandleIdleMobile();
    }

    #region 1. 初始化核心组件
    private void InitAnimatorMobile()
    {
        // 优先找自身Animator（按钮触发成功的逻辑）
        _animator = GetComponent<Animator>();
        if (_animator == null)
        {
            _animator = GetComponentInChildren<Animator>(true);
        }

        if (_animator == null)
        {
            Debug.LogError("[初始化] Animator为空！请确保脚本挂载到Kle物体", this);
            enabled = false;
            return;
        }

        _animator.enabled = true;
        _animator.updateMode = AnimatorUpdateMode.AnimatePhysics;
        _animator.keepAnimatorStateOnDisable = true;

        _fallingTriggerHash = Animator.StringToHash(fallingTriggerName);
        _fallingStateHash = Animator.StringToHash(fallingStateName);

        _animator.Rebind();
        _animator.Update(0);

        Debug.Log($"[初始化] Animator准备完成！Trigger哈希：{_fallingTriggerHash}");
    }

    private void InitClickCamera()
    {
        // 自动获取主相机（若未手动指定）
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[初始化] 未找到主相机！请手动拖入MainCamera", this);
            }
        }
    }

    private void InitFeedbackMobile()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && clickSound != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.volume = 1f;
        }

        _modelRenderers = GetComponentsInChildren<Renderer>(true);
        _originalColors = new Color[_modelRenderers.Length];
        for (int i = 0; i < _modelRenderers.Length; i++)
        {
            _originalColors[i] = _modelRenderers[i].material.color;
        }

        if (clickEffect != null)
        {
            clickEffect.playOnAwake = false;
            clickEffect.transform.SetParent(transform, false);
        }
    }

    private void InitIdleSwitchTime()
    {
        if (idleAnimationNames == null || idleAnimationNames.Length == 0) return;
        _nextIdleTime = Time.time + Random.Range(minIdleInterval + 2f, maxIdleInterval + 2f);
    }
    #endregion

    #region 2. 模型点击检测（核心：仅点击模型触发）
    private void CheckModelClick()
    {
        // 编辑器：鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            if (IsClickOnModel(Input.mousePosition))
            {
                TriggerFallingCore(); // 复用核心触发逻辑
            }
        }
        // 手机端：触摸点击（仅检测第一个触摸点）
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (IsClickOnModel(Input.GetTouch(0).position))
            {
                TriggerFallingCore(); // 复用核心触发逻辑
            }
        }
    }

    /// <summary>
    /// 射线检测：判断点击是否命中模型（精准检测，仅模型触发）
    /// </summary>
    private bool IsClickOnModel(Vector2 screenPos)
    {
        if (mainCamera == null) return false;

        // 从相机发射射线到点击/触摸位置
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, modelLayer))
        {
            // 检测是否命中当前模型（自身/子物体）
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                Debug.Log("[模型点击] 命中Kle模型，触发Falling！");
                return true;
            }
        }
        return false;
    }
    #endregion

    #region 3. 核心触发逻辑（复用按钮成功的逻辑）
    /// <summary>
    /// 核心触发方法：仅模型点击/按钮点击 调用此方法
    /// </summary>
    private void TriggerFallingCore()
    {
        if (_isFallingPlaying)
        {
            Debug.Log("[触发] Falling动画正在播放，跳过");
            return;
        }

        _isFallingPlaying = true;

        // 复用按钮触发成功的逻辑
        _animator.SetLayerWeight(0, 1f);
        _animator.ResetTrigger(_fallingTriggerHash);
        _animator.Update(0);
        _animator.SetTrigger(_fallingTriggerHash);
        _animator.Update(Time.deltaTime);
        _animator.SetTrigger(_fallingTriggerHash);

        Debug.Log($"[触发] 执行Falling Trigger！名称：{fallingTriggerName}");

        // 播放反馈
        PlayFeedbackMobile();
        // 等待动画结束
        StartCoroutine(WaitFallingEndMobile());
    }

    /// <summary>
    /// 保留按钮触发的公开方法（兼容原有按钮）
    /// </summary>
    public void TriggerFallingByButton()
    {
        TriggerFallingCore();
    }
    #endregion

    #region 4. 辅助逻辑（仅保留待机和反馈，删除屏幕触摸）
    private void HandleIdleMobile()
    {
        if (idleAnimationNames == null || idleAnimationNames.Length == 0 || _animator.IsInTransition(0)) return;

        if (Time.time >= _nextIdleTime)
        {
            int randomIndex = Random.Range(0, idleAnimationNames.Length);
            _animator.CrossFade(idleAnimationNames[randomIndex], idleCrossFadeDuration, 0, 0f);
            _nextIdleTime = Time.time + Random.Range(minIdleInterval, maxIdleInterval);
        }
    }

    private IEnumerator WaitFallingEndMobile()
    {
        float timeout = 8f;
        float timer = 0f;
        bool enterFalling = false;

        while (timer < timeout)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash == _fallingStateHash || stateInfo.IsName(fallingStateName))
            {
                enterFalling = true;
                break;
            }
            timer += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }

        if (!enterFalling)
        {
            Debug.LogError("[触发] 未进入Falling状态！检查：1.Trigger名 2.过渡Has Exit Time", this);
            _isFallingPlaying = false;
            yield break;
        }

        Debug.Log("[触发] Falling动画开始播放！");

        while (true)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.fullPathHash != _fallingStateHash && !stateInfo.IsName(fallingStateName))
            {
                break;
            }
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(0.1f);
        Debug.Log("[触发] Falling动画播放结束！");

        _isFallingPlaying = false;
        _animator.ResetTrigger(_fallingTriggerHash);
    }

    private void PlayFeedbackMobile()
    {
        if (clickSound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clickSound);
        }

        if (clickEffect != null)
        {
            clickEffect.transform.position = transform.position + Vector3.up * 0.1f;
            clickEffect.Play();
        }

        if (_modelRenderers.Length > 0)
        {
            StartCoroutine(HighlightMobile());
        }
    }

    private IEnumerator HighlightMobile()
    {
        for (int i = 0; i < _modelRenderers.Length; i++)
        {
            _modelRenderers[i].material.color = highlightColor;
        }
        yield return new WaitForSeconds(highlightDuration);
        for (int i = 0; i < _modelRenderers.Length; i++)
        {
            _modelRenderers[i].material.color = _originalColors[i];
        }
    }
    #endregion

    // ❌ 完全删除：CheckMobileTouch方法（屏幕任意触摸触发的冗余逻辑）
}