using UnityEngine;
using UnityEngine.InputSystem;


public interface IInteractable
{
    void Interact(PlayerController playerController);
    string GetPromptText();
    InteractInfo GetInteractInfo();
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
    private CapsuleCollider capsuleCollider;

    private bool isPrimaryAttacking = false;
    private bool isSecondaryAttacking = false;
    private int comboIndex = 0;
    private float lastAttackTime = 0f;
    private float primaryCooldownTimer = 0f;
    private float secondaryCooldownTimer = 0f;

    public float SecondaryCooldownRemaining => secondaryCooldownTimer;

    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector3 dashDirection;
    private DashTrail dashTrail;
    private bool isBarrierIgnored = false;

    [Header("Interaction")]
    public float interactRange = 2f;
    public LayerMask interactableLayer;
    private InteractPromptUI interactPrompt;
    private UIManager uiManager;

    [Header("Dash Barrier")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask barrierLayer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponentInChildren<Animator>();
        stats = GetComponent<PlayerStats>();
        weaponEquip = GetComponent<WeaponEquip>();
        attackHitbox = GetComponentInChildren<AttackHitbox>();
        wandAttack = GetComponent<WandAttack>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        interactPrompt = FindFirstObjectByType<InteractPromptUI>(FindObjectsInactive.Include);
        uiManager = FindFirstObjectByType<UIManager>();
        dashTrail = GetComponentInChildren<DashTrail>();

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

        bool shouldIgnoreBarrier = CanDashThroughBarrier(weapon);
        if (shouldIgnoreBarrier)
            SetBarrierIgnore(true);

        isDashing = true;
        dashTrail?.StartTrail();
        dashTimer = weapon.dashDuration;
        dashCooldownTimer = weapon.dashCooldown;

        stats.SetInvincible(true);
        anim.SetBool("isDashing", true);
    }

    private bool CanDashThroughBarrier(WeaponData weapon)
    {
        float dashDistance = weapon.dashSpeed * weapon.dashDuration;
        Vector3 landingPos = transform.position + dashDirection * dashDistance;

        float radius = capsuleCollider != null ? capsuleCollider.radius : 0.3f;
        Vector3 right = Vector3.Cross(dashDirection, Vector3.up).normalized;

        Vector3 centerPos = landingPos;
        Vector3 leftPos = landingPos - right * radius;
        Vector3 rightPos = landingPos + right * radius;

        return HasGround(centerPos) && HasGround(leftPos) && HasGround(rightPos);
    }

    private bool HasGround(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * 1f;
        return Physics.Raycast(origin, Vector3.down, 2f, groundLayer);
    }

    private void SetBarrierIgnore(bool ignore)
    {
        int playerLayer = gameObject.layer;
        int barrier = (int)Mathf.Log(barrierLayer.value, 2);
        Physics.IgnoreLayerCollision(playerLayer, barrier, ignore);
        isBarrierIgnored = ignore;
    }

    public void OnPrimaryAttack(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        TryPrimaryAttack();
    }

    private void TryPrimaryAttack()
    {
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

        if (weapon.weaponType == WeaponType.Wand && wandAttack != null && !wandAttack.IsTotemReady) return;

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
    }


    public void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!ctx.started) return;
        if (UIManager.Instance != null && UIManager.Instance.isInBattle) return;

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
            AudioManager.Instance?.PlaySwordAttack();
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
        if (weapon != null && weapon.weaponType != WeaponType.Wand)
            secondaryCooldownTimer = weapon.secondaryCooldown / stats.AttackSpeed;
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            TryPrimaryAttack();
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
            if (weapon == null)
            {
                isDashing = false;
                stats.SetInvincible(false);
                if (isBarrierIgnored) SetBarrierIgnore(false);
                return;
            }
            rb.linearVelocity = new Vector3(
                dashDirection.x * weapon.dashSpeed,
                rb.linearVelocity.y,
                dashDirection.z * weapon.dashSpeed
            );

            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
                dashTrail?.StopTrail();
                stats.SetInvincible(false);
                anim.SetBool("isDashing", false);
                if (isBarrierIgnored) SetBarrierIgnore(false);
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
        if (UIManager.Instance != null && UIManager.Instance.isInBattle)
        {
            interactPrompt?.Hide();
            return;
        }

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
            interactPrompt?.Show(closest.GetInteractInfo(), (closest as MonoBehaviour)?.transform);
        else
            interactPrompt?.Hide();
    }

    Vector3 GetExactMouseDirection()
    {
        float groundY = transform.position.y;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit groundHit, 5f, 1 << LayerMask.NameToLayer("Ground")))
            groundY = groundHit.point.y;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, groundY, 0));
        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 worldPos = ray.GetPoint(distance);
            Vector3 dir = worldPos - transform.position;
            dir.y = 0;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
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
            || uiManager.IsStorageOpen
            || uiManager.IsHealthStationOpen
            || uiManager.IsLuckStationOpen
            || (uiManager.GetPassiveScreen()?.IsOpen ?? false)
            || (uiManager.GetGamblerScreen()?.IsOpen ?? false);
    }

    public void OnHitVFX()
    {
        attackHitbox.PlayHitVFX();
    }
}