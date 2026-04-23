using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Press F to show both weapons (drawn) or hide them (sheathed).
/// Weapons stay attached to their hand bones at all times.
/// </summary>
public class SwordManager : MonoBehaviour
{
    [Header("Weapons")]
    [FormerlySerializedAs("fireSword")]
    public Transform sword;

    [FormerlySerializedAs("warScythe")]
    public Transform dagger;

    [Header("Bones")]
    public Transform rightHandBone;
    public Transform leftHandBone;

    [Header("Sword Position (Right Hand)")]
    [FormerlySerializedAs("fireSwordDrawnPosition")]
    public Vector3 swordPosition = new Vector3(-0.03f, 0.01f, 0.4f);

    [FormerlySerializedAs("fireSwordDrawnRotation")]
    public Vector3 swordRotation = new Vector3(0f, 0f, 0f);

    public Vector3 swordScale = new Vector3(1f, 1f, 1f);

    [Header("Dagger Position (Left Hand)")]
    [FormerlySerializedAs("warScytheDrawnPosition")]
    public Vector3 daggerPosition = new Vector3(-0.06f, 0.03f, 0.15f);

    [FormerlySerializedAs("warScytheDrawnRotation")]
    public Vector3 daggerRotation = new Vector3(0f, 0f, 180f);

    public Vector3 daggerScale = new Vector3(1000f, 1000f, 1000f);

    [Header("Input")]
    public KeyCode drawSheathKey = KeyCode.F;

    public bool IsDrawn => _drawn;

    public enum ActiveWeapon
    {
        Sword,
        Dagger
    }

    public ActiveWeapon CurrentWeapon => _activeWeapon;

    private ActiveWeapon _activeWeapon = ActiveWeapon.Sword;
    private bool _drawn;
    private Animator _animator;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        RefreshAttachments();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        RefreshAttachments();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
            Toggle();
    }

    public void Toggle()
    {
        _drawn = !_drawn;
        SetVisible(_drawn);
        SetCombatIdle(_drawn);
    }

    public void Draw()
    {
        _drawn = true;
        SetVisible(true);
        SetCombatIdle(true);
    }

    public void Sheathe()
    {
        _drawn = false;
        SetVisible(false);
        SetCombatIdle(false);
    }

    public void SetActiveWeapon(ActiveWeapon weapon)
    {
        _activeWeapon = weapon;
    }

    private void SetVisible(bool visible)
    {
        if (sword != null)
            sword.gameObject.SetActive(visible);

        if (dagger != null)
            dagger.gameObject.SetActive(visible);
    }

    private void SetCombatIdle(bool active)
    {
        if (_animator != null)
            _animator.SetBool("IsDrawn", active);
    }

    private void RefreshAttachments()
    {
        Attach(sword, rightHandBone, swordPosition, swordRotation, swordScale);
        Attach(dagger, leftHandBone, daggerPosition, daggerRotation, daggerScale);
    }

    private void Attach(Transform weapon, Transform bone, Vector3 pos, Vector3 rot, Vector3 desiredWorldScale)
    {
        if (weapon == null || bone == null)
            return;

        if (weapon.parent != bone)
            weapon.SetParent(bone, worldPositionStays: false);

        weapon.localPosition = pos;
        weapon.localRotation = Quaternion.Euler(rot);

        Vector3 parentScale = bone.lossyScale;
        weapon.localScale = new Vector3(
            desiredWorldScale.x / Mathf.Max(parentScale.x, 0.0001f),
            desiredWorldScale.y / Mathf.Max(parentScale.y, 0.0001f),
            desiredWorldScale.z / Mathf.Max(parentScale.z, 0.0001f));
    }

    private void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.button)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };

        string label = _drawn ? "[ F ]  SHEATHE" : "[ F ]  DRAW";
        if (GUI.Button(new Rect(10, 10, 180, 45), label, style))
            Toggle();
    }
}
