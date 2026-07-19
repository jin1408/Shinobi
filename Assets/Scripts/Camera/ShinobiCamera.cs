using UnityEngine;

/// <summary>
/// 3rdPerson+Fly 스타일의 TPS 백뷰 카메라.
/// Main Camera에 부착하고, Player 태그가 붙은 오브젝트를 자동으로 추적한다.
/// 기존 CameraOrbit 컴포넌트는 비활성화하거나 제거할 것.
/// </summary>
public class ShinobiCamera : MonoBehaviour
{
    [Header("타겟")]
    public Transform player;

    [Header("카메라 오프셋")]
    public Vector3 pivotOffset = new Vector3(0.0f, 1.0f, 0.0f);
    public Vector3 camOffset = new Vector3(0.3f, 1.8f, -5.5f);

    [Header("회전 속도")]
    public float horizontalSpeed = 6f;
    public float verticalSpeed = 6f;

    [Header("수직 각도 제한")]
    public float maxVerticalAngle = 30f;
    public float minVerticalAngle = -60f;

    [Header("부드러움")]
    public float smooth = 10f;

    private float angleH = 0f;
    private float angleV = 0f;
    private Transform cam;
    private Vector3 smoothPivotOffset;
    private Vector3 smoothCamOffset;
    private Vector3 targetPivotOffset;
    private Vector3 targetCamOffset;
    private float defaultFOV;
    private float targetFOV;

    public float GetH => angleH;

    void Awake()
    {
        cam = transform;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (player == null)
        {
            Debug.LogError("ShinobiCamera: Player를 찾을 수 없습니다. Player 태그를 확인하세요.");
            return;
        }

        cam.position = player.position + Quaternion.identity * pivotOffset + Quaternion.identity * camOffset;
        cam.rotation = Quaternion.identity;

        smoothPivotOffset = pivotOffset;
        smoothCamOffset = camOffset;
        defaultFOV = cam.GetComponent<Camera>().fieldOfView;
        targetFOV = defaultFOV;
        angleH = player.eulerAngles.y;

        targetPivotOffset = pivotOffset;
        targetCamOffset = camOffset;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (player == null) return;

        angleH += Mathf.Clamp(Input.GetAxis("Mouse X"), -1, 1) * horizontalSpeed;
        angleV += Mathf.Clamp(Input.GetAxis("Mouse Y"), -1, 1) * verticalSpeed;
        angleV = Mathf.Clamp(angleV, minVerticalAngle, maxVerticalAngle);

        Quaternion camYRotation = Quaternion.Euler(0, angleH, 0);
        Quaternion aimRotation = Quaternion.Euler(-angleV, angleH, 0);
        cam.rotation = aimRotation;

        cam.GetComponent<Camera>().fieldOfView = Mathf.Lerp(cam.GetComponent<Camera>().fieldOfView, targetFOV, Time.deltaTime);

        Vector3 baseTempPosition = player.position + camYRotation * targetPivotOffset;
        Vector3 noCollisionOffset = targetCamOffset;

        while (noCollisionOffset.magnitude >= 0.2f)
        {
            if (DoubleViewingPosCheck(baseTempPosition + aimRotation * noCollisionOffset))
                break;
            noCollisionOffset -= noCollisionOffset.normalized * 0.2f;
        }
        if (noCollisionOffset.magnitude < 0.2f)
            noCollisionOffset = Vector3.zero;

        smoothPivotOffset = Vector3.Lerp(smoothPivotOffset, targetPivotOffset, smooth * Time.deltaTime);
        smoothCamOffset = Vector3.Lerp(smoothCamOffset, noCollisionOffset, smooth * Time.deltaTime);

        cam.position = player.position + camYRotation * smoothPivotOffset + aimRotation * smoothCamOffset;
    }

    bool DoubleViewingPosCheck(Vector3 checkPos)
    {
        Vector3 target = player.position + pivotOffset;
        Vector3 direction = target - checkPos;
        if (Physics.SphereCast(checkPos, 0.2f, direction, out RaycastHit hit, direction.magnitude))
        {
            if (hit.transform != player && !hit.transform.GetComponent<Collider>().isTrigger)
                return false;
        }

        Vector3 origin = player.position + pivotOffset;
        Vector3 dir2 = checkPos - origin;
        if (Physics.SphereCast(origin, 0.2f, dir2, out RaycastHit hit2, dir2.magnitude))
        {
            if (hit2.transform != player && hit2.transform != transform && !hit2.transform.GetComponent<Collider>().isTrigger)
                return false;
        }

        return true;
    }

    public void SetFOV(float fov)
    {
        targetFOV = fov;
    }

    public void ResetFOV()
    {
        targetFOV = defaultFOV;
    }
}
