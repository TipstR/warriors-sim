using System;

namespace WarriorsSim.Simulation;

/// <summary>The skills a cat can learn, see <see cref="Cat.Skills" />.</summary>
public enum SkillType {
    Hunting,

    /// <summary>Medicine cats learn it from their mentor and by caring for the sick, other cats a little as patients.</summary>
    Healing
}

/// <summary>
///     How good a cat is at one thing (<see cref="Level" />) and how gifted it was born for it (<see cref="Gift" />).
///     Learning works the same for every skill: gifted cats learn faster, and every level is a bit harder than the one
///     before. How a skill is used (catch chance, care, ...) is up to the code that uses it.
/// </summary>
public class Skill {
    public Skill(float level, int gift) {
        Level = Math.Clamp(level, Tuning.SkillMin, Tuning.SkillMax);
        Gift = Math.Clamp(gift, Tuning.SkillMin, Tuning.SkillMax);
    }

    /// <summary>
    ///     How good the cat is, from <see cref="Tuning.SkillMin" /> to <see cref="Tuning.SkillMax" />. A decimal, so
    ///     even a small lesson counts. Rounded only for display.
    /// </summary>
    public float Level { get; private set; }

    /// <summary>
    ///     How gifted the cat was born for this, from <see cref="Tuning.SkillMin" /> to <see cref="Tuning.SkillMax" />.
    ///     Gifted cats learn faster.
    /// </summary>
    public int Gift { get; }

    /// <summary>How much faster (above 1) or slower (below 1) the cat learns this skill, from its gift.</summary>
    public float LearningFactor =>
        Tuning.LeastGiftedLearningFactor
        + (Tuning.MostGiftedLearningFactor - Tuning.LeastGiftedLearningFactor) * Gift / Tuning.SkillMax;

    /// <summary>
    ///     How much of a lesson still sticks at the current level, from 1 (a beginner learns everything) to 0
    ///     (perfect, nothing left to learn). See <see cref="Tuning.SkillDifficulty" />.
    /// </summary>
    public float Difficulty => MathF.Pow(1f - Level / Tuning.SkillMax, Tuning.SkillDifficulty);

    /// <summary>
    ///     The cat learns: the skill grows by <paramref name="amount" /> × <see cref="LearningFactor" /> ×
    ///     <see cref="Difficulty" />, so the first levels come quickly and only the best get very high.
    ///     Never above <paramref name="cap" />.
    /// </summary>
    public void Improve(float amount, float cap = Tuning.SkillMax) {
        if (Level >= cap)
            return;

        Level = Math.Min(cap, Level + amount * LearningFactor * Difficulty);
    }
}
