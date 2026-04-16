using UnityEngine;

/// <summary>
/// Attach to the same GameObject as the Animator.
/// Blocks all root motion EXCEPT Roll, which uses root motion
/// to move the character to the correct end position.
/// </summary>
[RequireComponent(typeof(Animator))]
public class RootMotionBlocker : MonoBehaviour
{
    private Animator _animator;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void OnAnimatorMove()
    {
        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);

        if (state.IsName("Roll"))
        {
            // Apply root motion position to the root parent (the actual player GameObject)
            transform.root.position += _animator.deltaPosition;
        }
        // All other states: root motion blocked
    }
}
