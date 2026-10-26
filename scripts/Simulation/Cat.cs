using System;

namespace WarriorsSim.Simulation;

public class Cat {
    public Cat(string name, Rank rank, int ageMoons, int hunting, int diligence) {
        Name = name;
        Rank = rank;
        AgeMoons = ageMoons;
        Hunting = Math.Clamp(hunting, Tuning.SkillMin, Tuning.SkillMax);
        Diligence = Math.Clamp(diligence, Tuning.SkillMin, Tuning.SkillMax);
        Satiation = Tuning.SatiationMax;
        Energy = Tuning.EnergyMax;
    }

    public string Name { get; }
    public Rank Rank { get; set; }
    public int AgeMoons { get; set; }

    /// <summary>Hunting skill, from <see cref="Tuning.SkillMin" /> to <see cref="Tuning.SkillMax" />.</summary>
    public int Hunting { get; }

    /// <summary>How hard-working the cat is. Lazy cats hunt worse.</summary>
    public int Diligence { get; }

    /// <summary>How full the cat is. 0 = starving, <see cref="Tuning.SatiationMax" /> = full.</summary>
    public float Satiation { get; private set; }

    /// <summary>How rested the cat is. 0 = exhausted, <see cref="MaxEnergy" /> = full.</summary>
    public float Energy { get; private set; }

    /// <summary>The most energy the cat can have. Starvation lowers it, see <see cref="Tuning.MaxEnergyLossPerStarvationStack" />.</summary>
    public float MaxEnergy =>
        Math.Max(0f, Tuning.EnergyMax - StarvationStacks * Tuning.MaxEnergyLossPerStarvationStack);

    public bool IsHungry => Satiation < Tuning.EatBelowSatiation;
    public bool IsStarving => Satiation <= 0f;

    public bool IsTired => Energy <= Tuning.SleepBelowEnergy;

    /// <summary>Below 1 counts as empty, so rounding errors (e.g. 0.00001) don't matter.</summary>
    public bool IsExhausted => Energy < 1f;

    /// <summary>
    ///     How long the cat has gone without food while starving. See <see cref="Tuning.StarvationStacksToDie" />.
    /// </summary>
    public int StarvationStacks { get; private set; }

    public bool HasStarvedToDeath => StarvationStacks >= Tuning.StarvationStacksToDie;

    public bool CanHunt => Rank is Rank.Apprentice or Rank.Warrior or Rank.Deputy or Rank.Leader;

    /// <summary>
    ///     How much food the cat needs compared to an adult (1 = adult, 0 = still nursing).
    ///     Read from <see cref="Tuning.FoodNeedByAge" />, interpolated between the points.
    /// </summary>
    public float FoodNeed {
        get {
            var points = Tuning.FoodNeedByAge;
            if (AgeMoons <= points[0].AgeMoons)
                return points[0].FoodNeed;

            for (var i = 1; i < points.Length; i++) {
                if (AgeMoons > points[i].AgeMoons)
                    continue;

                var (fromAge, fromNeed) = points[i - 1];
                var (toAge, toNeed) = points[i];
                var t = (float)(AgeMoons - fromAge) / (toAge - fromAge);
                return fromNeed + (toNeed - fromNeed) * t;
            }

            return points[^1].FoodNeed;
        }
    }

    /// <summary>Chance (0 to 1) that a single hunting attempt catches a mouse.</summary>
    public float CatchChance {
        get {
            var chance = Tuning.BaseCatchChance
                         + Hunting * Tuning.CatchChancePerHuntingPoint
                         + (Diligence - Tuning.AverageDiligence) * Tuning.CatchChancePerDiligencePoint;
            return Math.Clamp(chance * EnergyFactor, 0f, 1f);
        }
    }

    /// <summary>
    ///     How much tiredness lets the cat still perform, from 0 (nothing works) to 1 (no effect).
    ///     Not linear: an S-curve that barely matters above <see cref="Tuning.EnergyPenaltyStart" />
    ///     and drops to almost nothing towards <see cref="Tuning.EnergyPenaltyFull" />.
    /// </summary>
    public float EnergyFactor {
        get {
            var t = (Energy - Tuning.EnergyPenaltyFull) / (Tuning.EnergyPenaltyStart - Tuning.EnergyPenaltyFull);
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }

    public void LoseSatiation(float amount) {
        Satiation = Math.Max(0f, Satiation - amount);
    }

    /// <summary>Eating also removes one starvation stack.</summary>
    public void Eat(float satiation) {
        Satiation = Math.Min(Tuning.SatiationMax, Satiation + satiation);
        StarvationStacks = Math.Max(0, StarvationStacks - 1);
    }

    /// <summary>Also lowers <see cref="MaxEnergy" />, so the cat's current energy may drop with it.</summary>
    public void AddStarvationStack() {
        StarvationStacks++;
        Energy = Math.Min(Energy, MaxEnergy);
    }

    public void LoseEnergy(float amount) {
        Energy = Math.Max(0f, Energy - amount);
    }

    public void Sleep(float energy) {
        Energy = Math.Min(MaxEnergy, Energy + energy);
    }

    public override string ToString() {
        return $"{Name} ({Rank})";
    }
}