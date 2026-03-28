using UnityEngine;
using UnityEngine.InputSystem;


public interface IInteractable
{
    void Interact(PlayerController playerController);
    string GetPromptText();
}
public class PlayerController : MonoBehaviour
{
    private Vector2 move;
    private Vector2 lastDir = Vector2.down;
    private Rigidbody rb;
    private Animator anim;
    private PlayerStats stats;
    private WeaponEquip weaponEquip;
    private AttackHitbox attackHitbox;
    private WandAttack wandAttack;

    private bool isPrimaryAttacking = false;
    private bool isSecondaryAttacking = false;
    private int comboIndex = 0;
    private float lastAttackTime = 0f;
    private float primaryCooldownTimer = 0f;
    private float secondaryCooldownTimer = 0f;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;

    [Header("Interaction")]
    public float interactRange = 2f;
    public LayerMask interactableLayer;
    private InteractPromptUI interactPrompt;
    private UIManager uiManager;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<PlayerStats>();
        weaponEquip = GetComponent<WeaponEquip>();
        attackHitbox = GetComponentInChildren<AttackHitbox>();
        wandAttack = GetComponent<WandAttack>();
        interactPrompt = FindFirstObjectByType<InteractPromptUI>(FindObjectsInactive.Include);
        uiManager = FindFirstObjectByType<UIManager>();

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
        if (IsAnyUIOpen()) return;
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

        stats.SetInvincible(true);
        anim.SetBool("isDashing", true);
    }

    public void OnPrimaryAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (IsAnyUIOpen()) return;
        if (isPrimaryAttacking || isSecondaryAttacking) return;
        if (primaryCooldownTimer > 0) return;

        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon == null || weapon.combo.Length == 0) return;

        if (Time.time - lastAttackTime > weapon.comboResetTime)
            comboIndex = 0;

        ComboHit hit = weapon.combo[comboIndex];

        Vector3 exactDir = GetExactMouseDirection();
        Vector2 mouseDir = GetMouseDirection(exactDir);
        lastDir = mouseDir;
        anim.SetFloat("moveX", mouseDir.x);
        anim.SetFloat("moveY", mouseDir.y);

        if (hit.animationDuration > 0)
        {
            float clipLength = GetClipLength(hit.animationTrigger);
            anim.speed = clipLength / hit.animationDuration * stats.AttackSpeed;
        }

        anim.SetTrigger(hit.animationTrigger);

        attackHitbox.transform.rotation = Quaternion.LookRotation(exactDir);
        attackHitbox.SetComboHit(hit, comboIndex);

        isPrimaryAttacking = true;
        lastAttackTime = Time.time;

        comboIndex = (comboIndex + 1) % weapon.combo.Length;
    }

    public void OnSecondaryAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (IsAnyUIOpen()) return;
        if (isPrimaryAttacking || isSecondaryAttacking) return;
        if (secondaryCooldownTimer > 0) return;

        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon == null || weapon.secondaryAttack == null) return;

        ComboHit hit = weapon.secondaryAttack;

        Vector3 exactDir = GetExactMouseDirection();
        attackHitbox.transform.rotation = Quaternion.LookRotation(exactDir);
        attackHitbox.SetComboHit(hit, 0, true);

        Vector2 mouseDir = GetMouseDirection(exactDir);
        lastDir = mouseDir;
        anim.SetFloat("moveX", mouseDir.x);
        anim.SetFloat("moveY", mouseDir.y);

        if (hit.animationDuration > 0)
        {
            float clipLength = GetClipLength(hit.animationTrigger);
            anim.speed = clipLength / hit.animationDuration * stats.AttackSpeed;
        }

        anim.SetTrigger(hit.animationTrigger);

        isSecondaryAttacking = true;

        comboIndex = 0;
    }


    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);

        IInteractable closest = null;
        float closestDist = Mathf.Infinity;

        foreach (Collider hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        closest?.Interact(this);
        Debug.Log("casted");
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    public void OnAttackActive()
    {
        WeaponData weapon = weaponEquip?.GetCurrentWeapon();
        if (weapon == null) return;

        if (weapon.weaponType == WeaponType.Wand)
        {
            Vector3 dir = attackHitbox.transform.forward;
            wandAttack?.FireProjectile(attackHitbox.GetCurrentHit(), attackHitbox.GetCurrentComboIndex(), dir);
        }
        else
        {
            attackHitbox.Attack();
        }
    }

    public void OnPrimaryAttackEnd()
    {
        isPrimaryAttacking = false;
        anim.speed = 1f;
        anim.SetFloat("moveX", lastDir.x);
        anim.SetFloat("moveY", lastDir.y);

        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon != null && comboIndex == 0)
            primaryCooldownTimer = weapon.comboCooldown / stats.AttackSpeed;
    }

    public void OnSecondaryAttackEnd()
    {
        isSecondaryAttacking = false;
        anim.speed = 1f;
        anim.SetFloat("moveX", lastDir.x);
        anim.SetFloat("moveY", lastDir.y);

        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon != null)
            secondaryCooldownTimer = weapon.secondaryCooldown / stats.AttackSpeed;
    }

    void FixedUpdate()
    {
        if (primaryCooldownTimer > 0)
            primaryCooldownTimer -= Time.fixedDeltaTime;

        if (secondaryCooldownTimer > 0)
            secondaryCooldownTimer -= Time.fixedDeltaTime;

        if (dashCooldownTimer > 0)
            dashCooldownTimer -= Time.fixedDeltaTime;

        if (IsAnyUIOpen())
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            anim.SetBool("isMoving", false);
            interactPrompt?.Hide();
            return;
        }

        if (isDashing)
        {
            WeaponData weapon = weaponEquip.GetCurrentWeapon();
            if (weapon == null) { isDashing = false; stats.SetInvincible(false); return; }
            rb.linearVelocity = new Vector3(
                dashDirection.x * weapon.dashSpeed,
                rb.linearVelocity.y,
                dashDirection.z * weapon.dashSpeed
            );

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                stats.SetInvincible(false);
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

        CheckInteractable();
    }

    private void CheckInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableLayer);

        IInteractable closest = null;
        float closestDist = Mathf.Infinity;
        foreach (Collider hit in hits)
        {
            var interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;
            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }

        if (closest != null)
            interactPrompt?.Show(closest.GetPromptText());
        else
            interactPrompt?.Hide();
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

    private float GetClipLength(string clipName)
    {
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName)
                return clip.length;
        return 1f;
    }

    private bool IsAnyUIOpen()
    {
        if (uiManager == null) return false;
        return uiManager.IsInventoryOpen
            || uiManager.IsShopOpen
            || (uiManager.GetPassiveScreen()?.IsOpen ?? false)
            || (uiManager.GetGamblerScreen()?.IsOpen ?? false);
    }

    public void OnHitVFX()
    {
        attackHitbox.PlayHitVFX();
    }
}