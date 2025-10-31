//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//namespace UnityEngine.XR.ARFoundation.Samples
//{
//    public class ARPlaceSphere : MonoBehaviour
//    {
//       // Start is called before the first frame update
//        //void Start()
//        //{

//        //}
//        //// Update is called once per frame
//        //void Update()
//        //{

//        //}
//        public GameObject spherePrefab; // ��ק���������嵽�˴�����ΪԤ���壩
//        private ARRaycastManager _raycastManager;	    
//        private bool _hasPlaced = false;
//        void Awake() => _raycastManager = GetComponent<ARRaycastManager>();
//        void Update()
//        {
//            if (_hasPlaced) return; // ֻ����һ��

//            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
//            {
//                Vector2 touchPos = Input.GetTouch(0).position;	           
//                List<ARRaycastHit> hits = new List<ARRaycastHit>();
//                if (_raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
//	            {	                
//                    Pose hitPose = hits[0].pose;
//                    Instantiate(spherePrefab, hitPose.position, hitPose.rotation);
//                    _hasPlaced = true;
//                }
//            }     
//        }

//    }
//}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARSubsystems; // ����������TrackableType���������ռ�

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ARPlaceSphere : MonoBehaviour
    {
        public GameObject spherePrefab; // ��ק���������嵽�˴�����ΪԤ���壩
        private ARRaycastManager _raycastManager;
        private bool _hasPlaced = false;
        void Awake() => _raycastManager = GetComponent<ARRaycastManager>();
        void Update()
        {
            if (_hasPlaced) return; // ֻ����һ��

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                Vector2 touchPos = Input.GetTouch(0).position;
                List<ARRaycastHit> hits = new List<ARRaycastHit>();
                if (_raycastManager.Raycast(touchPos, hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = hits[0].pose;
                    Instantiate(spherePrefab, hitPose.position, hitPose.rotation);
                    _hasPlaced = true;
                }
            }
        }
    }
}