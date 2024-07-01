using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Speed")]
    public float moveSpeed = 10f;
    public float turnSpeed = 5f;
    [Space(10)]

    [Header("Rotation")]
    private float yaw = 0f;
    private float pitch = 0f;

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // Up- / Down- Movement
        float upward = 0f;
        if (Input.GetKey(KeyCode.E))
        {
            upward = 1f;
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            upward = -1f;
        }

        Vector3 direction = new Vector3(horizontal, upward, vertical);
        direction = transform.TransformDirection(direction);
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Camera rotation
        if (Input.GetMouseButton(1))
        {
            yaw += turnSpeed * Input.GetAxis("Mouse X");
            pitch -= turnSpeed * Input.GetAxis("Mouse Y");

            pitch = Mathf.Clamp(pitch, -90f, 90f);

            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }
    }
}
