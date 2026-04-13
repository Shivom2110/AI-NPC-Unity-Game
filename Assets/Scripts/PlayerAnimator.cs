using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimator : MonoBehaviour
{
    private Animator animator;
    private float currentSpeed;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();

        if (animator == null)
            Debug.LogError("No Animator found!");
    }

    void Update()
    {
        if (animator == null) return;

        float h = 0f;
        float v = 0f;

        if (Keyboard.current.wKey.isPressed) v =  1f;
        if (Keyboard.current.sKey.isPressed) v = -1f;
        if (Keyboard.current.aKey.isPressed) h = -1f;
        if (Keyboard.current.dKey.isPressed) h =  1f;

        float inputAmount = new Vector2(h, v).magnitude;
        bool isRunning    = Keyboard.current.leftShiftKey.isPressed;

        float targetSpeed = 0f;

        if (inputAmount > 0.1f)
            targetSpeed = isRunning ? 6f : 3f;

        // Smooth transition between speeds
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, 10f * Time.deltaTime);

        animator.SetFloat("Speed", currentSpeed);

        Debug.Log("Speed: " + currentSpeed);
    }
}