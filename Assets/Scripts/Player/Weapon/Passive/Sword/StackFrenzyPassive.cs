using System.Collections;
using UnityEngine;

public class StackFrenzyPassive : MonoBehaviour
{
    [Header("UI Icons")]
    public Sprite iconStack;
    public Sprite iconApex;

    [Header("Settings")]
    public int maxStacks = 10;
    public float bonusPerStack = 0.02f;
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

    private Coroutine apexCoroutine;
    private float apexCooldownTimer = 0f;
    private float currentStackExpireTimer = 0f;

    private const float stackExpireTime = 15f;
    private const float apexDuration = 5f;
    private const float apexCooldown = 30f;

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
        context.OnTakeDamage += OnTakeDamage;
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            context.OnAttack -= OnAttack;
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

    private void OnAttack()
    {
        if (!enabled) return;
        if (context.LastHitEnemies.Count == 0) return;
        int comboIndex = context.LastComboIndex;
        int stacksToAdd = (thirdHitTripleStack && comboIndex == 2) ? 3 : 1;
        AddStacks(stacksToAdd);
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
            OnMaxStacksReached();
    }

    public void SetStacks(int value)
    {
        if (stats == null) return;

        stats.RemoveMultiplierModifier(stackModifier);
        CurrentStacks = value;
        stackModifier.damage = CurrentStacks * bonusPerStack;

        if (CurrentStacks > 0)
        {
            stats.AddMultiplierModifier(stackModifier);
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

        frenzyEntry.isInnerBorderVisible = apexPredator;

        if (IsApexActive)
        {
            frenzyEntry.innerBorderFill = 1f;
            frenzyEntry.icon = iconApex;
        }
        else if (apexCooldownTimer > 0)
        {
            frenzyEntry.innerBorderFill = 1f - (apexCooldownTimer / apexCooldown);
            frenzyEntry.icon = iconStack;
        }
        else
        {
            frenzyEntry.innerBorderFill = apexPredator ? 1f : 0f;
            frenzyEntry.icon = iconStack;
        }

        frenzyEntry.stackCount = CurrentStacks;

        frenzyEntry.stackExpireFill = (CurrentStacks > 0) ? (currentStackExpireTimer / stackExpireTime) : 0;

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
            frenzyRushModifier.attackSpeed = 0.15f;
            stats.AddMultiplierModifier(frenzyRushModifier);
        }

        if (glassCannon)
        {
            RemoveGlassCannon();
            glassCannonModifier.critDamage = 0.5f;
            glassCannonModifier.damageTaken = 0.1f;
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
        apexCritModifier.critChance = 1f;
        apexAttackSpeedModifier.attackSpeed = 0.35f;

        stats.AddFlatModifier(apexCritModifier);
        stats.AddMultiplierModifier(apexAttackSpeedModifier);

        yield return new WaitForSeconds(apexDuration);

        stats.RemoveFlatModifier(apexCritModifier);
        stats.RemoveMultiplierModifier(apexAttackSpeedModifier);

        apexCritModifier.critChance = 0f;
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
    }
}