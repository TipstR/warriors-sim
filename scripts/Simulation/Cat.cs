using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

public class Cat {
    private int _moonsMentored;
    private float _huntingProgress;
    private int _healing;
    private float _healingProgress;
    private readonly List<Condition> _conditions = new();

    /// <summary>Until which day the cat can't catch a sickness again, because it just had it.</summary>
    private readonly Dictionary<ConditionType, int> _immuneUntilDay = new();

    public Cat(string name, Rank rank, Sex sex, int ageMoons, int hunting, int diligence,
        int huntingGift = Tuning.AverageGift, int mentoringGift = Tuning.AverageGift,
        Cat mother = null, Cat father = null) {
        (Prefix, Suffix) = CatNames.Split(name);
        Rank = rank;
        Sex = sex;
        AgeMoons = ageMoons;
        Hunting = Math.Clamp(hunting, Tuning.SkillMin, Tuning.SkillMax);
        Diligence = Math.Clamp(diligence, Tuning.SkillMin, Tuning.SkillMax);
        HuntingGift = Math.Clamp(huntingGift, Tuning.SkillMin, Tuning.SkillMax);
        MentoringGift = Math.Clamp(mentoringGift, Tuning.SkillMin, Tuning.SkillMax);
        Mother = mother;
        Father = father;
        Satiation = Tuning.SatiationMax;
        Energy = Tuning.EnergyMax;
    }

    public Sex Sex { get; }

    /// <summary>The cat's mother, or null if unknown.</summary>
    public Cat Mother { get; }

    /// <summary>The cat's father, or null if unknown.</summary>
    public Cat Father { get; }

    /// <summary>The cat's mate, or null. Only living cats are mates, see <see cref="Clan.RecordDeath" />.</summary>
    public Cat Mate { get; private set; }

    /// <summary>Days until the kits are born, or null if the cat isn't expecting kits.</summary>
    public int? PregnancyDaysLeft { get; private set; }

    /// <summary>The father of the kits she is expecting.</summary>
    public Cat FatherOfUnbornKits { get; private set; }

    public bool IsPregnant => PregnancyDaysLeft.HasValue;

    /// <summary>Whether she is nursing kits in the nursery. Set by the world every tick.</summary>
    public bool IsNursing { get; set; }

    /// <summary>
    ///     How many kits this queen's milk feeds right now. The queens in the nursery share all kits,
    ///     so this can be a fraction. Set by the world every tick.
    /// </summary>
    public float MilkLoad { get; set; }

    /// <summary>A queen is a she-cat who is expecting or nursing kits.</summary>
    public bool IsQueen => IsPregnant || IsNursing;

    /// <summary>Nursing queens and queens close to giving birth stay in the nursery.</summary>
    public bool IsInNursery => IsNursing || PregnancyDaysLeft <= Tuning.NurseryDaysBeforeBirth;

    /// <summary>Very young kits can only drink milk.</summary>
    public bool CanEatPrey => !(Rank == Rank.Kit && AgeMoons < Tuning.FirstPreyAgeMoons);

    /// <summary>The part of the name that stays for life, e.g. "Gray".</summary>
    public string Prefix { get; }

    /// <summary>The part of the name that changes with rank, e.g. "kit", "paw", "stripe", "star".</summary>
    public string Suffix { get; private set; }

    public string Name => Prefix + Suffix;
    public Rank Rank { get; private set; }
    public int AgeMoons { get; private set; }

    /// <summary>Hunting skill, from <see cref="Tuning.SkillMin" /> to <see cref="Tuning.SkillMax" />.</summary>
    public int Hunting { get; private set; }

    /// <summary>How hard-working the cat is. Lazy cats hunt worse.</summary>
    public int Diligence { get; }

    /// <summary>
    ///     How gifted the cat was born for hunting, from <see cref="Tuning.SkillMin" /> to <see cref="Tuning.SkillMax" />
    ///     (<see cref="Tuning.AverageGift" /> = average). Gifted apprentices learn faster.
    /// </summary>
    public int HuntingGift { get; }

