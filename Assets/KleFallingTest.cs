using UnityEngine;
using System.Collections;

// 极简测试：仅点击模型触发Falling（无冗余、带日志定位）
public class KleFallingTest : MonoBehaviour
{
    public Animator animator; // 手动拖入Kle的Animator
    private bool isPlaying = false;

    void Awake()
    {
        // 自动找Animator（兜底）
        if (animator == null)
        {
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        }

        // 日志：确认是否找到Animator
        if (animator != null)
        {
            Debug.Log("测试：找到Animator → " + animator.name);
        }
        else
        {
            Debug.LogError("测试：未找到Animator！");
            enabled = false;
        }
    }

    // Unity内置：点击带Collider的模型自动触发（无需射线/相机配置）
    void OnMouseDown()
    {
        Debug.Log("测试：检测到模型点击！"); // 日志：确认点击被检测到

        if (isPlaying || animator == null) return;

        isPlaying = true;
        // 复用你之前全局触发成功的逻辑
        animator.ResetTrigger("FallingTrigger");
        animator.SetTrigger("FallingTrigger");
        Debug.Log("测试：已执行SetTrigger(FallingTrigger)");

        StartCoroutine(ResetState());
    }

    // 动画结束后重置状态
    IEnumerator ResetState()
    {
        yield return new WaitForSeconds(1f); // 适配你的动画时长
        isPlaying = false;
        Debug.Log("测试：动画状态重置完成");
    }
}