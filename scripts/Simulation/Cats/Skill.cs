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
///     Learning works the same for every skill: the gift decides how fast the cat learns. How a skill is used
///     (catch chance, care, ...) is up to the code that uses it.
/// </summary>
public class Skill {
    private float _progress;

    public Skill(int level, int gift) {
        Level = Math.Clamp(level, Tuning.SkillMin, Tuning.SkillMax);
        Gift = Math.Clamp(gift, Tuning.SkillMin, Tuning.SkillMax);
    }

    /// <summary>How good the cat is, from <see cref="Tuning.SkillMin" /> to <see cref="Tuning.SkillMax" />.</summary>
    public int Level { get; private set; }

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
    ///     The level is a whole number, so parts of a point are kept until they add up to a full point. Otherwise a
    ///     gain of 5.4 every moon would always be rounded down to 5. Never above <paramref name="cap" />.
    /// </summary>
    public void Improve(float amount, int cap = Tuning.SkillMax) {
        if (Level >= cap)
            return;

        _progress += amount;
        var fullPoints = (int)_progress;
        _progress -= fullPoints;
        Level = Math.Clamp(Level + fullPoints, Tuning.SkillMin, cap);
    }
}
