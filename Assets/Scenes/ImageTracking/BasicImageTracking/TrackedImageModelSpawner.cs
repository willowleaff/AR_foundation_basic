using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.ARFoundation;

namespace UnityEngine.XR.ARFoundation.Samples
{
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class TrackedImageModelSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("卡片与模型的映射关系：键为卡片名称（需与图像库中一致），值为对应的模型预制体")]
        private List<CardModelPair> m_CardModelPairs = new List<CardModelPair>();

        [SerializeField]
        [Tooltip("当卡片未匹配到模型时，默认加载的模型（可选）")]
        private GameObject m_DefaultModel;

        // 存储已实例化的模型（键为跟踪图像的GUID，避免重复创建）
        private Dictionary<string, GameObject> m_SpawnedModels = new Dictionary<string, GameObject>();

        private ARTrackedImageManager m_TrackedImageManager;

        void Awake()
        {
            m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        void OnEnable()
        {
            // 监听卡片跟踪状态变化事件
            m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        void OnDisable()
        {
            m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        // 处理卡片跟踪状态变化（新增、更新、移除）
        void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            // 1. 处理新识别到的卡片：生成对应模型
            foreach (var trackedImage in eventArgs.added)
            {
                SpawnModelForTrackedImage(trackedImage);
            }

            // 2. 处理正在跟踪的卡片：更新模型状态（如显隐、位置校准）
            foreach (var trackedImage in eventArgs.updated)
            {
                UpdateModelState(trackedImage);
            }

            // 3. 处理丢失跟踪的卡片：销毁模型或隐藏（根据需求选择）
            foreach (var trackedImage in eventArgs.removed)
            {
                DestroySpawnedModel(trackedImage);
            }
        }

        // 为新识别的卡片生成模型
        private void SpawnModelForTrackedImage(ARTrackedImage trackedImage)
        {
            // 用卡片的GUID作为唯一标识（避免同一张卡片重复生成模型）
            string imageGuid = trackedImage.referenceImage.guid.ToString();

            // 如果已生成过模型，直接返回
            if (m_SpawnedModels.ContainsKey(imageGuid))
                return;

            // 查找当前卡片对应的模型预制体
            GameObject targetModelPrefab = GetModelPrefabByCardName(trackedImage.referenceImage.name);

            // 如果没有匹配的模型，使用默认模型（若设置）
            if (targetModelPrefab == null)
            {
                if (m_DefaultModel != null)
                    targetModelPrefab = m_DefaultModel;
                else
                {
                    Debug.LogWarning($"未为卡片 {trackedImage.referenceImage.name} 设置模型，且无默认模型");
                    return;
                }
            }

            // 在卡片的跟踪位置实例化模型
            GameObject spawnedModel = Instantiate(
                targetModelPrefab,
                trackedImage.transform.position,  // 模型位置与卡片一致
                trackedImage.transform.rotation,  // 模型旋转与卡片一致
                trackedImage.transform            // 模型设为卡片的子物体，自动跟随
            );

            // 调整模型缩放（可选：根据卡片实际尺寸适配模型大小）
            spawnedModel.transform.localScale = Vector3.one * 0.1f;  // 示例：统一缩放为0.1倍

            // 存储已生成的模型，便于后续更新或销毁
            m_SpawnedModels.Add(imageGuid, spawnedModel);
        }

        // 更新模型状态（跟踪正常时显示，丢失时隐藏）
        private void UpdateModelState(ARTrackedImage trackedImage)
        {
            string imageGuid = trackedImage.referenceImage.guid.ToString();

            // 如果模型不存在，直接返回
            if (!m_SpawnedModels.TryGetValue(imageGuid, out GameObject spawnedModel))
                return;

            // 根据跟踪状态控制模型显隐
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                spawnedModel.SetActive(true);
                // 可选：实时校准模型位置（避免长时间跟踪后的偏移）
                spawnedModel.transform.position = trackedImage.transform.position;
                spawnedModel.transform.rotation = trackedImage.transform.rotation;
            }
            else
            {
                spawnedModel.SetActive(false);  // 跟踪丢失时隐藏模型
            }
        }

        // 销毁丢失跟踪的卡片对应的模型
        private void DestroySpawnedModel(ARTrackedImage trackedImage)
        {
            string imageGuid = trackedImage.referenceImage.guid.ToString();

            if (m_SpawnedModels.TryGetValue(imageGuid, out GameObject spawnedModel))
            {
                Destroy(spawnedModel);  // 销毁模型
                m_SpawnedModels.Remove(imageGuid);  // 从字典中移除
            }
        }

        // 根据卡片名称查找对应的模型预制体
        private GameObject GetModelPrefabByCardName(string cardName)
        {
            foreach (var pair in m_CardModelPairs)
            {
                if (pair.cardName == cardName)
                {
                    return pair.modelPrefab;
                }
            }
            return null;
        }

        // 序列化类：用于在Inspector中设置“卡片名称-模型”映射
        [Serializable]
        public class CardModelPair
        {
            [Tooltip("卡片名称（必须与图像库中设置的名称一致）")]
            public string cardName;

            [Tooltip("该卡片对应的3D模型预制体")]
            public GameObject modelPrefab;
        }
    }
}