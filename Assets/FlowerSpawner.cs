//using UnityEngine;

//public class FlowerSpawner : MonoBehaviour
//{
//    [Tooltip("花的预制体，在Inspector中拖入")]
//    public GameObject flowerPrefab;

//    [Tooltip("花生成在脚下的偏移距离（根据模型高度调整，默认0.01米）")]
//    public float footOffset = 0.01f;

//    private GameObject spawnedFlower; // 记录生成的花，用于后续销毁
//    private Animator animator;

//    void Start()
//    {
//        // 获取动画组件（确保脚本挂载在有Animator的物体上）
//        animator = GetComponent<Animator>();
//        if (animator == null)
//        {
//            Debug.LogError("物体上没有Animator组件！请添加Animator后再运行");
//        }
//    }

//    /// <summary>
//    /// 动画事件调用：在预制体脚下生成花
//    /// </summary>
//    public void SpawnFlower()
//    {
//        // 检查预制体是否赋值
//        if (flowerPrefab == null)
//        {
//            Debug.LogWarning("请在Inspector中给flowerPrefab赋值花的预制体！");
//            return;
//        }

//        // 计算脚下位置（当前物体位置 + 向下偏移）
//        Vector3 footPosition = transform.position + Vector3.down * footOffset;

//        // 生成花（使用当前物体的旋转角度）
//        spawnedFlower = Instantiate(flowerPrefab, footPosition, transform.rotation);
//    }

//    /// <summary>
//    /// 动画事件调用：销毁生成的花
//    /// </summary>
//    public void DestroyFlower()
//    {
//        // 检查是否有花需要销毁
//        if (spawnedFlower != null)
//        {
//            Destroy(spawnedFlower);
//            spawnedFlower = null; // 清空引用，避免重复销毁
//        }
//    }
//}
using UnityEngine;
using System.Collections; // 补充这一行
using UnityEngine;

public class FlowerSpawner : MonoBehaviour
{
    [Tooltip("花的预制体，在Inspector中拖入")]
    public GameObject flowerPrefab;

    [Tooltip("花生成在脚下的偏移距离")]
    public float footOffset = 0.01f;

    [Header("运动曲线配置")]
    [Tooltip("Y轴上升高度曲线（X=时间0~1，Y=高度值）")]
    public AnimationCurve heightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 默认缓入缓出上升1米
    [Tooltip("X/Z轴左右偏移曲线（可选，比如摇摆）")]
    public AnimationCurve offsetCurve = AnimationCurve.Linear(0, 0, 1, 0); // 默认无偏移
    [Tooltip("运动总时长（秒）")]
    public float moveDuration = 1.5f;
    [Tooltip("是否在运动结束后销毁模型")]
    public bool destroyAfterMove = true;

    private GameObject spawnedFlower;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("物体上没有Animator组件！");
        }
    }

    public void SpawnFlower()
    {
        if (flowerPrefab == null)
        {
            Debug.LogWarning("请赋值flowerPrefab！");
            return;
        }

        // 生成位置
        Vector3 footPosition = transform.position + Vector3.down * footOffset;
        spawnedFlower = Instantiate(flowerPrefab, footPosition, transform.rotation);

        // 启动运动曲线协程
        StartCoroutine(MoveWithCurve(spawnedFlower, footPosition));
    }

    /// <summary>
    /// 按动画曲线驱动模型运动
    /// </summary>
    private IEnumerator MoveWithCurve(GameObject target, Vector3 startPos)
    {
        float elapsedTime = 0f;
        Vector3 startRotation = target.transform.eulerAngles; // 初始旋转

        while (elapsedTime < moveDuration)
        {
            // 计算时间比例（0~1）
            float timeRatio = elapsedTime / moveDuration;

            // 1. 采样高度曲线（Y轴运动）
            float height = heightCurve.Evaluate(timeRatio);
            // 2. 采样左右偏移曲线（X轴摇摆，可改为Z轴）
            float offsetX = offsetCurve.Evaluate(timeRatio);

            // 更新位置：起始位置 + 高度(Y) + 左右偏移(X)
            target.transform.position = new Vector3(
                startPos.x + offsetX,
                startPos.y + height,
                startPos.z
            );

            // 可选：添加旋转动画（比如绕Y轴旋转）
            target.transform.eulerAngles = startRotation + new Vector3(0, 360 * timeRatio, 0);

            // 累加时间
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 运动结束后处理
        if (destroyAfterMove)
        {
            Destroy(target);
            spawnedFlower = null;
        }
    }

    public void DestroyFlower()
    {
        if (spawnedFlower != null)
        {
            Destroy(spawnedFlower);
            spawnedFlower = null;
        }
    }
}

