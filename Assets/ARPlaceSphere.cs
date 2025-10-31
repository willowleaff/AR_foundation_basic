using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARPlaceSphere : MonoBehaviour
{
    [Header("球体设置")]
    public GameObject spherePrefab;
    public int maxSpheres = 10;

    [Header("交互设置")]
    public float longPressDuration = 1.0f;
    public float dragSpeed = 2.0f;
    public float minScale = 0.05f;
    public float maxScale = 0.5f;
    public float scaleSensitivity = 0.001f; // 新增：缩放灵敏度控制

    private ARRaycastManager _raycastManager;
    private List<GameObject> _spawnedSpheres = new List<GameObject>();
    private static List<ARRaycastHit> _planeHits = new List<ARRaycastHit>();

    // 交互状态管理
    private GameObject _selectedSphere;
    private InteractionMode _currentMode = InteractionMode.None;
    private float _touchStartTime;
    private Vector2 _touchStartPosition;
    private bool _isLongPress = false;
    private bool _isProcessingLongPress = false; // 新增：防止长按期间误操作
    private float _initialDistance;
    private Vector3 _initialScale;

    private enum InteractionMode
    {
        None,
        Placing,
        Dragging,
        Scaling,
        WaitingForLongPress
    }

    void Awake()
    {
        _raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // 重置长按处理状态（重要修复）
        if (Input.touchCount == 0)
        {
            _isProcessingLongPress = false;
        }

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touch.position);
                    break;

                case TouchPhase.Moved:
                    OnTouchMoved(touch.position);
                    break;

                case TouchPhase.Stationary:
                    OnTouchStationary(touch.position);
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    OnTouchEnded();
                    break;
            }
        }

        // 处理双指缩放
        if (Input.touchCount == 2)
        {
            HandleTwoFingerTouch();
        }
    }

    private void OnTouchBegan(Vector2 touchPosition)
    {
        _touchStartTime = Time.time;
        _touchStartPosition = touchPosition;
        _isLongPress = false;
        _isProcessingLongPress = false;

        // 首先检测是否点击到球体
        GameObject hitSphere = GetSphereAtPosition(touchPosition);
        if (hitSphere != null)
        {
            _selectedSphere = hitSphere;
            _currentMode = InteractionMode.Dragging;

            // 立即触发点击反馈（重要修复）
            SphereInteraction interaction = _selectedSphere.GetComponent<SphereInteraction>();
            if (interaction != null)
            {
                interaction.OnSphereClicked();
            }

            Debug.Log("选中球体，进入拖拽模式");
        }
        else
        {
            // 没有点击球体，准备放置新球体
            _currentMode = InteractionMode.Placing;
            Debug.Log("未选中球体，准备放置新球体");
        }
    }

    private void OnTouchMoved(Vector2 touchPosition)
    {
        if (_currentMode == InteractionMode.Dragging && _selectedSphere != null)
        {
            // 拖拽球体
            UpdateDrag(touchPosition);
        }
    }

    private void OnTouchStationary(Vector2 touchPosition)
    {
        // 长按检测 - 只有在选中球体且没有移动的情况下
        if (_currentMode == InteractionMode.Dragging &&
            _selectedSphere != null &&
            !_isProcessingLongPress &&
            Time.time - _touchStartTime > longPressDuration)
        {
            _isProcessingLongPress = true;
            DeleteSelectedSphere();
        }
    }

    private void OnTouchEnded()
    {
        // 如果是放置模式且没有处理长按，则放置球体
        if (_currentMode == InteractionMode.Placing && !_isProcessingLongPress)
        {
            PlaceSphere(_touchStartPosition);
        }

        // 重置状态
        _currentMode = InteractionMode.None;
        _selectedSphere = null;

        Debug.Log("触摸结束，重置状态");
    }

    private void HandleTwoFingerTouch()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        // 检测双指是否在同一个球体上
        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            GameObject sphere1 = GetSphereAtPosition(touch1.position);
            GameObject sphere2 = GetSphereAtPosition(touch2.position);

            if (sphere1 != null && sphere2 != null && sphere1 == sphere2)
            {
                _selectedSphere = sphere1;
                _currentMode = InteractionMode.Scaling;

                // 记录初始距离和缩放
                _initialDistance = Vector2.Distance(touch1.position, touch2.position);
                _initialScale = _selectedSphere.transform.localScale;

                Debug.Log("开始双指缩放");
            }
        }
        else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) &&
                 _currentMode == InteractionMode.Scaling)
        {
            // 更新缩放
            UpdateScaling(touch1.position, touch2.position);
        }
        else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
        {
            // 结束缩放
            if (_currentMode == InteractionMode.Scaling)
            {
                _currentMode = InteractionMode.None;
                _selectedSphere = null;
                Debug.Log("结束双指缩放");
            }
        }
    }

    private GameObject GetSphereAtPosition(Vector2 screenPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit hit;

        int sphereLayer = LayerMask.NameToLayer("Sphere");
        if (sphereLayer == -1)
        {
            Debug.LogError("请创建名为'Sphere'的层级！");
            return null;
        }

        int layerMask = (1 << sphereLayer);

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    private void PlaceSphere(Vector2 touchPos)
    {
        _planeHits.Clear();

        if (_raycastManager.Raycast(touchPos, _planeHits, TrackableType.PlaneWithinPolygon))
        {
            // 检查是否超过最大球体数量
            if (_spawnedSpheres.Count >= maxSpheres)
            {
                GameObject oldestSphere = _spawnedSpheres[0];
                _spawnedSpheres.RemoveAt(0);
                Destroy(oldestSphere);
                Debug.Log("达到最大球体数量，删除最旧的球体");
            }

            foreach (var hit in _planeHits)
            {
                var plane = hit.trackable as ARPlane;
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    Pose hitPose = hit.pose;
                    GameObject newSphere = Instantiate(spherePrefab, hitPose.position, Quaternion.identity);
                    _spawnedSpheres.Add(newSphere);

                    Debug.Log($"放置球体，当前总数: {_spawnedSpheres.Count}");
                    return;
                }
            }
        }
    }

    private void UpdateDrag(Vector2 touchPos)
    {
        if (_selectedSphere == null) return;

        _planeHits.Clear();
        if (_raycastManager.Raycast(touchPos, _planeHits, TrackableType.PlaneWithinPolygon))
        {
            foreach (var hit in _planeHits)
            {
                var plane = hit.trackable as ARPlane;
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    // 直接移动球体到新位置
                    _selectedSphere.transform.position = hit.pose.position;
                    break;
                }
            }
        }
    }

    private void UpdateScaling(Vector2 touchPos1, Vector2 touchPos2)
    {
        if (_selectedSphere == null) return;

        float currentDistance = Vector2.Distance(touchPos1, touchPos2);

        // 使用灵敏度系数控制缩放速度（重要修复）
        float distanceChange = currentDistance - _initialDistance;
        float scaleFactor = 1.0f + distanceChange * scaleSensitivity;

        Vector3 newScale = _initialScale * scaleFactor;

        // 限制缩放范围
        newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
        newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
        newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

        _selectedSphere.transform.localScale = newScale;
    }

    private void DeleteSelectedSphere()
    {
        if (_selectedSphere != null)
        {
            string sphereName = _selectedSphere.name;
            _spawnedSpheres.Remove(_selectedSphere);
            Destroy(_selectedSphere);
            _selectedSphere = null;
            _currentMode = InteractionMode.None;

            Debug.Log($"删除球体: {sphereName}, 剩余总数: {_spawnedSpheres.Count}");
        }
    }

    // 公共方法：清空所有球体
    public void ClearAllSpheres()
    {
        foreach (GameObject sphere in _spawnedSpheres)
        {
            Destroy(sphere);
        }
        _spawnedSpheres.Clear();
        Debug.Log("清空所有球体");
    }
}
