using UnityEngine;
using UnityEngine.EventSystems; // 👈 necesario para detectar UI

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          
    public float distance = 8f;       
    public Vector2 minMaxDistance = new Vector2(4f, 20f);

    [Header("Rotación")]
    public float rotateSpeed = 200f;  
    public float minYAngle = 10f;     
    public float maxYAngle = 75f;     

    [Header("Zoom")]
    public float zoomSpeed = 5f;

    float currentYaw;   
    float currentPitch; 

    void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("OrbitCamera: No target set.");
            return;
        }

        Vector3 offset = transform.position - target.position;
        distance = offset.magnitude;

        Vector3 angles = Quaternion.LookRotation(-offset).eulerAngles;
        currentYaw = angles.y;
        currentPitch = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 🚫 Si el mouse está sobre UI, ignorar controles de cámara
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        // --- ROTACIÓN ---
        if (Input.GetMouseButton(0))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentYaw   += mouseX * rotateSpeed * Time.deltaTime;
            currentPitch -= mouseY * rotateSpeed * Time.deltaTime;
            currentPitch = Mathf.Clamp(currentPitch, minYAngle, maxYAngle);
        }

        // --- ZOOM ---
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            distance -= scroll * zoomSpeed;
            distance = Mathf.Clamp(distance, minMaxDistance.x, minMaxDistance.y);
        }

        // --- APLICAR ---
        Quaternion rot = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 dir = rot * Vector3.back;
        transform.position = target.position + dir * distance;
        transform.LookAt(target.position);
    }
}