    /// <summary>How gifted the cat was born for teaching. Some cats are just bad mentors, however long they mentor.</summary>
    public int MentoringGift { get; }

    /// <summary>
    ///     How gifted the cat was born for healing. Much rarer than the other gifts, see
    ///     <see cref="Tuning.HealingGiftRarity" />. Can be set for cats that start the game.
    /// </summary>
    public int HealingGift { get; init; } = Tuning.AverageHealingGift;

    /// <summary>
    ///     Healing skill, from <see cref="Tuning.SkillMin" /> to <see cref="Tuning.SkillMax" />. Medicine cats learn it
    ///     from their mentor and by caring for the sick. Other cats learn a little when they are cared for.
    ///     Can be set for cats that start the game.
    /// </summary>
    public int Healing {
        get => _healing;
        init => _healing = Math.Clamp(value, Tuning.SkillMin, Tuning.SkillMax);
    }

    public bool IsMedicineCat => Rank is Rank.MedicineCat or Rank.MedicineCatApprentice;

    /// <summary>The cat that trains this apprentice, or trained this warrior. Null if the cat never had one.</summary>
    public Cat Mentor { get; private set; }

    /// <summary>How many moons the cat has spent training apprentices. Can be set for cats that start the game.</summary>
    public int MoonsMentored {
        get => _moonsMentored;
        init => _moonsMentored = value;
    }

    /// <summary>From 0 (never mentored) to 1 (fully experienced, see <see cref="Tuning.MoonsToExperiencedMentor" />).</summary>
    public float MentoringExperience => Math.Min(1f, (float)MoonsMentored / Tuning.MoonsToExperiencedMentor);

    /// <summary>
    ///     How well the cat teaches, from 0 (worst) to 1 (best). It's the cat's mentoring gift, but an inexperienced
    ///     mentor only reaches <see cref="Tuning.InexperiencedMentorShare" /> of it.
    /// </summary>
    public float MentoringQuality {
        get {
            var share = Tuning.InexperiencedMentorShare
                        + (1f - Tuning.InexperiencedMentorShare) * MentoringExperience;
            return (float)MentoringGift / Tuning.SkillMax * share;
        }
    }

    /// <summary>How much faster (above 1) or slower (below 1) this cat's apprentice learns, from its mentoring quality.</summary>
    public float MentorFactor =>
        Tuning.WorstMentorFactor + (Tuning.BestMentorFactor - Tuning.WorstMentorFactor) * MentoringQuality;

    /// <summary>How much faster (above 1) or slower (below 1) this cat learns to hunt, from its hunting gift.</summary>
    public float HuntingLearningFactor => LearningFactor(HuntingGift);

    /// <summary>How much faster (above 1) or slower (below 1) this cat learns to heal, from its healing gift.</summary>
    public float HealingLearningFactor => LearningFactor(HealingGift);

    private static float LearningFactor(int gift) {
        return Tuning.LeastGiftedLearningFactor
               + (Tuning.MostGiftedLearningFactor - Tuning.LeastGiftedLearningFactor) * gift / Tuning.SkillMax;
    }

    /// <summary>Warriors, the deputy and the leader can mentor, but not while they are in the nursery.</summary>
    public bool CanMentor => Rank is Rank.Warrior or Rank.Deputy or Rank.Leader && !IsInNursery;

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

    /// <summary>Chance to die of starvation this tick, from 0 to 1.</summary>
    public float StarvationDeathChance {
        get {
            if (StarvationStacks >= Tuning.StarvationStacksToDie)
                return 1f;

            var chance = Math.Max(0, StarvationStacks - Tuning.StarvationSafeStacks)
                         * Tuning.StarvationDeathChancePerStack;
            if (IsFrail)
                chance *= Tuning.FrailStarvationMultiplier;
            return Math.Min(1f, chance);
        }
    }

