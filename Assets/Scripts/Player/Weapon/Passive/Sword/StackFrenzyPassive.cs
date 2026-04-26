using System.Collections;
using UnityEngine;

public class StackFrenzyPassive : MonoBehaviour
{
    [Header("UI Icons")]
    public Sprite iconStack;
    public Sprite iconApex;

    [Header("Settings")]
    public int maxStacks = 10;
    public float bonusPerStack = 0.2f;
    public bool thirdHitTripleStack = false;
    public bool frenzyRush = false;
    public bool glassCannon = false;
    public bool resilientFury = false;
    public bool apexPredator = false;

    public int CurrentStacks { get; private set; } = 0;
    public bool IsApexActive { get; private set; } = false;

    private PlayerStats stats;
    private PlayerCombatContext context;
    private StatusEntry frenzyEntry;

    private StatModifier stackModifier = new StatModifier();
    private StatModifier frenzyRushModifier = new StatModifier();
    private StatModifier glassCannonModifier = new StatModifier();
    private StatModifier apexCritModifier = new StatModifier();
    private StatModifier apexAttackSpeedModifier = new StatModifier();
    private StatModifier apexCritDmgModifier = new StatModifier();

    private Coroutine apexCoroutine;
    private float apexCooldownTimer = 0f;
    private float currentStackExpireTimer = 0f;

    private const float stackExpireTime = 15f;
    private const float apexDuration = 5f;
    private const float apexCooldown = 30f;
    private float apexTimer = 0f;

