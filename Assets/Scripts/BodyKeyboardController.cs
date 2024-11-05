using UnityEngine;

public class BodyKeyboardController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float rotationSpeed = 100f;

    void Update()
    {
        float moveDirection = Input.GetAxis("Vertical");
        float strafeDirection = Input.GetAxis("Horizontal");
        float rotationDirection = 0;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotationDirection = -1;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            rotationDirection = 1;
        }

        Vector3 move = transform.forward * moveDirection * moveSpeed * Time.deltaTime;
        Vector3 strafe = transform.right * strafeDirection * moveSpeed * Time.deltaTime;

        transform.position += move + strafe;
        transform.Rotate(0, rotationDirection * rotationSpeed * Time.deltaTime, 0);
    }
}
