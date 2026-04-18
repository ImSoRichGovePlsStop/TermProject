using System.Collections.Generic;


public static class FloorModifierCardRegistry
{
    public static IReadOnlyList<FloorModifierCard> GetAllCards()
    {
        var cards = new List<FloorModifierCard>();

        
        cards.Add(Make("elite_budget_s", "Hardened Foes",
            "Each battle room grants +1 elite monster",
            ModifierScope.WholeRun,
            m => m.eliteBudgetBonus = 1));

        cards.Add(Make("elite_budget_l", "Elite Incursion",
            "Each battle room grants +3 elite monster",
            ModifierScope.WholeRun,
            m => m.eliteBudgetBonus = 3));

        cards.Add(Make("enemy_count_run", "Overcrowded",
            "Battle rooms spawn 25% more enemies for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.enemyCountMultiplier = 1.25f));

        cards.Add(Make("enemy_count_next", "The Horde Approaches",
            "Battle rooms on the next floor spawn 50% more enemies.",
            ModifierScope.NextFloorOnly,
            m => m.enemyCountMultiplier = 1.5f));

        cards.Add(Make("extra_wave_run", "Unending Waves",
            "Every battle room gains one additional wave for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.extraWaves = 1));

        cards.Add(Make("extra_wave_next", "Surge",
            "Every battle room on the next floor gains one additional wave.",
            ModifierScope.NextFloorOnly,
            m => m.extraWaves = 1));


        cards.Add(Make("coin_mult_s", "Cultist Sacrifices",
            "Enemies drop 25% more coins for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.coinMultiplier = 1.25f));

        cards.Add(Make("coin_mult_l", "Hand Of Midas",
            "Enemies drop 60% more coins for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.coinMultiplier = 1.6f));

        cards.Add(Make("entry_coins_s", "Golden Egg",
            "Gain 30 bonus coins at the start of each new floor.",
            ModifierScope.WholeRun,
            m => m.bonusCoinsOnFloorEntry = 30));

        cards.Add(Make("entry_coins_l", "Trophy Collector",
            "Gain 80 bonus coins at the start of each new floor.",
            ModifierScope.WholeRun,
            m => m.bonusCoinsOnFloorEntry = 80));

        cards.Add(Make("loot_mean_run", "Better Pickings",
            "Loot rewards are worth more.",
            ModifierScope.WholeRun,
            m => m.lootMeanBonus = 40f));

        cards.Add(Make("loot_mean_next", "Lucky Find",
            "Loot rewards on the next floor are worth a lot more.",
            ModifierScope.NextFloorOnly,
            m => m.lootMeanBonus = 80f));

        //cards.Add(Make("extra_loot_run", "More Choices",
        //    "Loot drops offer one extra card to choose from for the rest of the run.",
        //    ModifierScope.WholeRun,
        //    m => m.extraLootOptions = 1));

        //cards.Add(Make("extra_loot_next", "Windfall",
        //    "Loot drops on the next floor offer two extra cards to choose from.",
        //    ModifierScope.NextFloorOnly,
        //    m => m.extraLootOptions = 2));

        //commented until fix placement of reward more than 3

        cards.Add(Make("more_events_next", "Event Surge",
            "The next floor has chances to spawn more event room.",
            ModifierScope.NextFloorOnly,
            m => m.extraEventRoomMin = 1));

        cards.Add(Make("more_events_run", "Eventful Journey",
            "Every floors from now on have chance to spawn more event room.",
            ModifierScope.WholeRun,
            m => m.extraEventRoomMin = 1));

        cards.Add(Make("more_battles_next", "Battlefield",
            "The next floor have chance for more battle rooms.",
            ModifierScope.NextFloorOnly,
            m => m.extraBattleRoomMin = 2));

       

        cards.Add(Make("heal_room_s", "Resilience",
            "Recover more HP after clearing each battle room.",
            ModifierScope.WholeRun,
            m => m.healPerRoomBonus = 0.05f));

        cards.Add(Make("heal_room_l", "Vitality Surge",
            "Recover a lot more HP after clearing each battle room.",
            ModifierScope.WholeRun,
            m => m.healPerRoomBonus = 0.15f));

        cards.Add(Make("heal_room_next", "Second Wind",
            "Recover a lot more HP after each battle room on the next floor.",
            ModifierScope.NextFloorOnly,
            m => m.healPerRoomBonus = 0.20f));


        cards.Add(Make("hollow_ground", "Hollow Ground",
            "Fewer enemies per room.",
            ModifierScope.NextFloorOnly,
            m => m.enemyCountMultiplier = 0.7f));

        cards.Add(Make("elite_hunt", "Elite Hunt",
            "Fewer enemies overall, but more of them are stronger.",
            ModifierScope.NextFloorOnly,
            m => { m.enemyCountMultiplier = 0.65f; m.eliteBudgetBonus = 4; }));

        cards.Add(Make("quiet_floor", "Quiet Floor",
            "More event rooms, fewer battle rooms.",
            ModifierScope.NextFloorOnly,
            m => { m.extraEventRoomMin = 2; m.extraBattleRoomMin = -1; }));

        cards.Add(Make("warpath", "Warpath",
            "More battle rooms, fewer event rooms.",
            ModifierScope.NextFloorOnly,
            m => { m.extraBattleRoomMin = 2; m.extraEventRoomMin = -1; }));

        cards.Add(Make("enforcers_call", "Enforcer's Call",
            "More stronger enemies per room. Better loot quality.",
            ModifierScope.NextFloorOnly,
            m => { m.eliteBudgetBonus = 5; m.lootMeanBonus = 60f; }));

        cards.Add(Make("gauntlet", "Gauntlet",
            "Extra waves per room. Enemies drop more coins.",
            ModifierScope.NextFloorOnly,
            m => { m.extraWaves = 2; m.coinMultiplier = 1.5f; }));

        // ── Sell value ───────────────────────────────────────────────────────

        cards.Add(Make("sell_boost_s", "Shrewd Trader",
            "Sell items for 25% more coins for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.sellPriceMultiplier = 1.25f));

        cards.Add(Make("sell_boost_l", "Black Market Contact",
            "Sell items for 60% more coins for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.sellPriceMultiplier = 1.6f));

        cards.Add(Make("sell_boost_next", "Fire Sale",
            "Sell items for double their value on the next floor.",
            ModifierScope.NextFloorOnly,
            m => m.sellPriceMultiplier = 2.0f));

        // ── Shop discount ────────────────────────────────────────────────────

        cards.Add(Make("shop_discount_s", "Haggler",
            "Shop items cost 15% less for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.shopDiscount = 0.15f));

        cards.Add(Make("shop_discount_l", "Merchant's Favor",
            "Shop items cost 30% less for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.shopDiscount = 0.30f));

        cards.Add(Make("shop_discount_next", "Clearance",
            "Shop items cost 40% less on the next floor.",
            ModifierScope.NextFloorOnly,
            m => m.shopDiscount = 0.40f));

        // ── Upgrade discount ─────────────────────────────────────────────────

        cards.Add(Make("upgrade_discount_l", "Master Forge",
            "Upgrading modules costs 40% less for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.upgradeDiscount = 0.40f));

        // ── Heal discount ────────────────────────────────────────────────────

        cards.Add(Make("heal_discount_l", "House Surgeon",
            "Paid healing costs 40% less for the rest of the run.",
            ModifierScope.WholeRun,
            m => m.healDiscount = 0.40f));

        // ── Economy combos ───────────────────────────────────────────────────

        cards.Add(Make("merchant_run", "Merchant's Eye",
            "Shop 20% cheaper. Sell items for 20% more.",
            ModifierScope.WholeRun,
            m => { m.shopDiscount = 0.20f; m.sellPriceMultiplier = 1.20f; }));

        cards.Add(Make("scavenger_run", "Scavenger",
            "Sell items for 40% more. Upgrades cost 20% less.",
            ModifierScope.WholeRun,
            m => { m.sellPriceMultiplier = 1.40f; m.upgradeDiscount = 0.20f; }));

        cards.Add(Make("full_service", "Full Service",
            "Everything discounted: shop 20% off, upgrades 20% off, heals 20% off.",
            ModifierScope.WholeRun,
            m => { m.shopDiscount = 0.20f; m.upgradeDiscount = 0.20f; m.healDiscount = 0.20f; }));

        // ── Merge ────────────────────────────────────────────────────────────

        cards.Add(Make("merge_value_l", "Philosopher's Catalyst",
            "Merge output value is 50% higher.",
            ModifierScope.WholeRun,
            m => m.mergeValueMultiplier = 1.5f));

        cards.Add(Make("merge_spread_more_s", "Volatile Mix",
            "Merge results are more unpredictable — wider output range.",
            ModifierScope.WholeRun,
            m => m.mergeSpreadMultiplier = 2.0f));

        cards.Add(Make("merge_rarity_higher", "Superior Fusion",
            "Merge output rarity is guaranteed to be at least one tier above the average input rarity.",
            ModifierScope.WholeRun,
            m => { m.mergeGuaranteeSameRarity = true; m.mergeRarityBonus = 1; }));

        cards.Add(Make("merge_value_tight", "Master Craftsman",
            "Higher merge value and tighter spread — consistently good output.",
            ModifierScope.WholeRun,
            m => { m.mergeValueMultiplier = 1.3f; m.mergeSpreadMultiplier = 0.4f; }));

        return cards;
    }

    static FloorModifierCard Make(string id, string name, string desc,
                                  ModifierScope scope,
                                  System.Action<RunModifiers> apply)
    {
        var m = new RunModifiers();
        apply(m);
        return new FloorModifierCard { cardId = id, displayName = name, description = desc, scope = scope, modifier = m };
    }
}
