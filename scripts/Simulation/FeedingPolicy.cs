namespace WarriorsSim.Simulation;

/// <summary>
///     How a leader decides who eats first when there isn't enough prey for everyone.
///     Every cat gets a score from these weights, and the highest score eats first.
///     Later, the weights will come from the leader's personality.
/// </summary>
/// <param name="StarvingFirst">Bonus for cats that are starving right now. A high value means starving cats always eat first.</param>
/// <param name="Urgency">Weight for how close a cat is to death (starvation stacks).</param>
/// <param name="Compassion">Weight for protecting the weak: kits, elders, apprentices.</param>
/// <param name="Pragmatism">Weight for feeding the cats the Clan needs to survive: the best hunters.</param>
/// <param name="WarriorCode">Weight for the warrior code's order: kits, elders, leader, deputy, warriors, apprentices.</param>
public record FeedingPolicy(
    float StarvingFirst,
    float Urgency,
    float Compassion,
    float Pragmatism,
    float WarriorCode) {
    /// <summary>Starving cats first, then a mix of everything.</summary>
    public static readonly FeedingPolicy Balanced = new(10f, 1f, 0.5f, 0.5f, 0.5f);

    /// <summary>Protects the weak, even when it costs the Clan (a Firestar).</summary>
    public static readonly FeedingPolicy Compassionate = new(10f, 1f, 2f, 0f, 0.5f);

    /// <summary>Feeds whoever keeps the Clan alive. The weak die first (a Tigerclaw).</summary>
    public static readonly FeedingPolicy Ruthless = new(0f, 0f, 0f, 2f, 0f);

    /// <summary>Follows the warrior code to the letter, no matter who starves.</summary>
    public static readonly FeedingPolicy ByTheCode = new(0f, 0f, 0f, 0f, 1f);

    public float Score(Cat cat) {
        var starving = cat.IsStarving ? 1f : 0f;
        var nearDeath = (float)cat.StarvationStacks / Tuning.StarvationStacksToDie;

        return StarvingFirst * starving
               + Urgency * nearDeath
               + Compassion * Vulnerability(cat)
               + Pragmatism * Usefulness(cat)
               + WarriorCode * WarriorCodeRank(cat);
    }

    /// <summary>How much the cat needs protecting, from 0 to 1.</summary>
    private static float Vulnerability(Cat cat) {
        return cat.Rank switch {
            Rank.Kit => 1f,
            Rank.Elder => 1f,
            Rank.Apprentice => 0.5f,
            _ => 0f
        };
    }

    /// <summary>How much the Clan needs this cat to survive, from 0 to 1. For now: hunting skill.</summary>
    private static float Usefulness(Cat cat) {
        return cat.CanHunt ? (float)cat.Hunting / Tuning.SkillMax : 0f;
    }

    /// <summary>The warrior code's eating order, from 1 (eats first) to 0 (eats last).</summary>
    private static float WarriorCodeRank(Cat cat) {
        return cat.Rank switch {
            Rank.Kit => 1f,
            Rank.Elder => 0.8f,
            // Sick cats will go here (0.7) once sickness exists.
            Rank.Leader => 0.6f,
            Rank.Deputy => 0.4f,
            Rank.Warrior => 0.2f,
            _ => 0f
        };
    }
}
