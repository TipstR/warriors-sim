using System;
using System.Collections.Generic;

namespace WarriorsSim.Simulation;

/// <summary>
///     How a leader decides who eats first when there isn't enough prey for everyone.
///     Every cat gets a score from these weights, and the highest score eats first.
///     Later, the weights will come from the leader's personality.
/// </summary>
/// <param name="StarvingFirst">Bonus for cats that are starving right now. A high value means starving cats always eat first.</param>
/// <param name="Urgency">Weight for how close a cat is to death (starvation stacks).</param>
/// <param name="Compassion">
///     Weight for protecting the weak: queens, the sick and the frail (young kits, old cats). Goes by how weak a cat
///     really is, not by its rank.
/// </param>
/// <param name="Pragmatism">Weight for feeding the cats the Clan needs to survive: the best hunters.</param>
/// <param name="WarriorCode">Weight for the warrior code's order: kits, queens, elders, leader, deputy, warriors, apprentices.</param>
/// <param name="FamineHunterPriority">
///     Extra weight for the hunters during a famine, so the Clan stays able to hunt. Added to Pragmatism.
/// </param>
public record FeedingPolicy(
    float StarvingFirst,
    float Urgency,
    float Compassion,
    float Pragmatism,
    float WarriorCode,
    float FamineHunterPriority) {
    /// <summary>Starving cats first, then a mix of everything. In a famine, the hunters come first.</summary>
    public static readonly FeedingPolicy Balanced = new(10f, 1f, 0.5f, 0.5f, 0.5f, 2f);

    /// <summary>Protects the weak, even when it costs the Clan (a Firestar). Only reluctantly favors hunters in a famine.</summary>
    public static readonly FeedingPolicy Compassionate = new(10f, 1f, 2f, 0f, 0.5f, 0.5f);

    /// <summary>Feeds whoever keeps the Clan alive. The weak die first (a Tigerclaw).</summary>
    public static readonly FeedingPolicy Ruthless = new(0f, 0f, 0f, 2f, 0f, 2f);

    /// <summary>Follows the warrior code to the letter, no matter who starves, even in a famine.</summary>
    public static readonly FeedingPolicy ByTheCode = new(0f, 0f, 0f, 0f, 1f, 0f);

    /// <summary>All ready-made leader styles by name, e.g. for a dropdown in a settings screen.</summary>
    public static readonly IReadOnlyDictionary<string, FeedingPolicy> Presets = new Dictionary<string, FeedingPolicy> {
        ["Balanced"] = Balanced,
        ["Compassionate"] = Compassionate,
        ["Ruthless"] = Ruthless,
        ["By the code"] = ByTheCode
    };

    /// <param name="famine">Whether the Clan is in a famine right now, see <see cref="Clan.IsInFamine" />.</param>
    public float Score(Cat cat, bool famine) {
        var starving = cat.IsStarving ? 1f : 0f;
        var nearDeath = (float)cat.StarvationStacks / Tuning.StarvationStacksToDie;
        var pragmatism = Pragmatism + (famine ? FamineHunterPriority : 0f);

        return StarvingFirst * starving
               + Urgency * nearDeath
               + Compassion * Vulnerability(cat)
               + pragmatism * Usefulness(cat)
               + WarriorCode * WarriorCodeRank(cat);
    }

    /// <summary>
    ///     How much the cat needs protecting, from 0 to 1. This is what a compassionate leader sees: queens and sick
    ///     cats need it most, everyone else as much as their age makes them frail. Ranks only count for the warrior
    ///     code (see <see cref="WarriorCodeRank" />).
    /// </summary>
    private static float Vulnerability(Cat cat) {
        if (cat.IsQueen || cat.IsSick)
            return 1f;

        return cat.Frailty;
    }

    /// <summary>
    ///     How much the Clan needs this cat to survive, from 0 to 1: hunting skill, for medicine cats healing skill.
    ///     Apprentices are judged by what they promise to become: their gift, or their skill if it's already higher.
    /// </summary>
    private static float Usefulness(Cat cat) {
        var hunting = cat.Skills[SkillType.Hunting];
        var healing = cat.Skills[SkillType.Healing];
        var value = cat.Rank switch {
            Rank.MedicineCat => healing.Level,
            Rank.MedicineCatApprentice => Math.Max(healing.Level, healing.Gift),
            Rank.Apprentice => Math.Max(hunting.Level, hunting.Gift),
            _ => cat.CanHunt ? hunting.Level : 0
        };
        return (float)value / Tuning.SkillMax;
    }

    /// <summary>The warrior code's eating order, from 1 (eats first) to 0 (eats last): kits, queens, elders, sick, ...</summary>
    private static float WarriorCodeRank(Cat cat) {
        if (cat.IsQueen)
            return 0.9f;

        var rank = cat.Rank switch {
            Rank.Kit => 1f,
            Rank.Elder => 0.8f,
            // Medicine cats stand next to the leader: they are the Clan's spiritual leaders and save the most lives.
            // Their apprentice comes right after them.
            Rank.Leader => 0.6f,
            Rank.MedicineCat => 0.6f,
            Rank.MedicineCatApprentice => 0.5f,
            Rank.Deputy => 0.4f,
            Rank.Warrior => 0.2f,
            _ => 0f
        };
        // Sick cats eat right after the elders
        return cat.IsSick ? Math.Max(rank, 0.7f) : rank;
    }
}