    public void RegisterHUD()
    {
        frenzyEntry = new StatusEntry("frenzy", iconStack);
        PlayerStatusHUD.Instance.Register(frenzyEntry);
    }

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext)
    {
        stats = playerStats;
        context = combatContext;
        context.OnAttack += OnAttack;
        context.OnSecondaryAttack += OnSecondaryAttackHit;
        context.OnTakeDamage += OnTakeDamage;
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            context.OnAttack -= OnAttack;
            context.OnSecondaryAttack -= OnSecondaryAttackHit;
            context.OnTakeDamage -= OnTakeDamage;
        }
        ClearAllModifiers();
    }

    private void Update()
    {
        if (apexCooldownTimer > 0f)
            apexCooldownTimer -= Time.deltaTime;

        if (CurrentStacks > 0)
        {
            currentStackExpireTimer -= Time.deltaTime;
            if (currentStackExpireTimer <= 0f)
            {
                SetStacks(0);
            }
        }

        UpdateHUD();
    }

    private bool AnyNonInvincibleHit()
    {
        foreach (var enemy in context.LastHitEnemies)
            if (enemy != null && !enemy.IsInvincible) return true;
        return false;
    }

    private void OnAttack()
    {
        if (!enabled) return;
        if (!AnyNonInvincibleHit()) return;
        int comboIndex = context.LastComboIndex;
        int stacksToAdd = (thirdHitTripleStack && comboIndex == 2) ? 3 : 1;
        AddStacks(stacksToAdd);
    }

    private void OnSecondaryAttackHit()
    {
        if (!enabled) return;
        if (!AnyNonInvincibleHit()) return;
        AddStacks(1);
    }

    private void OnTakeDamage()
    {
        if (!enabled) return;
        if (CurrentStacks == 0) return;
        SetStacks(resilientFury ? CurrentStacks / 2 : 0);
    }

    private void AddStacks(int amount)
    {
        int prevStacks = CurrentStacks;
        int newStacks = Mathf.Min(CurrentStacks + amount, maxStacks);
        SetStacks(newStacks);

        currentStackExpireTimer = stackExpireTime;

        if (CurrentStacks >= maxStacks && prevStacks < maxStacks)
        {
            OnMaxStacksReached();
        }
        else if (CurrentStacks >= maxStacks && apexCooldownTimer <= 0f && !IsApexActive)
        {
            OnMaxStacksReached();
        }
    }

    public void SetStacks(int value)
    {
        if (stats == null) return;

        stats.RemoveFlatModifier(stackModifier);
        CurrentStacks = value;
        stackModifier.damage = CurrentStacks * bonusPerStack;

        if (CurrentStacks > 0)
        {
            stats.AddFlatModifier(stackModifier);
            if (currentStackExpireTimer <= 0) currentStackExpireTimer = stackExpireTime;
        }
        else
        {
            currentStackExpireTimer = 0f;
        }

        if (CurrentStacks < maxStacks)
        {
            RemoveFrenzyRush();
            RemoveGlassCannon();
        }
    }

    private void UpdateHUD()
    {
        if (frenzyEntry == null) return;

        frenzyEntry.outerBorderType = (CurrentStacks >= maxStacks) ? StatusBorderType.Gold : StatusBorderType.Default;

        if (IsApexActive)
        {
            frenzyEntry.showInnerBorder = true;
            frenzyEntry.innerFill = apexTimer / apexDuration;
            frenzyEntry.innerBorderColor = Color.red;
            frenzyEntry.icon = iconApex;
            frenzyEntry.innerFillClockwise = false;
        }
        else if (apexCooldownTimer > 0)
        {
            frenzyEntry.showInnerBorder = true;
            frenzyEntry.innerFill = 1f - (apexCooldownTimer / apexCooldown);
            frenzyEntry.innerBorderColor = Color.red;
            frenzyEntry.icon = iconStack;
            frenzyEntry.innerFillClockwise = true;
        }
        else
        {
            frenzyEntry.showInnerBorder = apexPredator;
            frenzyEntry.innerBorderColor = Color.red;
            frenzyEntry.innerFill = 1f;
            frenzyEntry.icon = iconStack;
        }

        frenzyEntry.count = CurrentStacks;
        frenzyEntry.isActive = CurrentStacks > 0;

        frenzyEntry.sweepFill = (CurrentStacks > 0) ? 1f - (currentStackExpireTimer / stackExpireTime) : 0;
        frenzyEntry.sweepClockwise = true;

        PlayerStatusHUD.Instance.Refresh("frenzy");
    }

    public void ClampStacks()
    {
        if (CurrentStacks > maxStacks) SetStacks(maxStacks);
    }

    private void OnMaxStacksReached()
    {
        if (frenzyRush)
        {
            RemoveFrenzyRush();
            frenzyRushModifier.attackSpeed = 0.25f;
            stats.AddMultiplierModifier(frenzyRushModifier);
        }

        if (glassCannon)
        {
            RemoveGlassCannon();
            glassCannonModifier.critDamage = 0.35f;
            glassCannonModifier.damageTaken = 1.5f;
            stats.AddMultiplierModifier(glassCannonModifier);
        }

        if (apexPredator && !IsApexActive && apexCooldownTimer <= 0f)
        {
            if (apexCoroutine != null) StopCoroutine(apexCoroutine);
            apexCoroutine = StartCoroutine(ApexState());
        }
    }

    public void RemoveFrenzyRush()
    {
        stats.RemoveMultiplierModifier(frenzyRushModifier);
        frenzyRushModifier.attackSpeed = 0f;
    }

    public void RemoveGlassCannon()
    {
        stats.RemoveMultiplierModifier(glassCannonModifier);
        glassCannonModifier.critDamage = 0f;
        glassCannonModifier.damageTaken = 0f;
    }

    private IEnumerator ApexState()
    {
        IsApexActive = true;
        apexTimer = apexDuration;
        apexCritModifier.critChance = 1f;
        apexAttackSpeedModifier.attackSpeed = 0.5f;

        stats.AddFlatModifier(apexCritModifier);
        stats.AddMultiplierModifier(apexAttackSpeedModifier);

        while (apexTimer > 0f)
        {
            apexTimer -= Time.deltaTime;
            float totalCrit = stats.CritChance;
            float excess = Mathf.Max(0f, totalCrit - 1f);
            stats.RemoveFlatModifier(apexCritDmgModifier);
            apexCritDmgModifier.critDamage = excess * 3f;
            stats.AddFlatModifier(apexCritDmgModifier);
            yield return null;
        }

        stats.RemoveFlatModifier(apexCritModifier);
        stats.RemoveFlatModifier(apexCritDmgModifier);
        stats.RemoveMultiplierModifier(apexAttackSpeedModifier);

        apexCritModifier.critChance = 0f;
        apexCritDmgModifier.critDamage = 0f;
        apexAttackSpeedModifier.attackSpeed = 0f;

        IsApexActive = false;
        apexCooldownTimer = apexCooldown;
    }

    private void ClearAllModifiers()
    {
        SetStacks(0);
        RemoveFrenzyRush();
        RemoveGlassCannon();
        if (IsApexActive && stats != null)
        {
            stats.RemoveFlatModifier(apexCritModifier);
            stats.RemoveFlatModifier(apexCritDmgModifier);
            stats.RemoveMultiplierModifier(apexAttackSpeedModifier);
        }
    }

    public void ForceClean()
    {
        if (apexCoroutine != null) StopCoroutine(apexCoroutine);
        ClearAllModifiers();
        IsApexActive = false;
        apexCooldownTimer = 0f;
        currentStackExpireTimer = 0f;
        apexTimer = 0f;
    }
}