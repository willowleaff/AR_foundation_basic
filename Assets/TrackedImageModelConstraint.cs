using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// 整合版：图像-预制体配对 + 模型范围约束（一个脚本解决所有需求）
/// </summary>
[RequireComponent(typeof(ARTrackedImageManager))]
public class ARImageModelConstraint : MonoBehaviour
{
    [Header("图像-预制体配对")]
    public XRReferenceImageLibrary imageLibrary;
    public List<string> imageNames;       // 参考图像的名称（需与XRReferenceImageLibrary中完全一致）
    public List<GameObject> pairedPrefabs; // 对应图像的预制体

    [Header("模型范围约束")]
    public float maxModelHeight = 0.3f;      // 模型最大高度（米）
    public bool allowGroundContact = true;   // 是否允许贴地
    public bool enableHorizontalConstraint = true; // 水平范围约束
    public bool enableHeightConstraint = true;     // 高度约束
    [Range(0f, 20f)]
    public float smoothConstraintSpeed = 8f; // 约束平滑速度

    // 私有变量
    private ARTrackedImageManager _trackedImageManager;
    private Dictionary<string, GameObject> _imagePrefabMap = new();
    private Dictionary<ARTrackedImage, GameObject> _imageModelMap = new();

    void Awake()
    {
        _trackedImageManager = GetComponent<ARTrackedImageManager>();
        // 初始化图像-预制体映射
        InitImagePrefabMap();
    }

    void OnEnable()
    {
        _trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        _trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnDestroy()
    {
        // 销毁所有生成的模型
        foreach (var model in _imageModelMap.Values)
            Destroy(model);
        _imageModelMap.Clear();
    }

    /// <summary>
    /// 初始化图像-预制体映射字典
    /// </summary>
    private void InitImagePrefabMap()
    {
        _imagePrefabMap.Clear();
        for (int i = 0; i < imageNames.Count && i < pairedPrefabs.Count; i++)
        {
            string imageName = imageNames[i].Trim();
            GameObject prefab = pairedPrefabs[i];
            if (!string.IsNullOrEmpty(imageName) && prefab != null)
            {
                if (_imagePrefabMap.ContainsKey(imageName))
                    Debug.LogWarning($"图像名称「{imageName}」重复，请检查配置");
                else
                    _imagePrefabMap.Add(imageName, prefab);
            }
        }
    }

    /// <summary>
    /// 图像跟踪状态变化时触发
    /// </summary>
    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        // 新增图像：生成对应模型
        foreach (var trackedImage in args.added)
        {
            if (!_imageModelMap.ContainsKey(trackedImage))
                CreatePairedModel(trackedImage);
        }

        // 更新图像：控制模型激活状态
        foreach (var trackedImage in args.updated)
        {
            if (_imageModelMap.TryGetValue(trackedImage, out var model))
                UpdateModelActiveState(trackedImage, model);
        }

        // 移除图像：销毁模型
        foreach (var trackedImage in args.removed)
        {
            if (_imageModelMap.TryGetValue(trackedImage, out var model))
            {
                Destroy(model);
                _imageModelMap.Remove(trackedImage);
            }
        }
    }

    /// <summary>
    /// 为跟踪图像创建对应模型
    /// </summary>
    private void CreatePairedModel(ARTrackedImage trackedImage)
    {
        if (trackedImage.referenceImage == null)
        {
            Debug.LogWarning("跟踪图像无参考数据，跳过模型创建");
            return;
        }

        string imageName = trackedImage.referenceImage.name;
        if (!_imagePrefabMap.TryGetValue(imageName, out var targetPrefab))
        {
            Debug.LogWarning($"图像「{imageName}」未配置配对预制体，跳过");
            return;
        }

        // 在跟踪图像下创建模型
        var model = Instantiate(targetPrefab, trackedImage.transform);
        model.transform.localPosition = Vector3.zero + Vector3.up * (allowGroundContact ? 0f : 0.01f);
        model.transform.localRotation = Quaternion.identity;
        model.SetActive(false);

        _imageModelMap.Add(trackedImage, model);
    }

    /// <summary>
    /// 更新模型激活状态
    /// </summary>
    private void UpdateModelActiveState(ARTrackedImage trackedImage, GameObject model)
    {
        bool shouldActive = trackedImage.trackingState == TrackingState.Tracking;
        if (model.activeSelf != shouldActive)
        {
            model.SetActive(shouldActive);
            if (shouldActive)
                model.transform.localPosition = Vector3.zero + Vector3.up * (allowGroundContact ? 0f : 0.01f);
        }
    }

    void Update()
    {
        // 每帧约束所有模型位置
        ConstraintAllModelsInRange();
    }

    /// <summary>
    /// 约束所有模型在对应图像范围内
    /// </summary>
    private void ConstraintAllModelsInRange()
    {
        foreach (var (trackedImage, model) in _imageModelMap)
        {
            if (trackedImage.trackingState != TrackingState.Tracking || !model.activeSelf)
                continue;

            ConstraintModelPosition(trackedImage, model);
        }
    }

    /// <summary>
    /// 强制约束模型位置在图像范围内
    /// </summary>
    private void ConstraintModelPosition(ARTrackedImage trackedImage, GameObject model)
    {
        Vector2 imageSize = trackedImage.referenceImage.size;
        if (imageSize == Vector2.zero)
        {
            Debug.LogWarning($"图像「{trackedImage.referenceImage.name}」未设置物理尺寸，无法约束");
            return;
        }

        Transform imageTransform = trackedImage.transform;
        Transform modelTransform = model.transform;

        // 世界坐标转图像本地坐标
        Vector3 localPos = imageTransform.InverseTransformPoint(modelTransform.position);
        Vector3 targetLocalPos = localPos;

        // 水平约束（X/Z轴）
        if (enableHorizontalConstraint)
        {
            float halfWidth = imageSize.x / 2f;
            float halfHeight = imageSize.y / 2f;
            targetLocalPos.x = Mathf.Clamp(targetLocalPos.x, -halfWidth, halfWidth);
            targetLocalPos.z = Mathf.Clamp(targetLocalPos.z, -halfHeight, halfHeight);
        }

        // 高度约束（Y轴）
        if (enableHeightConstraint)
        {
            float minHeight = allowGroundContact ? 0f : 0.01f;
            targetLocalPos.y = Mathf.Clamp(targetLocalPos.y, minHeight, maxModelHeight);
        }

        // 本地坐标转世界坐标，平滑移动
        Vector3 targetWorldPos = imageTransform.TransformPoint(targetLocalPos);
        if (smoothConstraintSpeed > 0f)
        {
            modelTransform.position = Vector3.Lerp(
                modelTransform.position,
                targetWorldPos,
                smoothConstraintSpeed * Time.deltaTime
            );
        }
        else
        {
            modelTransform.position = targetWorldPos;
        }
    }
}