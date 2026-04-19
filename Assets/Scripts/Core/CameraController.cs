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
        _targetZoom = _cam.fieldOfView;
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

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            move += transform.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            move -= transform.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            move -= transform.right;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            move += transform.right;

        // Keep movement horizontal
        move.y = 0f;
        if (move.magnitude > 0.01f)
            move = move.normalized;

        transform.position += move * MoveSpeed * Time.deltaTime;
    }

    void HandleEdgeScrolling()
    {
        if (!UseEdgeScrolling) return;

        Vector3 move = Vector3.zero;
        Vector3 mousePos = Input.mousePosition;

        if (mousePos.x < EdgeScrollThreshold) move -= transform.right;
        else if (mousePos.x > Screen.width - EdgeScrollThreshold) move += transform.right;
        if (mousePos.y < EdgeScrollThreshold) move -= transform.forward;
        else if (mousePos.y > Screen.height - EdgeScrollThreshold) move += transform.forward;

        move.y = 0f;
        transform.position += move * MoveSpeed * 0.7f * Time.deltaTime;
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _targetZoom -= scroll * ZoomSpeed * 10f;
            _targetZoom = Mathf.Clamp(_targetZoom, MinZoom, MaxZoom);
        }
        _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, _targetZoom, Time.deltaTime * 8f);
    }

    void HandleRotation()
    {
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