    /// <summary>
    ///     Starving for longer than one hungry night (see <see cref="Tuning.FamineMinStacks" />).
    ///     Such a cat fights sickness worse.
    /// </summary>
    public bool IsWeakFromHunger => StarvationStacks >= Tuning.FamineMinStacks;

    /// <summary>Kits and elders survive hunger and sickness worse.</summary>
    public bool IsFrail => Rank is Rank.Kit or Rank.Elder;

    public bool CanHunt =>
        Rank is Rank.Apprentice or Rank.Warrior or Rank.Deputy or Rank.Leader && !IsInNursery && !IsResting;

    /// <summary>In a famine, elders and queens in the nursery help with hunting too.</summary>
    public bool CanHuntInFamine =>
        CanHunt || (Rank == Rank.Elder || (IsInNursery && Rank != Rank.Kit)) && !IsResting;

    /// <summary>Too sick to hunt, or resting in the medicine den.</summary>
    private bool IsResting => IsConfined || InMedicineDen;

    /// <summary>
    ///     Whether the cat is a patient in the medicine den, chosen by the medicine cat. Set by the world every tick.
    ///     Patients rest, recover faster and are away from the crowded dens.
    /// </summary>
    public bool InMedicineDen { get; set; }

    /// <summary>Where the cat sleeps. Queens sleep in the nursery with the kits, patients in the medicine den.</summary>
    public Den Den {
        get {
            if (InMedicineDen)
                return Den.Medicine;
            if (Rank == Rank.Kit || IsInNursery)
                return Den.Nursery;

            return Rank switch {
                Rank.Apprentice => Den.Apprentices,
                Rank.Elder => Den.Elders,
                Rank.Leader => Den.Leader,
                Rank.MedicineCat or Rank.MedicineCatApprentice => Den.Medicine,
                _ => Den.Warriors
            };
        }
    }

    /// <summary>The cat's sicknesses and injuries right now.</summary>
    public IReadOnlyList<Condition> Conditions => _conditions;

    /// <summary>Sick or injured. The warrior code treats both the same.</summary>
    public bool IsSick => _conditions.Count > 0;

    /// <summary>All severities together, at most <see cref="Condition.Max" />.</summary>
    public float TotalSeverity => Math.Min(Condition.Max, _conditions.Sum(c => c.Severity));

    /// <summary>Too sick to leave its nest, see <see cref="Tuning.ConfinedSeverity" />. A confined cat doesn't hunt.</summary>
    public bool IsConfined => TotalSeverity >= Tuning.ConfinedSeverity;

    /// <summary>How much sickness lets the cat still perform, from 1 (healthy) to 0 (severity 100).</summary>
    public float HealthFactor => 1f - TotalSeverity / Condition.Max;

    /// <summary>Whether the cat can spread a sickness right now.</summary>
    public bool IsContagious => _conditions.Any(c => c.IsContagious);

    /// <summary>Whether the cat can spread this sickness right now.</summary>
    public bool Spreads(ConditionType type) {
        return _conditions.Any(c => c.Type == type && c.IsContagious);
    }

    /// <summary>
    ///     How much food the cat needs compared to an adult (1 = adult, 0 = still nursing).
    ///     Pregnant and nursing queens need more.
    /// </summary>
    public float FoodNeed {
        get {
            var need = FoodNeedForAge;
            if (IsPregnant)
                need += Tuning.PregnancyExtraFoodNeed;
            return need + MilkLoad * Tuning.MilkFoodNeedPerKit;
        }
    }

