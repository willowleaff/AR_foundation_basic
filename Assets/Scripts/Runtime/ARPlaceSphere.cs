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
//        public GameObject spherePrefab; // 拖拽场景中球体到此处（作为预制体）
//        private ARRaycastManager _raycastManager;	    
//        private bool _hasPlaced = false;
//        void Awake() => _raycastManager = GetComponent<ARRaycastManager>();
//        void Update()
//        {
//            if (_hasPlaced) return; // 只放置一次

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
using UnityEngine.XR.ARSubsystems; // 新增，引入TrackableType所在命名空间

namespace UnityEngine.XR.ARFoundation.Samples
{
    public class ARPlaceSphere : MonoBehaviour
    {
        public GameObject spherePrefab; // 拖拽场景中球体到此处（作为预制体）
        private ARRaycastManager _raycastManager;
        private bool _hasPlaced = false;
        void Awake() => _raycastManager = GetComponent<ARRaycastManager>();
        void Update()
        {
            if (_hasPlaced) return; // 只放置一次

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