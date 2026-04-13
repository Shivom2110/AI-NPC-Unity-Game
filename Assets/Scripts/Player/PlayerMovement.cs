using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed     = 2f;
    public float runSpeed      = 4f;
    public float gravity       = -9.81f;
    public float rotationSpeed = 10f;

    [Header("Jump")]
    public float jumpHeight = 1.2f;

    private CharacterController controller;
    private Transform cameraTransform;
    private Vector3 velocity;
    private Animator characterAnimator;
    private bool isGrounded;

    void Start()
    {
        controller        = GetComponent<CharacterController>();
        cameraTransform   = Camera.main.transform;
        characterAnimator = GetComponentInChildren<Animator>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void Update()
    {
        CheckGround();
        Move();
        HandleJump();
        ApplyGravity();
    }

    void CheckGround()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;
    }

    void Move()
    {
        float h = 0f;
        float v = 0f;

        if (Keyboard.current.wKey.isPressed) v =  1f;
        if (Keyboard.current.sKey.isPressed) v = -1f;
        if (Keyboard.current.aKey.isPressed) h = -1f;
        if (Keyboard.current.dKey.isPressed) h =  1f;

        Vector3 input = new Vector3(h, 0f, v).normalized;

        if (input.magnitude >= 0.1f)
        {
            // Direction relative to camera
            float targetAngle = Mathf.Atan2(input.x, input.z)
                                * Mathf.Rad2Deg
                                + cameraTransform.eulerAngles.y;

            // Smooth rotation
            float angle = Mathf.LerpAngle(
                transform.eulerAngles.y,
                targetAngle,
                rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            // Move direction
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f)
                              * Vector3.forward;

            bool isRunning = Keyboard.current.leftShiftKey.isPressed;
            float speed    = isRunning ? runSpeed : walkSpeed;

            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
    }

    void HandleJump()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            if (characterAnimator != null)
                characterAnimator.SetBool("IsJumping", true);
        }

        if (isGrounded)
        {
            if (characterAnimator != null)
                characterAnimator.SetBool("IsJumping", false);
        }
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}