    /// <summary>Read from <see cref="Tuning.FoodNeedByAge" />, interpolated between the points.</summary>
    private float FoodNeedForAge {
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
            if (Rank == Rank.Elder)
                chance *= Tuning.ElderHuntingMultiplier;
            return Math.Clamp(chance * EnergyFactor * HealthFactor, 0f, 1f);
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

    public void AgeOneMoon() {
        AgeMoons++;
    }

    /// <summary>
    ///     The skill is a whole number, so parts of a point are kept until they add up to a full point.
    ///     Otherwise a gain of 5.4 every moon would always be rounded down to 5.
    /// </summary>
    public void ImproveHunting(float amount) {
        _huntingProgress += amount;
        var fullPoints = (int)_huntingProgress;
        _huntingProgress -= fullPoints;
        Hunting = Math.Clamp(Hunting + fullPoints, Tuning.SkillMin, Tuning.SkillMax);
    }

    public bool HasCondition(ConditionType type) {
        return _conditions.Any(c => c.Type == type);
    }

    /// <summary>Whether the cat can catch this sickness on <paramref name="day" />: not while it has it or just had it.</summary>
    public bool CanCatch(ConditionType type, int day) {
        return !HasCondition(type) && (!_immuneUntilDay.TryGetValue(type, out var until) || day > until);
    }

    public void AddCondition(Condition condition) {
        _conditions.Add(condition);
    }

    /// <summary>Removes the condition. A cat that got over a sickness can't catch it again until <paramref name="immuneUntilDay" />.</summary>
    public void RemoveCondition(Condition condition, int immuneUntilDay) {
        _conditions.Remove(condition);
        _immuneUntilDay[condition.Type] = immuneUntilDay;
    }

    /// <summary>Replaces a condition with a worse one, e.g. whitecough with greencough. No immunity for the old one.</summary>
    public void ReplaceCondition(Condition old, Condition worse) {
        _conditions[_conditions.IndexOf(old)] = worse;
    }

    /// <summary>Like <see cref="ImproveHunting" />: parts of a point are kept. Never above <paramref name="cap" />.</summary>
    public void ImproveHealing(float amount, int cap = Tuning.SkillMax) {
        if (_healing >= cap)
            return;

        _healingProgress += amount;
        var fullPoints = (int)_healingProgress;
        _healingProgress -= fullPoints;
        _healing = Math.Clamp(_healing + fullPoints, Tuning.SkillMin, cap);
    }

    public void SetMentor(Cat mentor) {
        Mentor = mentor;
    }

    /// <summary>The cat has trained an apprentice for one more moon and gains experience as a mentor.</summary>
    public void MentorOneMoon() {
        _moonsMentored++;
    }

    /// <summary>
    ///     Changes the cat's rank. Kits, apprentices and leaders get the matching name suffix,
    ///     new warriors and medicine cats get <paramref name="warriorSuffix" />. Deputies and elders keep their name.
    /// </summary>
    public void ChangeRank(Rank rank, string warriorSuffix = null) {
        Rank = rank;
        Suffix = rank switch {
            Rank.Kit => CatNames.KitSuffix,
            Rank.Apprentice or Rank.MedicineCatApprentice => CatNames.ApprenticeSuffix,
            Rank.Warrior or Rank.MedicineCat when warriorSuffix != null => warriorSuffix,
            Rank.Leader => CatNames.LeaderSuffix,
            _ => Suffix
        };
    }

    /// <summary>Parents, children and siblings (including half-siblings).</summary>
    public bool IsCloseKinOf(Cat other) {
        var isParentOrChild = Mother == other || Father == other || other.Mother == this || other.Father == this;
        var isSibling = (Mother != null && Mother == other.Mother) || (Father != null && Father == other.Father);
        return isParentOrChild || isSibling;
    }

    public void BecomeMates(Cat other) {
        Mate = other;
        other.Mate = this;
    }

    public void LoseMate() {
        Mate = null;
    }

    public void BecomePregnant(Cat father) {
        PregnancyDaysLeft = Tuning.PregnancyDays;
        FatherOfUnbornKits = father;
    }

    /// <summary>Counts down one day of pregnancy. Returns true when the kits are born.</summary>
    public bool AdvancePregnancy() {
        if (!IsPregnant)
            return false;

        PregnancyDaysLeft--;
        if (PregnancyDaysLeft > 0)
            return false;

        PregnancyDaysLeft = null;
        return true;
    }

    public override string ToString() {
        return $"{Name} ({Rank})";
    }
}