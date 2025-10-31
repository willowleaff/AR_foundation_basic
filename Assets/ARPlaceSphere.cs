using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARPlaceSphere : MonoBehaviour
{
    [Header("��������")]
    public GameObject spherePrefab;
    public int maxSpheres = 10;

    [Header("��������")]
    public float longPressDuration = 1.0f;
    public float dragSpeed = 2.0f;
    public float minScale = 0.05f;
    public float maxScale = 0.5f;
    public float scaleSensitivity = 0.001f; // ���������������ȿ���

    private ARRaycastManager _raycastManager;
    private List<GameObject> _spawnedSpheres = new List<GameObject>();
    private static List<ARRaycastHit> _planeHits = new List<ARRaycastHit>();

    // ����״̬����
    private GameObject _selectedSphere;
    private InteractionMode _currentMode = InteractionMode.None;
    private float _touchStartTime;
    private Vector2 _touchStartPosition;
    private bool _isLongPress = false;
    private bool _isProcessingLongPress = false; // ��������ֹ�����ڼ������
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
        // ���ó�������״̬����Ҫ�޸���
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

        // ����˫ָ����
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

        // ���ȼ���Ƿ���������
        GameObject hitSphere = GetSphereAtPosition(touchPosition);
        if (hitSphere != null)
        {
            _selectedSphere = hitSphere;
            _currentMode = InteractionMode.Dragging;

            // �������������������Ҫ�޸���
            SphereInteraction interaction = _selectedSphere.GetComponent<SphereInteraction>();
            if (interaction != null)
            {
                interaction.OnSphereClicked();
            }

            Debug.Log("ѡ�����壬������קģʽ");
        }
        else
        {
            // û�е�����壬׼������������
            _currentMode = InteractionMode.Placing;
            Debug.Log("δѡ�����壬׼������������");
        }
    }

    private void OnTouchMoved(Vector2 touchPosition)
    {
        if (_currentMode == InteractionMode.Dragging && _selectedSphere != null)
        {
            // ��ק����
            UpdateDrag(touchPosition);
        }
    }

    private void OnTouchStationary(Vector2 touchPosition)
    {
        // ������� - ֻ����ѡ��������û���ƶ��������
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
        // ����Ƿ���ģʽ��û�д����������������
        if (_currentMode == InteractionMode.Placing && !_isProcessingLongPress)
        {
            PlaceSphere(_touchStartPosition);
        }

        // ����״̬
        _currentMode = InteractionMode.None;
        _selectedSphere = null;

        Debug.Log("��������������״̬");
    }

    private void HandleTwoFingerTouch()
    {
        Touch touch1 = Input.GetTouch(0);
        Touch touch2 = Input.GetTouch(1);

        // ���˫ָ�Ƿ���ͬһ��������
        if (touch1.phase == TouchPhase.Began || touch2.phase == TouchPhase.Began)
        {
            GameObject sphere1 = GetSphereAtPosition(touch1.position);
            GameObject sphere2 = GetSphereAtPosition(touch2.position);

            if (sphere1 != null && sphere2 != null && sphere1 == sphere2)
            {
                _selectedSphere = sphere1;
                _currentMode = InteractionMode.Scaling;

                // ��¼��ʼ���������
                _initialDistance = Vector2.Distance(touch1.position, touch2.position);
                _initialScale = _selectedSphere.transform.localScale;

                Debug.Log("��ʼ˫ָ����");
            }
        }
        else if ((touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved) &&
                 _currentMode == InteractionMode.Scaling)
        {
            // ��������
            UpdateScaling(touch1.position, touch2.position);
        }
        else if (touch1.phase == TouchPhase.Ended || touch2.phase == TouchPhase.Ended)
        {
            // ��������
            if (_currentMode == InteractionMode.Scaling)
            {
                _currentMode = InteractionMode.None;
                _selectedSphere = null;
                Debug.Log("����˫ָ����");
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
            Debug.LogError("�봴����Ϊ'Sphere'�Ĳ㼶��");
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
            // ����Ƿ񳬹������������
            if (_spawnedSpheres.Count >= maxSpheres)
            {
                GameObject oldestSphere = _spawnedSpheres[0];
                _spawnedSpheres.RemoveAt(0);
                Destroy(oldestSphere);
                Debug.Log("�ﵽ�������������ɾ����ɵ�����");
            }

            foreach (var hit in _planeHits)
            {
                var plane = hit.trackable as ARPlane;
                if (plane != null && plane.alignment == PlaneAlignment.HorizontalUp)
                {
                    Pose hitPose = hit.pose;
                    GameObject newSphere = Instantiate(spherePrefab, hitPose.position, Quaternion.identity);
                    _spawnedSpheres.Add(newSphere);

                    Debug.Log($"�������壬��ǰ����: {_spawnedSpheres.Count}");
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
                    // ֱ���ƶ����嵽��λ��
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

        // ʹ��������ϵ�����������ٶȣ���Ҫ�޸���
        float distanceChange = currentDistance - _initialDistance;
        float scaleFactor = 1.0f + distanceChange * scaleSensitivity;

        Vector3 newScale = _initialScale * scaleFactor;

        // �������ŷ�Χ
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

            Debug.Log($"ɾ������: {sphereName}, ʣ������: {_spawnedSpheres.Count}");
        }
    }

    // ���������������������
    public void ClearAllSpheres()
    {
        foreach (GameObject sphere in _spawnedSpheres)
        {
            Destroy(sphere);
        }
        _spawnedSpheres.Clear();
        Debug.Log("�����������");
    }
}
