using UnityEngine;

public class RandomIdleController : MonoBehaviour
{
    [SerializeField] private Animator _animator; // 关联模型的Animator组件
    [SerializeField] private string[] _idleAnimationNames; // 存储待机动画的名称（如["Idle1", "Idle2", "Idle3"]）
    [SerializeField] private float _minInterval = 5f; // 最小切换间隔（秒）
    [SerializeField] private float _maxInterval = 10f; // 最大切换间隔（秒）

    private float _nextSwitchTime;

    void Start()
    {
        // 初始化下次切换时间
        _nextSwitchTime = Time.time + Random.Range(_minInterval, _maxInterval);
    }

    void Update()
    {
        // 到时间则随机切换动画
        if (Time.time >= _nextSwitchTime)
        {
            PlayRandomIdle();
            // 重新计算下次切换时间
            _nextSwitchTime = Time.time + Random.Range(_minInterval, _maxInterval);
        }
    }

    // 随机播放一个待机动画
    private void PlayRandomIdle()
    {
        if (_idleAnimationNames.Length == 0) return;

        // 随机选择一个动画名称
        int randomIndex = Random.Range(0, _idleAnimationNames.Length);
        string randomIdleName = _idleAnimationNames[randomIndex];

        // 播放选中的动画（通过Animator参数触发，或直接CrossFade）
        _animator.CrossFade(randomIdleName, 0.2f); // 0.2秒平滑过渡
    }
}