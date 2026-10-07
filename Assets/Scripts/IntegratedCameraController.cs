using UnityEngine;

public class IntegratedCameraController : MonoBehaviour
{
    [Header("LLM Target Focus")]
    public Transform targetFocus;

    [Header("Speeds")]
    public float touchpadZoomSpeed = 900f;
    public float panSpeed = 2.0f;
    public float mouseDragSensitivity = 300f;

    [Header("Touch Speeds (mobile)")]
    [Tooltip("Single-finger drag se rotate karne ki sensitivity")]
    public float touchRotateSensitivity = 0.25f;
    [Tooltip("Pinch se zoom karne ki sensitivity")]
    public float touchZoomSpeed = 0.02f;
    [Tooltip("Do ungliyon se drag karke pan karne ki sensitivity")]
    public float touchPanSpeed = 0.0025f;

    [Header("Camera range")]
    public float minDistance = 0.8f;
    public float maxDistance = 27000f;
    public float stageFocusDistance = 15f;
    public float stageFocusXRot = 10f;
    public float stageFocusYRot = 0f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    public float minYaw = -1800f;
    public float maxYaw = 1800f;

    [Header("Smoothing")]
    public float rotationSmoothTime = 0.18f; // kam time = zyada snappy/free response
    public float zoomSmoothTime = 1.2f;
    public float panSmoothTime = 0.22f;
    public float centerSmoothTime = 1.2f;
    private Vector3 centerVelocity;

    [Header("DEBUG — Play mode me orbit karke yahan se xRot/yRot/distance copy karo")]
    [Tooltip("Ye live update hoti hai — Play mode me mouse se orbit karo, jab achi angle mil jaye to yahi 3 values ResetToDefaultView() me paste kardena")]
    public float debug_CurrentXRot;
    public float debug_CurrentYRot;
    public float debug_CurrentDistance;
    public Vector3 debug_CurrentPanOffset;

    // ---- Default pose (jab bhi model switch ho, isi par snap karna hai) ----
    private const float DefaultXRot = 15f;
    private const float DefaultYRot = -45f;
    private const float DefaultDistance = 36.0f;
    private static readonly Vector3 DefaultPanOffset = Vector3.zero;

    // ---- Target values (Second image wala side pose default angle) ----
    private float targetXRot = DefaultXRot;
    private float targetYRot = DefaultYRot;
    private float targetDistance = DefaultDistance;
    private Vector3 targetPanOffset = DefaultPanOffset;

    // ---- Smoothed values ----
    private float currentXRot = DefaultXRot;
    private float currentYRot = DefaultYRot;
    private float currentDistance = DefaultDistance;
    private Vector3 currentPanOffset = DefaultPanOffset;

    private float xRotVelocity, yRotVelocity, distanceVelocity;
    private Vector3 panVelocity;

    // Dynamic target tracking point
    private Vector3 calculatedCenterPoint;
    private bool useTargetFocusPosition;
    private bool allowUserRotation = true;
    private bool allowUserZoom = true;
    private bool allowHorizontalPan = true;
    private bool allowVerticalPan = true;

    void Start()
    {
        // SceneSwitcher ke single configured LLM model ko camera ka fallback target banao.
        if (targetFocus == null)
        {
            SceneSwitcher sceneSwitcher = SceneSwitcher.Instance;
            if (sceneSwitcher != null && sceneSwitcher.llmModel != null)
            {
                Transform origin = sceneSwitcher.llmModel.transform.Find("NetworkOrigin");
                targetFocus = origin != null ? origin : sceneSwitcher.llmModel.transform;
            }
        }

        // Pehli frame ke liye initial center set kar dete hain
        if (targetFocus != null)
        {
            calculatedCenterPoint = CalculateAbsoluteCenter(targetFocus);
        }
    }

