using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Vector2 move;
    private Vector2 lastDir = Vector2.down;
    private Rigidbody rb;
    private Animator anim;
    private PlayerStats stats;
    private WeaponEquip weaponEquip;
    private AttackHitbox attackHitbox;

    private bool isPrimaryAttacking = false;
    private bool isSecondaryAttacking = false;
    private int comboIndex = 0;
    private float lastAttackTime = 0f;
    private float comboResetTime = 1f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<PlayerStats>();
        weaponEquip = GetComponent<WeaponEquip>();
        attackHitbox = GetComponentInChildren<AttackHitbox>();

        anim.SetFloat("moveX", 0);
        anim.SetFloat("moveY", -1);
    }

    public void OnMove(InputAction.CallbackContext ctx)
    {
        move = ctx.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (isDashing) return;
        if (dashCooldownTimer > 0) return;

        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon == null) return;

        if (move != Vector2.zero)
            dashDirection = new Vector3(move.x, 0, move.y).normalized;
        else
            dashDirection = new Vector3(lastDir.x, 0, lastDir.y).normalized;

        isDashing = true;
        dashTimer = weapon.dashDuration;
        dashCooldownTimer = weapon.dashCooldown;

        anim.SetBool("isDashing", true);
    }

    public void OnPrimaryAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (isPrimaryAttacking) return;

        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon == null || weapon.combo.Length == 0) return;

        if (Time.time - lastAttackTime > comboResetTime)
            comboIndex = 0;

        ComboHit hit = weapon.combo[comboIndex];

        Vector3 exactDir = GetExactMouseDirection();
        attackHitbox.transform.rotation = Quaternion.LookRotation(exactDir);
        attackHitbox.SetComboHit(hit);

        Vector2 mouseDir = GetMouseDirection(exactDir);
        lastDir = mouseDir;
        anim.SetFloat("moveX", mouseDir.x);
        anim.SetFloat("moveY", mouseDir.y);

        anim.SetTrigger(hit.animationTrigger);

        isPrimaryAttacking = true;
        lastAttackTime = Time.time;

        comboIndex = (comboIndex + 1) % weapon.combo.Length;
    }

    public void OnSecondaryAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (isPrimaryAttacking || isSecondaryAttacking) return;

        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon == null || weapon.secondaryAttack == null) return;

        ComboHit hit = weapon.secondaryAttack;

        Vector3 exactDir = GetExactMouseDirection();
        attackHitbox.transform.rotation = Quaternion.LookRotation(exactDir);
        attackHitbox.SetComboHit(hit);

        Vector2 mouseDir = GetMouseDirection(exactDir);
        lastDir = mouseDir;
        anim.SetFloat("moveX", mouseDir.x);
        anim.SetFloat("moveY", mouseDir.y);

        anim.SetTrigger(hit.animationTrigger);

        isSecondaryAttacking = true;

        comboIndex = 0;
    }

    public void OnAttackActive()
    {
        attackHitbox.Attack();
    }

    public void OnPrimaryAttackEnd()
    {
        isPrimaryAttacking = false;
        anim.SetFloat("moveX", lastDir.x);
        anim.SetFloat("moveY", lastDir.y);
    }

    public void OnSecondaryAttackEnd()
    {
        isSecondaryAttacking = false;
        anim.SetFloat("moveX", lastDir.x);
        anim.SetFloat("moveY", lastDir.y);
    }

    void FixedUpdate()
    {
        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.fixedDeltaTime;

        if (isDashing)
        {
            WeaponData weapon = weaponEquip.GetCurrentWeapon();
            if (weapon == null) { isDashing = false; return; }

            rb.linearVelocity = new Vector3(
                dashDirection.x * weapon.dashSpeed,
                rb.linearVelocity.y,
                dashDirection.z * weapon.dashSpeed
            );

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                anim.SetBool("isDashing", false);
            }

            return;
        }

        if (isPrimaryAttacking || isSecondaryAttacking)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 movement = new Vector3(move.x, 0, move.y).normalized;
        rb.linearVelocity = new Vector3(
            movement.x * stats.MoveSpeed,
            rb.linearVelocity.y,
            movement.z * stats.MoveSpeed
        );

        if (move != Vector2.zero)
        {
            if (Mathf.Abs(move.x) >= Mathf.Abs(move.y))
                lastDir = new Vector2(Mathf.Sign(move.x), 0);
            else
                lastDir = new Vector2(0, Mathf.Sign(move.y));

            anim.SetFloat("moveX", lastDir.x);
            anim.SetFloat("moveY", lastDir.y);
            anim.SetBool("isMoving", true);
        }
        else
        {
            anim.SetBool("isMoving", false);
        }
    }

    Vector3 GetExactMouseDirection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            Vector3 dir = (worldPos - transform.position);
            dir.y = 0;
            return dir.normalized;
        }
        return transform.forward;
    }

    Vector2 GetMouseDirection(Vector3 exactDir)
    {
        Vector2 dir = new Vector2(exactDir.x, exactDir.z).normalized;

        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            return new Vector2(Mathf.Sign(dir.x), 0);
        else
            return new Vector2(0, Mathf.Sign(dir.y));
    }
}