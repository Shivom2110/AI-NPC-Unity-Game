using UnityEngine;

public class SwordManager : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Weapon references
    // ─────────────────────────────────────────────
    [Header("Weapons")]
    public Transform sword;
    public Transform dagger;

    // ─────────────────────────────────────────────
    //  Skeleton bones
    // ─────────────────────────────────────────────
    [Header("Bones")]
    public Transform rightHandBone;
    public Transform leftHandBone;
    public Transform backBone;

    // ─────────────────────────────────────────────
    //  Sword – Drawn (Right Hand)
    // ─────────────────────────────────────────────
    [Header("Sword – Drawn (Right Hand)")]
    public Vector3 swordDrawnPosition = new Vector3(0f, 0f, 0f);
    public Vector3 swordDrawnRotation = new Vector3(73.084f, 94.803f, -84.981f);
    public Vector3 swordDrawnScale    = new Vector3(1f, 1f, 1f);

    // ─────────────────────────────────────────────
    //  Dagger – Drawn (Left Hand)
    // ─────────────────────────────────────────────
    [Header("Dagger – Drawn (Left Hand)")]
    public Vector3 daggerDrawnPosition = new Vector3(-0.11f, 0.022f, 0.204f);
    public Vector3 daggerDrawnRotation = new Vector3(-250.454f, 90f, -90f);
    public Vector3 daggerDrawnScale    = new Vector3(1000f, 1000f, 1000f);

    // ─────────────────────────────────────────────
    //  Sword – Sheathed (Back)
    // ─────────────────────────────────────────────
    [Header("Sword – Sheathed (Back)")]
    public Vector3 swordSheathPosition = new Vector3(0f, 0.1f, -0.05f);
    public Vector3 swordSheathRotation = new Vector3(0f, 0f, 90f);
    public Vector3 swordSheathScale    = new Vector3(1f, 1f, 1f);

    // ─────────────────────────────────────────────
    //  Dagger – Sheathed (Hip / Back)
    // ─────────────────────────────────────────────
    [Header("Dagger – Sheathed (Hip)")]
    public Vector3 daggerSheathPosition = new Vector3(-0.1f, 0f, -0.05f);
    public Vector3 daggerSheathRotation = new Vector3(-250.454f, 90f, -90f);
    public Vector3 daggerSheathScale    = new Vector3(1000f, 1000f, 1000f);

    // ─────────────────────────────────────────────
    //  Input
    // ─────────────────────────────────────────────
    [Header("Input")]
    public KeyCode drawSheathKey   = KeyCode.F;
    public KeyCode switchWeaponKey = KeyCode.Tab;

    // ─────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────
    public enum ActiveWeapon { Sword, Dagger }

    public bool         IsDrawn       => _drawn;
    public ActiveWeapon CurrentWeapon => _activeWeapon;

    private bool         _drawn        = false;
    private ActiveWeapon _activeWeapon = ActiveWeapon.Sword;

    // ─────────────────────────────────────────────
    //  Unity lifecycle
    // ─────────────────────────────────────────────
    void Start()
    {
        Sheath();
    }

    void Update()
    {
        if (Input.GetKeyDown(drawSheathKey))
            Toggle();

        if (Input.GetKeyDown(switchWeaponKey) && _drawn)
            SwitchWeapon();
    }

    // ─────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────
    public void Draw()
    {
        _drawn = true;
        Place(sword,  rightHandBone, swordDrawnPosition,  swordDrawnRotation,  swordDrawnScale);
        Place(dagger, leftHandBone,  daggerDrawnPosition, daggerDrawnRotation, daggerDrawnScale);
    }

    public void Sheath()
    {
        _drawn = false;
        Place(sword,  backBone, swordSheathPosition,  swordSheathRotation,  swordSheathScale);
        Place(dagger, backBone, daggerSheathPosition, daggerSheathRotation, daggerSheathScale);
    }

    public void Toggle()
    {
        if (_drawn) Sheath();
        else        Draw();
    }

    public void SwitchWeapon()
    {
        _activeWeapon = (_activeWeapon == ActiveWeapon.Sword)
            ? ActiveWeapon.Dagger
            : ActiveWeapon.Sword;
    }

    public void SetActiveWeapon(ActiveWeapon weapon) => _activeWeapon = weapon;

    // ─────────────────────────────────────────────
    //  Helper
    // ─────────────────────────────────────────────
    void Place(Transform weapon, Transform parent, Vector3 pos, Vector3 rot, Vector3 scale)
    {
        if (weapon == null || parent == null) return;
        weapon.SetParent(parent);
        weapon.localPosition = pos;
        weapon.localRotation = Quaternion.Euler(rot);
        weapon.localScale    = scale;
    }

    // ─────────────────────────────────────────────
    //  Debug UI
    // ─────────────────────────────────────────────
    void OnGUI()
    {
        string label = _drawn ? $"SHEATH  [{_activeWeapon}]" : "DRAW";

        if (GUI.Button(new Rect(10, 10, 200, 50), label))
            Toggle();

        if (_drawn && GUI.Button(new Rect(10, 65, 200, 40),
            $"SWITCH → {(_activeWeapon == ActiveWeapon.Sword ? "Dagger" : "Sword")}"))
            SwitchWeapon();
    }
}
