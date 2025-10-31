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
        [Tooltip("��Ƭ��ģ�͵�ӳ���ϵ����Ϊ��Ƭ���ƣ�����ͼ�����һ�£���ֵΪ��Ӧ��ģ��Ԥ����")]
        private List<CardModelPair> m_CardModelPairs = new List<CardModelPair>();

        [SerializeField]
        [Tooltip("����Ƭδƥ�䵽ģ��ʱ��Ĭ�ϼ��ص�ģ�ͣ���ѡ��")]
        private GameObject m_DefaultModel;

        // �洢��ʵ������ģ�ͣ���Ϊ����ͼ���GUID�������ظ�������
        private Dictionary<string, GameObject> m_SpawnedModels = new Dictionary<string, GameObject>();

        private ARTrackedImageManager m_TrackedImageManager;

        void Awake()
        {
            m_TrackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        void OnEnable()
        {
            // ������Ƭ����״̬�仯�¼�
            m_TrackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        void OnDisable()
        {
            m_TrackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        // ����Ƭ����״̬�仯�����������¡��Ƴ���
        void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            // 1. ������ʶ�𵽵Ŀ�Ƭ�����ɶ�Ӧģ��
            foreach (var trackedImage in eventArgs.added)
            {
                SpawnModelForTrackedImage(trackedImage);
            }

            // 2. �������ڸ��ٵĿ�Ƭ������ģ��״̬����������λ��У׼��
            foreach (var trackedImage in eventArgs.updated)
            {
                UpdateModelState(trackedImage);
            }

            // 3. ����ʧ���ٵĿ�Ƭ������ģ�ͻ����أ���������ѡ��
            foreach (var trackedImage in eventArgs.removed)
            {
                DestroySpawnedModel(trackedImage);
            }
        }

        // Ϊ��ʶ��Ŀ�Ƭ����ģ��
        private void SpawnModelForTrackedImage(ARTrackedImage trackedImage)
        {
            // �ÿ�Ƭ��GUID��ΪΨһ��ʶ������ͬһ�ſ�Ƭ�ظ�����ģ�ͣ�
            string imageGuid = trackedImage.referenceImage.guid.ToString();

            // ��������ɹ�ģ�ͣ�ֱ�ӷ���
            if (m_SpawnedModels.ContainsKey(imageGuid))
                return;

            // ���ҵ�ǰ��Ƭ��Ӧ��ģ��Ԥ����
            GameObject targetModelPrefab = GetModelPrefabByCardName(trackedImage.referenceImage.name);

            // ���û��ƥ���ģ�ͣ�ʹ��Ĭ��ģ�ͣ������ã�
            if (targetModelPrefab == null)
            {
                if (m_DefaultModel != null)
                    targetModelPrefab = m_DefaultModel;
                else
                {
                    Debug.LogWarning($"δΪ��Ƭ {trackedImage.referenceImage.name} ����ģ�ͣ�����Ĭ��ģ��");
                    return;
                }
            }

            // �ڿ�Ƭ�ĸ���λ��ʵ����ģ��
            GameObject spawnedModel = Instantiate(
                targetModelPrefab,
                trackedImage.transform.position,  // ģ��λ���뿨Ƭһ��
                trackedImage.transform.rotation,  // ģ����ת�뿨Ƭһ��
                trackedImage.transform            // ģ����Ϊ��Ƭ�������壬�Զ�����
            );

            // ����ģ�����ţ���ѡ�����ݿ�Ƭʵ�ʳߴ�����ģ�ʹ�С��
            spawnedModel.transform.localScale = Vector3.one * 0.1f;  // ʾ����ͳһ����Ϊ0.1��

            // �洢�����ɵ�ģ�ͣ����ں������»�����
            m_SpawnedModels.Add(imageGuid, spawnedModel);
        }

        // ����ģ��״̬����������ʱ��ʾ����ʧʱ���أ�
        private void UpdateModelState(ARTrackedImage trackedImage)
        {
            string imageGuid = trackedImage.referenceImage.guid.ToString();

            // ���ģ�Ͳ����ڣ�ֱ�ӷ���
            if (!m_SpawnedModels.TryGetValue(imageGuid, out GameObject spawnedModel))
                return;

            // ���ݸ���״̬����ģ������
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                spawnedModel.SetActive(true);
                // ��ѡ��ʵʱУ׼ģ��λ�ã����ⳤʱ����ٺ��ƫ�ƣ�
                spawnedModel.transform.position = trackedImage.transform.position;
                spawnedModel.transform.rotation = trackedImage.transform.rotation;
            }
            else
            {
                spawnedModel.SetActive(false);  // ���ٶ�ʧʱ����ģ��
            }
        }

        // ���ٶ�ʧ���ٵĿ�Ƭ��Ӧ��ģ��
        private void DestroySpawnedModel(ARTrackedImage trackedImage)
        {
            string imageGuid = trackedImage.referenceImage.guid.ToString();

            if (m_SpawnedModels.TryGetValue(imageGuid, out GameObject spawnedModel))
            {
                Destroy(spawnedModel);  // ����ģ��
                m_SpawnedModels.Remove(imageGuid);  // ���ֵ����Ƴ�
            }
        }

        // ���ݿ�Ƭ���Ʋ��Ҷ�Ӧ��ģ��Ԥ����
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

        // ���л��ࣺ������Inspector�����á���Ƭ����-ģ�͡�ӳ��
        [Serializable]
        public class CardModelPair
        {
            [Tooltip("��Ƭ���ƣ�������ͼ��������õ�����һ�£�")]
            public string cardName;

            [Tooltip("�ÿ�Ƭ��Ӧ��3Dģ��Ԥ����")]
            public GameObject modelPrefab;
        }
    }
}