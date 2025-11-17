using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ARSpawnOnAnimation : MonoBehaviour
{
    [SerializeField] private GameObject spawnPrefab; // 要蹦出的模型预制体
    [SerializeField] private float spawnOffsetX = 1f; // X轴偏移（右侧1米）
    private ARPlane currentPlane; // 存储检测到的平面

    // （步骤1：监听平面检测，记录平面）
    void Start()
    {
        ARPlaneManager planeManager = FindObjectOfType<ARPlaneManager>();
        planeManager.planesChanged += OnPlanesChanged;
    }

    void OnPlanesChanged(ARPlanesChangedEventArgs args)
    {
        if (args.added.Count > 0)
        {
            currentPlane = args.added[0]; // 记录第一个检测到的平面（可根据需求调整）
        }
    }

    // （步骤3：动画事件触发的方法，计算并生成位置）
    public void SpawnModelAtOffset()
    {
        if (currentPlane == null || spawnPrefab == null) return;

        // 计算最终位置：平面位置 + 偏移量
        Vector3 spawnPosition = currentPlane.transform.position +
                               currentPlane.transform.right * spawnOffsetX; // 沿平面右侧偏移

        // 实例化模型
        Instantiate(spawnPrefab, spawnPosition, Quaternion.identity);
    }
}