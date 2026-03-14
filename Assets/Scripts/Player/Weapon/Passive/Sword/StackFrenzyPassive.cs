using System.Collections;
using UnityEngine;

public class StackFrenzyPassive : MonoBehaviour
{
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

    private StatModifier stackModifier = new StatModifier();
    private StatModifier frenzyRushModifier = new StatModifier();
    private StatModifier glassCannonModifier = new StatModifier();
    private StatModifier apexCritModifier = new StatModifier();
    private StatModifier apexAttackSpeedModifier = new StatModifier();

    private Coroutine expireCoroutine;
    private Coroutine apexCoroutine;
    private float apexCooldownTimer = 0f;

    private const float stackExpireTime = 15f;
    private const float apexDuration = 5f;
    private const float apexCooldown = 30f;

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

        if (expireCoroutine != null) StopCoroutine(expireCoroutine);
        expireCoroutine = StartCoroutine(ExpireStacks());

        if (CurrentStacks >= maxStacks && prevStacks < maxStacks)
            OnMaxStacksReached();
    }

    public void SetStacks(int value)
    {
        stats.RemoveMultiplierModifier(stackModifier);

        CurrentStacks = value;

        stackModifier.damage = CurrentStacks * bonusPerStack;

        if (CurrentStacks > 0)
            stats.AddMultiplierModifier(stackModifier);

        if (CurrentStacks < maxStacks)
        {
            RemoveFrenzyRush();
            RemoveGlassCannon();
        }
    }

    public void ClampStacks()
    {
        if (CurrentStacks > maxStacks)
            SetStacks(maxStacks);
    }

    private void OnMaxStacksReached()
    {
        if (frenzyRush)
        {
            RemoveFrenzyRush();
            frenzyRushModifier = new StatModifier { attackSpeed = 0.15f };
            stats.AddMultiplierModifier(frenzyRushModifier);
        }

        if (glassCannon)
        {
            RemoveGlassCannon();
            glassCannonModifier = new StatModifier { critDamage = 0.5f, damageTaken = 0.1f };
            stats.AddMultiplierModifier(glassCannonModifier);
        }

        if (apexPredator && !IsApexActive && apexCooldownTimer <= 0f)
        {
            if (apexCoroutine != null) StopCoroutine(apexCoroutine);
            apexCoroutine = StartCoroutine(ApexState());
        }
    }

    private void RemoveFrenzyRush()
    {
        stats.RemoveMultiplierModifier(frenzyRushModifier);
        frenzyRushModifier.attackSpeed = 0f;
    }

    private void RemoveGlassCannon()
    {
        stats.RemoveMultiplierModifier(glassCannonModifier);
        glassCannonModifier.critDamage = 0f;
        glassCannonModifier.damageTaken = 0f;
    }

    private IEnumerator ExpireStacks()
    {
        yield return new WaitForSeconds(stackExpireTime);
        SetStacks(0);
    }

    private IEnumerator ApexState()
    {
        IsApexActive = true;
        apexCritModifier = new StatModifier { critChance = 1f };
        apexAttackSpeedModifier = new StatModifier { attackSpeed = 0.35f };
        stats.AddFlatModifier(apexCritModifier);
        stats.AddMultiplierModifier(apexAttackSpeedModifier);

        yield return new WaitForSeconds(apexDuration);

        stats.RemoveFlatModifier(apexCritModifier);
        stats.RemoveMultiplierModifier(apexAttackSpeedModifier);
        apexCritModifier = new StatModifier();
        apexAttackSpeedModifier = new StatModifier();
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
        if (expireCoroutine != null) StopCoroutine(expireCoroutine);
        if (apexCoroutine != null) StopCoroutine(apexCoroutine);
        ClearAllModifiers();
        IsApexActive = false;
        apexCooldownTimer = 0f;
    }
}