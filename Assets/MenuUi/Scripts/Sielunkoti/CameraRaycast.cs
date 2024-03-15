using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace MenuUI.Scripts.SoulHome
{
    public class CameraRaycast : MonoBehaviour
    {
        private Camera Camera;
        private Vector3 prevWideCameraPos = new(0,0);
        private float prevWideCameraFoV;
        private GameObject selectedRoom = null;
        private GameObject tempSelectedRoom = null;
        [SerializeField]
        private CameraController mainScreen;
        [SerializeField]
        private float scrollSpeed = 2f;
        [SerializeField]
        private float scrollSpeedMouse = 2f;
        [SerializeField]
        private SpriteRenderer backgroundSprite;
        [SerializeField]
        private bool isometric = false;
        [SerializeField]
        private SoulHomeController _soulHomeController;
        [SerializeField]
        private RawImage _displayScreen;

        private GameObject _selectedFurniture;
        private GameObject _tempSelectedFurniture;
        private float _startFurnitureTime;
        private float _tempRoomHitStart;

        private Bounds cameraBounds;
        private float cameraMinX;
        private float cameraMinY;
        private float cameraMaxX;
        private float cameraMaxY;

        private Vector2 prevp;
        private bool cameraMove = false;

        private float outDelay = 0;
        private float inDelay = 0;

        public GameObject SelectedRoom { get => selectedRoom; private set => selectedRoom = value; }
        public GameObject SelectedFurniture { get => _selectedFurniture; private set => _selectedFurniture = value; }

        // Start is called before the first frame update
        void Start()
        {
            Camera = GetComponent<Camera>();
            cameraBounds = backgroundSprite.bounds;
            cameraMinX = cameraBounds.min.x;
            cameraMinY = cameraBounds.min.y;
            cameraMaxX = cameraBounds.max.x;
            cameraMaxY = cameraBounds.max.y;
            Debug.Log(_displayScreen.GetComponent<RectTransform>().rect.x /*.sizeDelta.x*/ +" : "+ _displayScreen.GetComponent<RectTransform>().rect.y /*.sizeDelta.y*/);
            //Camera.aspect = _displayScreen.GetComponent<RectTransform>().sizeDelta.x / _displayScreen.GetComponent<RectTransform>().sizeDelta.y;
            Camera.aspect = _displayScreen.GetComponent<RectTransform>().rect.x / _displayScreen.GetComponent<RectTransform>().rect.y;
            if (Application.platform is RuntimePlatform.Android or RuntimePlatform.IPhonePlayer or RuntimePlatform.WebGLPlayer || AppPlatform.IsSimulator) Camera.fieldOfView = 100;
            else if (AppPlatform.IsEditor) Camera.fieldOfView = 45;
            transform.localPosition = new(0, 0, transform.position.z);
            Vector3 bl = Camera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.transform.position.z)));
            Vector3 tr = Camera.ViewportToWorldPoint(new Vector3(1, 1, Mathf.Abs(Camera.transform.position.z)));
            float currentX = transform.position.x;
            float currentY = transform.position.y;
            float offsetX = Mathf.Abs(currentX - bl.x);
            float offsetY = Mathf.Abs(currentY - bl.y);

            Debug.Log(currentY + " : " + (cameraMinY + offsetY) + " : " + (cameraMaxY - offsetY));
            Debug.Log(currentX + " : " + (cameraMinX + offsetX) + " : " + (cameraMaxX - offsetX));

            float y = Mathf.Clamp(currentY, cameraMinY + offsetY, cameraMaxY - offsetY);
            float x = Mathf.Clamp(currentX, cameraMinX + offsetX, cameraMaxX - offsetX);
            transform.position = new(x, y, transform.position.z);
        }
            // Update is called once per frame
            void Update()
        {
            Touch touch = new();
            if(Input.touchCount > 0) touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began || prevp == Vector2.zero) prevp = touch.position;
            if ((Input.GetMouseButton(0) || Input.touchCount == 1) && cameraMove)
            {
                if (selectedRoom == null)
                {
                    Vector3 bl = Camera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.transform.position.z)));
                    float currentY = transform.position.y;
                    float offsetY = Mathf.Abs(currentY-bl.y);
                    float targetY;
                    if (Input.touchCount == 1)
                    {
                        touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began || prevp == Vector2.zero) prevp = touch.position;
                        Vector2 lp = touch.position;
                        targetY = currentY + (prevp.y - lp.y) / scrollSpeed;
                        //Debug.Log("Touch: Y: "+(prevp.y - lp.y));
                        prevp = touch.position;
                        if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled) prevp = Vector2.zero;
                    }
                    else
                    {
                        float moveAmountY = Input.GetAxis("Mouse Y");
                        targetY = currentY - moveAmountY * scrollSpeedMouse;
                    }
                    float y;
                    if (cameraMinY + offsetY < cameraMaxY - offsetY)
                    {
                        y = Mathf.Clamp(targetY, cameraMinY + offsetY, cameraMaxY - offsetY);
                    }
                    else
                    {
                        y = (cameraMinY+ cameraMaxY) /2;
                    }

                    float currentX = transform.position.x;
                    float offsetX = Mathf.Abs(currentX - bl.x);
                    //float moveAmountX = Input.GetAxis("Mouse X");
                    float moveAmountX = 0;
                    float targetX = currentX - moveAmountX * scrollSpeedMouse;

                    float x;
                    if (cameraMinX + offsetX < cameraMaxX - offsetX)
                    {
                        x = Mathf.Clamp(targetX, cameraMinX + offsetX, cameraMaxX - offsetX);
                    }
                    else
                    {
                        x = (cameraMinX + cameraMaxX) / 2;
                    }
                    transform.position = new(x, y, transform.position.z);
                }
                else if(_selectedFurniture == null)
                {
                    Bounds roomCameraBounds = selectedRoom.GetComponent<BoxCollider2D>().bounds;
                    float roomCameraMinX = roomCameraBounds.min.x;
                    float roomCameraMinY = roomCameraBounds.min.y;
                    float roomCameraMaxX = roomCameraBounds.max.x;
                    float roomCameraMaxY = roomCameraBounds.max.y;
                    Vector3 bl = Camera.ViewportToWorldPoint(new Vector3(0, 0, Mathf.Abs(Camera.transform.position.z)));
                    float currentX = transform.position.x;
                    float offsetX = Mathf.Abs(currentX - bl.x);
                    float targetX;
                    if (Input.touchCount == 1)
                    {
                        touch = Input.GetTouch(0);
                        if (touch.phase == TouchPhase.Began || prevp == Vector2.zero) prevp = touch.position;
                        Vector2 lp = touch.position;
                        targetX = currentX + (prevp.x - lp.x) / scrollSpeed;
                        //Debug.Log("Touch: X: " + (prevp.x - lp.x));
                        prevp = touch.position;
                        if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled) prevp = Vector2.zero;
                    }
                    else
                    {
                        float moveAmountY = Input.GetAxis("Mouse X");
                        targetX = currentX - moveAmountY * scrollSpeedMouse;
                    }
                    float x;
                    if (roomCameraMinX + offsetX < roomCameraMaxX - offsetX)
                    {
                        x = Mathf.Clamp(targetX, roomCameraMinX + offsetX, roomCameraMaxX - offsetX);
                    }
                    else
                    {
                        x = (roomCameraMinX + roomCameraMaxX) / 2;
                    }
                    transform.position = new(x, transform.position.y, transform.position.z);
                }
                cameraMove = false;
            }
            if (touch.phase is TouchPhase.Ended or TouchPhase.Canceled) prevp = Vector2.zero;
        }

        void OnEnable()
        {
            Camera.aspect = _displayScreen.GetComponent<RectTransform>().rect.x / _displayScreen.GetComponent<RectTransform>().rect.y;
        }

        public bool FindRayPoint(Vector2 relPoint, ClickState click)
        {
            //if (click == ClickState.Start) cameraMove = true;
            //if(click is ClickState.Hold or ClickState.Move) cameraMove = true;
            //if (click == ClickState.End) cameraMove = false;
            cameraMove = true;


            Ray ray = Camera.ViewportPointToRay(relPoint);
            RaycastHit2D[] hit;
            //Debug.Log("Camera2: " + ray);
            hit = Physics2D.GetRayIntersectionAll(ray, 1000);
            bool hitRoom = false;
            bool enterRoom = false;
            Vector2 hitPoint = Vector2.zero;
            GameObject furnitureObject = null;
            foreach (RaycastHit2D hit2 in hit)
            {
                if (hit2.collider != null)
                {
                    if (hit2.collider.gameObject.tag == "ScrollRectCanvas")
                    {
                        hitPoint = hit2.point;
                        continue;
                    }

                    if (hit2.collider.gameObject.tag == "Furniture" && selectedRoom != null)
                    {
                        Debug.Log("Furniture");
                        if(selectedRoom == null) continue;
                        else
                        {
                            GameObject furnitureObjectHit = hit2.collider.gameObject;
                            if(furnitureObject == null) furnitureObject = furnitureObjectHit;
                            else
                            {
                                if (furnitureObjectHit.GetComponent<FurnitureHandling>().checkTopCollider(hit2.point.y))
                                {
                                    if (furnitureObject.GetComponent<FurnitureHandling>().checkTopCollider(hit2.point.y)) //Check for an edgecase where the contact point is at the top of the collider on both objects.
                                    {
                                        if (furnitureObject.GetComponent<SpriteRenderer>().sortingOrder < furnitureObjectHit.GetComponent<SpriteRenderer>().sortingOrder)
                                        {
                                            furnitureObject = furnitureObjectHit;
                                        }
                                    }
                                }
                                else
                                if (furnitureObject.GetComponent<SpriteRenderer>().sortingOrder < furnitureObjectHit.GetComponent<SpriteRenderer>().sortingOrder)
                                {
                                    furnitureObject = furnitureObjectHit;
                                }
                            }
                            //_furnitureList.Add(furnitureObject);
                        }
                    } 

                    if (hit2.collider.gameObject.tag == "Room")
                    {
                        //Debug.Log("Camera2: " + hit2.collider.gameObject.name);
                        //Vector3 hitPoint = hit2.transform.InverseTransformPoint(hit2.point);
                        //Debug.Log("Camera2: " + hitPoint);
                        //Debug.Log("Camera2: " + click);
                        GameObject roomObject;
                        if (isometric)
                            roomObject = hit2.collider.gameObject.transform.parent.parent.gameObject;
                        else
                            roomObject = hit2.collider.gameObject;
                        if (click == ClickState.Start)
                        {
                            tempSelectedRoom = roomObject;
                            _tempRoomHitStart = transform.position.y;
                        }
                        else if (click is ClickState.Move or ClickState.Hold && tempSelectedRoom != null)
                        {
                            if (tempSelectedRoom != roomObject) tempSelectedRoom = null;
                        }

                        else if (click == ClickState.End)
                        {
                            if (selectedRoom == null && tempSelectedRoom != null && _tempRoomHitStart > transform.position.y - 3f && _tempRoomHitStart < transform.position.y + 3f)
                            {
                                selectedRoom = tempSelectedRoom;
                                ZoomIn(selectedRoom);
                                //enterRoom = true;
                            }
                            else if (selectedRoom != roomObject && tempSelectedRoom != null )
                            {
                                ZoomOut();
                            }
                        }
                        hitRoom = true;
                    }
                }

            }
            if ((Input.GetMouseButton(0) || Input.GetMouseButtonUp(0) || Input.touchCount == 1) && (furnitureObject != null || _selectedFurniture != null))
            {
                Debug.Log(furnitureObject);
                //Touch touch = Input.GetTouch(0);
                if (click == ClickState.Start)
                {
                    if (_tempSelectedFurniture == null)
                    {
                        _tempSelectedFurniture = furnitureObject;
                        _startFurnitureTime = Time.time;
                    }
                }
                else if (_startFurnitureTime + 1 <= Time.time && _tempSelectedFurniture != null && _selectedFurniture == null)
                {
                    _selectedFurniture = _tempSelectedFurniture;
                    Color color = _selectedFurniture.GetComponent<SpriteRenderer>().color;
                    color.a = 0.5f;
                    _selectedFurniture.GetComponent<SpriteRenderer>().color = color;
                }
                else if (click is ClickState.Hold or ClickState.Move && _selectedFurniture != null)
                {
                    Vector2 checkPoint;
                    FurnitureSize size = _selectedFurniture.GetComponent<FurnitureHandling>().Furniture.Size;
                    if (size is FurnitureSize.OneXOne)
                        checkPoint = hitPoint + new Vector2(0, (_selectedFurniture.transform.localScale.y / 2) * -1);
                    else if(size is FurnitureSize.OneXTwo)
                        checkPoint = hitPoint + new Vector2((_selectedFurniture.transform.localScale.x / 2)/2 * -1, (_selectedFurniture.transform.localScale.y / 2) * -1);
                    else checkPoint = hitPoint + new Vector2(0, (_selectedFurniture.transform.localScale.y / 2) * -1);

                    //Debug.Log("HitPoint: "+ hitPoint);
                    //Debug.Log("CheckPoint: " + checkPoint);


                    bool check = selectedRoom.GetComponent<RoomData>().HandleFurniturePosition(checkPoint, Camera, _selectedFurniture, true);

                    if(!check)_selectedFurniture.transform.position = hitPoint + new Vector2(0,(_selectedFurniture.transform.localScale.y/2)*-1);
                }
                else if (click is ClickState.End /*or ClickState.Move*/ || furnitureObject != _tempSelectedFurniture)
                {
                    _tempSelectedFurniture = null;
                    if (_selectedFurniture != null) {

                        Vector2 checkPoint;
                        FurnitureSize size = _selectedFurniture.GetComponent<FurnitureHandling>().Furniture.Size;
                        if (size is FurnitureSize.OneXOne)
                            checkPoint = hitPoint + new Vector2(0, (_selectedFurniture.transform.localScale.y / 2) * -1);
                        else if (size is FurnitureSize.OneXTwo)
                            checkPoint = hitPoint + new Vector2((_selectedFurniture.transform.localScale.x / 2) / 2 * -1, (_selectedFurniture.transform.localScale.y / 2) * -1);
                        else checkPoint = hitPoint + new Vector2(0, (_selectedFurniture.transform.localScale.y / 2) * -1);

                        selectedRoom.GetComponent<RoomData>().HandleFurniturePosition(checkPoint, Camera, _selectedFurniture, false);

                        Color color = _selectedFurniture.GetComponent<SpriteRenderer>().color;
                        color.a = 1f;
                        _selectedFurniture.GetComponent<SpriteRenderer>().color = color;
                        _selectedFurniture.GetComponent<FurnitureHandling>().ResetFurniturePosition();
                        _selectedFurniture = null;
                    }
                }

            }


            if (!hitRoom && selectedRoom != null)
            {
                ZoomOut();
            }
            return enterRoom;
        }

        public void ZoomIn(GameObject room)
        {
            if (inDelay + 1f < Time.time)
            {
                _soulHomeController.SetRoomName(selectedRoom);
                prevWideCameraPos = Camera.transform.position;
                prevWideCameraFoV = Camera.fieldOfView;
                Camera.transform.position = new(room.transform.position.x, room.transform.position.y + 10f, -27.5f);
                if (Application.platform is RuntimePlatform.Android or RuntimePlatform.IPhonePlayer or RuntimePlatform.WebGLPlayer || AppPlatform.IsSimulator) Camera.fieldOfView = 60;
                else if (AppPlatform.IsEditor) Camera.fieldOfView = 60;
                outDelay = Time.time;
                mainScreen.EnableTray(true);

                //_displayScreen.GetComponent<RectTransform>().anchorMin = new(_displayScreen.GetComponent<RectTransform>().anchorMin.x, 0.4f);
                //_displayScreen.GetComponent<RectTransform>().anchorMax = new(_displayScreen.GetComponent<RectTransform>().anchorMax.x, 0.6f);
                //Camera.aspect = _displayScreen.GetComponent<RectTransform>().rect.x / _displayScreen.GetComponent<RectTransform>().rect.y;
            }
        }

        public void ZoomOut()
        {
            if (selectedRoom != null && outDelay + 1f < Time.time)
            {
                if (mainScreen.TrayOpen) mainScreen.ToggleTray();
                selectedRoom = null;
                _soulHomeController.SetRoomName(selectedRoom);
                Camera.transform.position = prevWideCameraPos;
                Camera.fieldOfView = prevWideCameraFoV;
                inDelay = Time.time;
                mainScreen.EnableTray(false);

                //_displayScreen.GetComponent<RectTransform>().anchorMin = new(_displayScreen.GetComponent<RectTransform>().anchorMin.x, 0.2f);
                //_displayScreen.GetComponent<RectTransform>().anchorMax = new(_displayScreen.GetComponent<RectTransform>().anchorMax.x, 0.8f);
                //Camera.aspect = _displayScreen.GetComponent<RectTransform>().rect.x / _displayScreen.GetComponent<RectTransform>().rect.y;
            }
        }

        public void SetFurniture(GameObject furniture)
        {
            var furnitureObject = Instantiate(furniture.GetComponent<TrayFurniture>().FurnitureObject);
            furnitureObject.GetComponent<FurnitureHandling>().Furniture = furniture.GetComponent<TrayFurniture>().Furniture;
            furnitureObject.GetComponent<FurnitureHandling>().Position = new(-1, -1);
            furnitureObject.GetComponent<FurnitureHandling>().Slot = null;
            _selectedFurniture = furnitureObject;
            Color color = _selectedFurniture.GetComponent<SpriteRenderer>().color;
            color.a = 0.5f;
            _selectedFurniture.GetComponent<SpriteRenderer>().color = color;
        }

        public void RemoveFurniture()
        {
            if(_selectedFurniture.GetComponent<FurnitureHandling>().Slot != null)
                selectedRoom.GetComponent<RoomData>().FreeFurnitureSlots(_selectedFurniture.GetComponent<FurnitureHandling>().Furniture.Size, _selectedFurniture.GetComponent<FurnitureHandling>().Slot);
            Destroy(_selectedFurniture);
        }


        public void DeselectFurniture()
        {
            _tempSelectedFurniture = null;
            if (_selectedFurniture != null)
            {
                selectedRoom.GetComponent<RoomData>().ResetPosition(SelectedFurniture);
                Color color = _selectedFurniture.GetComponent<SpriteRenderer>().color;
                color.a = 1f;
                _selectedFurniture.GetComponent<SpriteRenderer>().color = color;
                _selectedFurniture.GetComponent<FurnitureHandling>().ResetFurniturePosition();
                _selectedFurniture = null;
            }
        }
    }
}