    void LateUpdate()
    {
        if (targetFocus == null) return;

        // Har frame par automatically generated model ka perfect center analyze karna
        Vector3 rawCenterPoint = CalculateAbsoluteCenter(targetFocus);
        calculatedCenterPoint = Vector3.SmoothDamp(calculatedCenterPoint, rawCenterPoint, ref centerVelocity, centerSmoothTime);

        bool mouseOverUI = UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

        // === 1. MOUSE DRAG ROTATION ===
        if (allowUserRotation && !mouseOverUI && Input.GetMouseButton(0))
        {
            targetYRot += Input.GetAxis("Mouse X") * mouseDragSensitivity;
            targetXRot -= Input.GetAxis("Mouse Y") * mouseDragSensitivity;
        }

        // === 2. PAN (Right Click Drag) - model ko "utha kar idhar udhar" karne ke liye yehi hai ===
        if (!mouseOverUI && Input.GetMouseButton(1))
        {
            Vector3 right = transform.right;
            if (allowHorizontalPan)
                targetPanOffset -= right * Input.GetAxis("Mouse X") * panSpeed * targetDistance;
            if (allowVerticalPan)
                targetPanOffset -= transform.up * Input.GetAxis("Mouse Y") * panSpeed * targetDistance;
        }

        // === 3. SCROLL ZOOM ===
        if (allowUserZoom && !mouseOverUI)
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scrollDelta) > 0.001f)
            {
                targetDistance -= scrollDelta * touchpadZoomSpeed;
            }
        }

        // === 4. TOUCH CONTROLS (mobile) ===
        HandleTouchInput();

        // Rotation aur zoom bounds - zyada orbit / zoom range dena hai bina robot ko move kiye.
        targetXRot = Mathf.Clamp(targetXRot, minPitch, maxPitch);
        targetYRot = Mathf.Clamp(targetYRot, minYaw, maxYaw);
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

        // ---- Interpolation Engine (Smoothing) ----
        currentXRot = Mathf.SmoothDampAngle(currentXRot, targetXRot, ref xRotVelocity, rotationSmoothTime);
        currentYRot = Mathf.SmoothDampAngle(currentYRot, targetYRot, ref yRotVelocity, rotationSmoothTime);
        currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, 1f / zoomSmoothTime);
        currentPanOffset = Vector3.SmoothDamp(currentPanOffset, targetPanOffset, ref panVelocity, panSmoothTime);

        // DEBUG — Inspector me live dikhne ke liye (Play mode me orbit karke inhi ko copy karna)
        debug_CurrentXRot = currentXRot;
        debug_CurrentYRot = currentYRot;
        debug_CurrentDistance = currentDistance;
        debug_CurrentPanOffset = currentPanOffset;

        Quaternion targetRotation = Quaternion.Euler(currentXRot, currentYRot, 0);
        Vector3 reverseDistance = new Vector3(0f, 0f, -currentDistance);

        transform.rotation = targetRotation;
        
        // Ab camera real calculated center point par focus karega, chahe model kahin bhi spawn ho!
        transform.position = targetRotation * reverseDistance + calculatedCenterPoint + currentPanOffset;
    }

    /// <summary>
    /// Mobile/touchscreen input: 1 finger rotates, 2 fingers zoom and pan.
    /// Touches over UI are ignored so drawing does not move the camera.
    /// </summary>
    private void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (IsTouchOverUI(touch.fingerId)) return;

            if (allowUserRotation && touch.phase == TouchPhase.Moved)
            {
                targetYRot += touch.deltaPosition.x * touchRotateSensitivity;
                targetXRot -= touch.deltaPosition.y * touchRotateSensitivity;
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            if (IsTouchOverUI(touchZero.fingerId) || IsTouchOverUI(touchOne.fingerId)) return;

            Vector2 touchZeroPreviousPosition = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePreviousPosition = touchOne.position - touchOne.deltaPosition;

            if (allowUserZoom)
            {
                float previousDistance = (touchZeroPreviousPosition - touchOnePreviousPosition).magnitude;
                float currentDistanceBetweenFingers = (touchZero.position - touchOne.position).magnitude;
                float pinchDelta = currentDistanceBetweenFingers - previousDistance;
                targetDistance -= pinchDelta * touchZoomSpeed;
            }

            Vector2 averageDelta = (touchZero.deltaPosition + touchOne.deltaPosition) * 0.5f;
            if (allowHorizontalPan)
                targetPanOffset -= transform.right * averageDelta.x * touchPanSpeed * targetDistance;
            if (allowVerticalPan)
                targetPanOffset -= transform.up * averageDelta.y * touchPanSpeed * targetDistance;
        }
    }

    private bool IsTouchOverUI(int fingerId)
    {
        return UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(fingerId);
    }

    /// <summary>
    /// Jab bhi model switch ho (SceneSwitcher se), isay call karo taake camera us model
    /// ki apni default position/angle/zoom par snap ho jaye - pichle model par kiya
    /// rotate/zoom/pan is model par carry-over na ho.
    /// </summary>
    public void ResetToDefaultView()
    {
        ResetToDefaultView(DefaultXRot, DefaultYRot, DefaultDistance, DefaultPanOffset);
    }

    public void FocusOnStage(Transform focus)
    {
        FocusOnStage(focus, 1f);
    }

    public void FocusOnStage(Transform focus, float distanceMultiplier)
    {
        targetFocus = focus;
        useTargetFocusPosition = false;
        targetDistance = stageFocusDistance * distanceMultiplier;
        targetXRot = stageFocusXRot;
        targetYRot = stageFocusYRot;
        targetPanOffset = Vector3.zero;
    }

    public void SetPanOffset(Vector3 panOffset)
    {
        targetPanOffset = panOffset;
        panVelocity = Vector3.zero;
    }

    public void SetFocusOrigin(Transform focusOrigin)
    {
        targetFocus = focusOrigin;
        useTargetFocusPosition = true;
        calculatedCenterPoint = focusOrigin.position;
        centerVelocity = Vector3.zero;
    }

    public void SetDistance(float distance)
    {
        targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    public void SetUserControls(
        bool allowRotation,
        bool allowZoom,
        bool allowHorizontalPan,
        bool allowVerticalPan)
    {
        allowUserRotation = allowRotation;
        allowUserZoom = allowZoom;
        this.allowHorizontalPan = allowHorizontalPan;
        this.allowVerticalPan = allowVerticalPan;
    }

    // Ab panOffset bhi customize ho sakta hai — Y negative karo to camera neeche jayega
    public void ResetToDefaultView(float xRot, float yRot, float distance, Vector3 panOffset = default)
    {
        targetXRot = Mathf.Clamp(xRot, minPitch, maxPitch);
        targetYRot = yRot;
        targetDistance = distance;
        targetPanOffset = panOffset;

        currentXRot = targetXRot;
        currentYRot = yRot;
        currentDistance = distance;
        currentPanOffset = panOffset;

        xRotVelocity = 0f;
        yRotVelocity = 0f;
        distanceVelocity = 0f;
        panVelocity = Vector3.zero;
    }

    /// <summary>
    /// Yeh function pure model ke bache-kuche saare parts (children) ka center point mathematically calculate karta hai.
    /// </summary>
    private Vector3 CalculateAbsoluteCenter(Transform parentTransform)
    {
        if (useTargetFocusPosition)
            return parentTransform.position;

        // Agar parent ke andar koi baccha child object nahi hai, toh seedha parent ki position de do
        if (parentTransform.childCount == 0) return parentTransform.position;

        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        bool hasBounds = false;

        // Model ke har ek child (Cubes, Spheres, Nodes) ki positions collect karo
        foreach (Transform child in parentTransform)
        {
            if (!hasBounds)
            {
                bounds = new Bounds(child.position, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(child.position);
            }
        }

        // Return the exact calculated mathematical center point of the network mesh
        return hasBounds ? bounds.center : parentTransform.position;
    }
}