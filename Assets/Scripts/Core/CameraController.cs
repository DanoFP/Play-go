using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement")]
    public float MoveSpeed = 20f;
    public float EdgeScrollThreshold = 30f;
    public bool UseEdgeScrolling = true;

    [Header("Zoom")]
    public float ZoomSpeed = 5f;
    public float MinZoom = 5f;
    public float MaxZoom = 60f;

    [Header("Rotation")]
    public float RotationSpeed = 100f;

    [Header("Bounds")]
    public Vector2 BoundsMin = new Vector2(-100f, -100f);
    public Vector2 BoundsMax = new Vector2(100f, 100f);

    private Camera _cam;
    private float _targetZoom;
    private bool _isDragging = false;
    private Vector3 _dragOrigin;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;
        _targetZoom = _cam.orthographic ? _cam.orthographicSize : _cam.fieldOfView;
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleEdgeScrolling();
        HandleZoom();
        HandleRotation();
        HandleMiddleMouseDrag();
        ClampPosition();
    }

    void HandleKeyboardMovement()
    {
        Vector3 move = Vector3.zero;

        // For top-down orthographic: pan on XZ plane
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    move += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))   move -= Vector3.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))   move -= Vector3.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))  move += Vector3.right;

        if (move.magnitude > 0.01f) move = move.normalized;
        var pos = transform.position;
        pos.x += move.x * MoveSpeed * Time.deltaTime;
        pos.z += move.z * MoveSpeed * Time.deltaTime;
        transform.position = pos;
    }

    void HandleEdgeScrolling()
    {
        if (!UseEdgeScrolling) return;

        Vector3 mousePos = Input.mousePosition;
        float dx = 0f, dz = 0f;

        if (mousePos.x < EdgeScrollThreshold)                      dx = -1f;
        else if (mousePos.x > Screen.width - EdgeScrollThreshold)  dx =  1f;
        if (mousePos.y < EdgeScrollThreshold)                      dz = -1f;
        else if (mousePos.y > Screen.height - EdgeScrollThreshold) dz =  1f;

        var pos = transform.position;
        pos.x += dx * MoveSpeed * 0.7f * Time.deltaTime;
        pos.z += dz * MoveSpeed * 0.7f * Time.deltaTime;
        transform.position = pos;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetZoom -= scroll * ZoomSpeed * 10f;
            _targetZoom = Mathf.Clamp(_targetZoom, MinZoom, MaxZoom);
        }

        if (_cam.orthographic)
            _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetZoom, Time.deltaTime * 8f);
        else
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetZoom, Time.deltaTime * 8f);
    }

    void HandleRotation()
    {
        // Top-down camera: Q/E rotate the view around Y axis
        if (Input.GetKey(KeyCode.Q))
            transform.Rotate(Vector3.up, -RotationSpeed * Time.deltaTime, Space.World);
        if (Input.GetKey(KeyCode.E))
            transform.Rotate(Vector3.up, RotationSpeed * Time.deltaTime, Space.World);
    }

    void HandleMiddleMouseDrag()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _isDragging = true;
            _dragOrigin = GetGroundPoint(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(2))
            _isDragging = false;

        if (_isDragging)
        {
            Vector3 current = GetGroundPoint(Input.mousePosition);
            Vector3 diff = _dragOrigin - current;
            diff.y = 0;
            transform.position += diff;
        }
    }

    Vector3 GetGroundPoint(Vector3 screenPos)
    {
        Ray ray = _cam.ScreenPointToRay(screenPos);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float enter))
            return ray.GetPoint(enter);
        return Vector3.zero;
    }

    void ClampPosition()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, BoundsMin.x, BoundsMax.x);
        pos.z = Mathf.Clamp(pos.z, BoundsMin.y, BoundsMax.y);
        transform.position = pos;
    }

    public void FocusOn(Vector3 worldPos)
    {
        Vector3 pos = worldPos;
        pos.y = transform.position.y;
        transform.position = pos;
    }
}
