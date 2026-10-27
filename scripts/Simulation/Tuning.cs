using System;

namespace WarriorsSim.Simulation;

/// <summary>
///     All balancing numbers of the simulation in one place. They can be changed before a simulation is started,
///     e.g. from a settings screen. <see cref="TuningSettings" /> lists them all, grouped, and can reset them.
///     Changing them while a simulation is running works too, but can give strange results.
///     See docs/Milestone1.md for what they mean.
/// </summary>
public static class Tuning {
    // Fixed values: the code depends on them (e.g. TimeOfDay has exactly 4 values), so they can't be tuned
    public const int TicksPerDay = 4;
    public const int SkillMin = 0;
    public const int SkillMax = 100;

    /// <summary>The middle of the gift scale. Cats created without a gift (e.g. from the UI) get this.</summary>
    public const int AverageGift = 50;

    /// <summary>
    ///     The average healing gift. Lower than the other gifts, because a gift for healing is rare
    ///     (with <see cref="HealingGiftRarity" /> 4, the average of the luck roll is 100 / 5 = 20).
    /// </summary>
    public const int AverageHealingGift = 20;

    // Time
    [TuningGroup("Time")] public static int DaysPerMoon = 30;
    [TuningGroup("Time")] public static int MoonsPerSeason = 3;
    [TuningGroup("Time")] public static int DaysToSimulate = 720;

    // Skills and traits
    [TuningGroup("Skills")] public static int AverageDiligence = 50;

    // Aging and ranks
    [TuningGroup("Aging")] public static int ApprenticeAgeMoons = 6;
    [TuningGroup("Aging")] public static int WarriorAgeMoons = 12;

    /// <summary>Warriors and deputies retire to the elders' den at this age. Leaders never retire.</summary>
    [TuningGroup("Aging")] public static int RetireAgeMoons = 120;

    /// <summary>
    ///     How much an apprentice's hunting skill improves every moon of training, before its gift and its mentor are
    ///     applied (see the Gifts and Mentors groups). Most mentors are not fully experienced, so the average
    ///     apprentice learns a bit less than this. With 8.7, the average new warrior has about 50 hunting.
    ///     Careful: the Clan's survival is very sensitive to that average.
    /// </summary>
    [TuningGroup("Aging")] public static float ApprenticeHuntingGainPerMoon = 8.7f;

    /// <summary>From this age on, a cat can die of old age (96 moons = 8 years).</summary>
    [TuningGroup("Aging")] public static int OldAgeStartMoons = 96;

    /// <summary>Chance per moon to die of old age at <see cref="OldAgeStartMoons" />.</summary>
    [TuningGroup("Aging")] public static float OldAgeDeathChanceAtStart = 0.005f;

    /// <summary>The chance to die of old age doubles every this many moons.</summary>
    [TuningGroup("Aging")] public static int OldAgeDeathChanceDoublingMoons = 18;

    // Gifts
    /// <summary>
    ///     How much of a kit's gift comes from its parents (the average of both). The rest is luck.
    ///     0 = only luck, 1 = kits are exactly as gifted as their parents.
    /// </summary>
    [TuningGroup("Gifts")] public static float GiftInheritance = 0.5f;

    /// <summary>How fast the least gifted cat (gift 0) learns to hunt. 1 = average.</summary>
    [TuningGroup("Gifts")] public static float LeastGiftedLearningFactor = 0.3f;

    /// <summary>How fast the most gifted cat (gift 100) learns to hunt. 1 = average.</summary>
    [TuningGroup("Gifts")] public static float MostGiftedLearningFactor = 1.7f;

    /// <summary>
    ///     How rare a gift for healing is. The luck part of the healing gift is 100 × (random 0–1) to this power.
    ///     With 4: half of all cats have a luck roll below 6, three in four below 30, and only about 1 in 8 above 60.
    /// </summary>
    [TuningGroup("Gifts")] public static float HealingGiftRarity = 4f;

    // Mentors
    /// <summary>How fast an apprentice learns with the worst possible mentor. 1 = average.</summary>
    [TuningGroup("Mentors")] public static float WorstMentorFactor = 0.5f;

    /// <summary>How fast an apprentice learns with the best possible mentor. 1 = average.</summary>
    [TuningGroup("Mentors")] public static float BestMentorFactor = 1.5f;

    /// <summary>How fast an apprentice learns when no warrior is free to mentor it.</summary>
    [TuningGroup("Mentors")] public static float NoMentorFactor = 0.3f;

    /// <summary>A mentor who has never mentored before only teaches with this share of its mentoring gift.</summary>
    [TuningGroup("Mentors")] public static float InexperiencedMentorShare = 0.6f;

    /// <summary>After this many moons of mentoring, a mentor teaches with its full gift (24 = about four apprentices).</summary>
    [TuningGroup("Mentors")] public static int MoonsToExperiencedMentor = 24;

    // Medicine cats
    /// <summary>A medicine cat apprentice becomes a full medicine cat at this age. Longer than a warrior's training.</summary>
    [TuningGroup("Medicine")] public static int MedicineCatAgeMoons = 18;

    /// <summary>
    ///     How much a medicine cat apprentice's healing skill improves every moon, before its healing gift and its
    ///     mentor are applied (like <see cref="ApprenticeHuntingGainPerMoon" />).
    /// </summary>
    [TuningGroup("Medicine")] public static float MedicineApprenticeHealingGainPerMoon = 6f;

    /// <summary>From this age on, a medicine cat looks for an apprentice to take over one day.</summary>
    [TuningGroup("Medicine")] public static int SeekMedicineApprenticeAgeMoons = 60;

    /// <summary>
    ///     A kit needs at least this healing gift to become a medicine cat apprentice. If the Clan has no medicine cat
    ///     at all, the most gifted kit is taken anyway.
    /// </summary>
    [TuningGroup("Medicine")] public static int MedicineApprenticeMinGift = 30;

    /// <summary>How many nests the medicine den has. When it's full, the sickest cats get the nests.</summary>
    [TuningGroup("Medicine")] public static int MedicineDenNests = 5;

    /// <summary>From this total severity on, the medicine cat takes a cat into the medicine den.</summary>
    [TuningGroup("Medicine")] public static float MedicineDenMinSeverity = 20f;

    /// <summary>A patient leaves the medicine den once its severity is below this. Lower than the min, so it doesn't come and go every tick.</summary>
    [TuningGroup("Medicine")] public static float MedicineDenLeaveSeverity = 5f;

    /// <summary>
    ///     Patients in the medicine den rest and recover this much faster (0.1 = 10% faster), on top of care.
    ///     Keep it small: the den's main use is keeping the sick away from the others. Together with care, a kit or
    ///     elder with greencough still dies about one time in three when there's no catmint.
    /// </summary>
    [TuningGroup("Medicine")] public static float MedicineDenRecoveryBonus = 0.1f;

    /// <summary>How many sick or injured cats one healer can care for per tick. Patients in the medicine den come first.</summary>
    [TuningGroup("Medicine")] public static int PatientsPerHealer = 3;

    /// <summary>
    ///     How much care speeds up recovery: a cat cared for by a healer with healing 100 recovers this much faster
    ///     (0.25 = 25% faster), with healing 50 half as much. Care alone doesn't save most kits and elders from
    ///     greencough, the right herb does (catmint).
    /// </summary>
    [TuningGroup("Medicine")] public static float CareRecoveryBonus = 0.25f;

    /// <summary>A wound cared for by a healer with healing 100 gets infected this much less often (0.5 = half as often).</summary>
    [TuningGroup("Medicine")] public static float CareInfectionReduction = 0.5f;

    /// <summary>How much a healer's skill improves every time it cares for a cat, before its healing gift is applied.</summary>
    [TuningGroup("Medicine")] public static float HealingPracticePerCare = 0.02f;

    /// <summary>
    ///     How much a cat that is cared for learns about healing every time, before its healing gift is applied.
    ///     "Cats that are often in the medicine den learn one or two things."
    /// </summary>
    [TuningGroup("Medicine")] public static float PatientHealingGainPerCare = 0.05f;

    /// <summary>Cats who aren't medicine cats only learn healing up to this share of their healing gift.</summary>
    [TuningGroup("Medicine")] public static float PatientHealingCapShare = 0.5f;

    /// <summary>
    ///     Without a medicine cat, the Clan's best healer (a warrior or elder who learned a little) cares for the sick,
    ///     but only if its healing skill is at least this.
    /// </summary>
    [TuningGroup("Medicine")] public static int MinHealingToCare = 5;

    // Herbs
    /// <summary>
    ///     How much faster a cat recovers on a day it got the right herb (0.75 = 75% faster). With catmint, even most
    ///     kits and elders survive greencough.
    /// </summary>
    [TuningGroup("Herbs")] public static float HerbRecoveryBonus = 0.75f;

    /// <summary>A wound treated with cobwebs gets infected this much less often (0.7 = 70% less often).</summary>
    [TuningGroup("Herbs")] public static float CobwebInfectionReduction = 0.7f;

    /// <summary>The medicine cat goes gathering when the store has fewer doses than this of any herb.</summary>
    [TuningGroup("Herbs")] public static int HerbStoreTarget = 8;

    /// <summary>
    ///     How many doses of each herb a gathering trip finds on average, before the season and the gatherer's skill.
    ///     A healer with healing 100 finds all of it, one with healing 0 half.
    /// </summary>
    [TuningGroup("Herbs")] public static float HerbsPerTrip = 3f;

    /// <summary>Herbs lose this share of their doses every day (0.02 = 2%, about half after a moon).</summary>
    [TuningGroup("Herbs")] public static float HerbSpoilPerDay = 0.02f;

    /// <summary>How many doses of every herb the medicine cat has when the game starts.</summary>
    [TuningGroup("Herbs")] public static int StartingHerbDoses = 6;

    /// <summary>How many herbs grow in each season. Cobwebs aren't plants and can be found all year.</summary>
    [TuningGroup("Herbs")] public static float NewleafHerbMultiplier = 1.0f;

    [TuningGroup("Herbs")] public static float GreenleafHerbMultiplier = 1.2f;
    [TuningGroup("Herbs")] public static float LeafFallHerbMultiplier = 0.6f;
    [TuningGroup("Herbs")] public static float LeafBareHerbMultiplier = 0.15f;

    /// <summary>Catmint is even rarer in leaf-bare than the other herbs: multiplied on top of the leaf-bare value.</summary>
    [TuningGroup("Herbs")] public static float CatmintLeafBareMultiplier = 0.3f;

    // Mates, queens and kits
    /// <summary>Chance per moon that a single she-cat warrior finds a mate (placeholder until relationships exist).</summary>
    [TuningGroup("Family")] public static float MateChancePerMoon = 0.15f;

    [TuningGroup("Family")] public static int MaxMateAgeDifferenceMoons = 36;

    /// <summary>She-cats can have kits from <see cref="WarriorAgeMoons" /> until this age.</summary>
    [TuningGroup("Family")] public static int FertileUntilMoons = 96;

    /// <summary>Chance per moon that a she-cat with a mate expects kits, before season and food are applied.</summary>
    [TuningGroup("Family")] public static float ConceptionChancePerMoon = 0.3f;

    /// <summary>
    ///     How much hunger lowers the chance of kits. With 5: if the Clan missed 20% of its meals this moon,
    ///     the chance is halved.
    /// </summary>
    [TuningGroup("Family")] public static float HungerConceptionPenalty = 5f;

    /// <summary>
    ///     Births get rarer the closer the Clan gets to the number of cats the territory feeds in this season.
    ///     With a good season, the Clan can grow beyond what leaf-bare can feed, and leaf-bare thins it out.
    /// </summary>
    [TuningGroup("Family")] public static Season CrowdingSeason = Season.Newleaf;

    /// <summary>
    ///     Even in a full Clan, the chance of kits never drops below this share (0.2 = 20% of the usual chance).
    ///     Then hunger thins the Clan instead, and the Clan stays young. Tested with 100 seeds × 30 years:
    ///     0.3 → 4% of Clans die out, ~11 cats starve per year; 0.2 → 12%, ~7; 0.15 → 16%, ~5.
    ///     With 0 (the old hard brake), about half the Clans grew old together and died out.
    /// </summary>
    [TuningGroup("Family")] public static float MinCrowdingFactor = 0.2f;

    /// <summary>Real cats are pregnant for 63–65 days.</summary>
    [TuningGroup("Family")] public static int PregnancyDays = 63;

    /// <summary>A pregnant queen moves to the nursery and stops hunting this many days before the birth.</summary>
    [TuningGroup("Family")] public static int NurseryDaysBeforeBirth = 21;

    [TuningGroup("Family")] public static float PregnancyExtraFoodNeed = 0.25f;

    /// <summary>Kits drink milk until this age.</summary>
    [TuningGroup("Family")] public static int WeanedAgeMoons = 2;

    /// <summary>Kits younger than this can't eat prey at all, only milk.</summary>
    [TuningGroup("Family")] public static int FirstPreyAgeMoons = 1;

    /// <summary>How many kits one queen's milk can feed. Queens share all kits in the nursery.</summary>
    [TuningGroup("Family")] public static int MaxKitsPerQueen = 6;

    /// <summary>Extra food a queen needs for every kit she nurses (real nursing queens need 2–3× as much).</summary>
    [TuningGroup("Family")] public static float MilkFoodNeedPerKit = 0.3f;

    /// <summary>How likely each litter size is. Index = number of kits, value = weight. Real litters: usually 3–4.</summary>
    public static readonly int[] LitterSizeWeights = { 0, 5, 15, 30, 30, 15, 5 };

    // Satiation
    [TuningGroup("Satiation")] public static float SatiationMax = 100f;
    [TuningGroup("Satiation")] public static int TicksUntilEmpty = 6;
    [TuningGroup("Satiation")] public static float EatBelowSatiation = 75f;
    [TuningGroup("Satiation")] public static float MouseSatiation = 25f;

    public static float SatiationLossPerTick => SatiationMax / TicksUntilEmpty;

    /// <summary>
    ///     How much food a cat needs compared to an adult, by age in moons. Ages in between are interpolated.
    ///     Based on real cats: kittens only nurse until about 1 moon and are weaned by 2 moons. Because they need
    ///     so much more food per kg, they eat about as much as an adult from 3 moons on, a little more while
    ///     growing fast, and are fully grown at about 12 moons.
    /// </summary>
    public static readonly (int AgeMoons, float FoodNeed)[] FoodNeedByAge = {
        (0, 0f),
        (1, 0f),
        (2, 0.8f),
        (3, 0.95f),
        (4, 1.05f),
        (6, 1.1f),
        (9, 1.15f),
        (12, 1f)
    };

    // Starvation
    /// <summary>
    ///     Every tick a starving cat doesn't eat adds a stack, every meal removes one.
    ///     At this many stacks the cat always dies (20 ticks = 5 days without food).
    /// </summary>
    [TuningGroup("Starvation")] public static int StarvationStacksToDie = 20;

    /// <summary>Up to this many stacks, starving can't kill a cat yet.</summary>
    [TuningGroup("Starvation")] public static int StarvationSafeStacks = 8;

    /// <summary>
    ///     Above <see cref="StarvationSafeStacks" />, every further stack adds this much chance per tick to die.
    ///     So not every starving cat dies at the same moment: e.g. at 14 stacks 6% per tick.
    /// </summary>
    [TuningGroup("Starvation")] public static float StarvationDeathChancePerStack = 0.01f;

    /// <summary>Kits and elders are frailer: their chance to die of starvation is multiplied by this.</summary>
    [TuningGroup("Starvation")] public static float FrailStarvationMultiplier = 2f;

    /// <summary>
    ///     The Clan is in a famine when at least this many cats carry starvation stacks.
    ///     Then elders and queens help hunting, and most leaders feed the hunters first.
    /// </summary>
    [TuningGroup("Starvation")] public static int FamineStarvingCats = 3;

    /// <summary>
    ///     A cat only counts as starving for a famine with at least this many stacks.
    ///     One stack after a hungry night is normal, it's not a famine yet.
    /// </summary>
    [TuningGroup("Starvation")] public static int FamineMinStacks = 2;

    /// <summary>
    ///     A famine only ends after fewer than <see cref="FamineStarvingCats" /> cats were starving for this many
    ///     ticks in a row (12 = three days). Keeps the famine from starting and ending every day.
    /// </summary>
    [TuningGroup("Starvation")] public static int FamineEndTicks = 12;

    /// <summary>Elders who help hunting in a famine are slower than they used to be.</summary>
    [TuningGroup("Starvation")] public static float ElderHuntingMultiplier = 0.6f;

    /// <summary>Each starvation stack lowers the cat's max energy by this much.</summary>
    [TuningGroup("Starvation")] public static float MaxEnergyLossPerStarvationStack = 4f;

    /// <summary>
    ///     Who eats first when there isn't enough prey. Pick one of <see cref="FeedingPolicy.Presets" />
    ///     or make your own with new FeedingPolicy(...).
    /// </summary>
    public static FeedingPolicy DefaultFeedingPolicy = FeedingPolicy.Balanced;

    // Sickness
    /// <summary>
    ///     Chance per day that a healthy cat catches whitecough on its own, before the season is applied
    ///     (see <see cref="SeasonSicknessMultiplier" />). Most cases come from other cats, see the contagion values.
    /// </summary>
    [TuningGroup("Sickness")] public static float WhitecoughChancePerDay = 0.001f;

    /// <summary>Chance per day that a healthy cat catches greencough on its own. Most greencough starts as whitecough.</summary>
    [TuningGroup("Sickness")] public static float GreencoughChancePerDay = 0.0001f;

    /// <summary>
    ///     Chance per day that a cat with whitecough infects a cat sleeping in the same den, before the season.
    ///     Small numbers matter a lot here: a sick cat is contagious for about a week and shares its den with up
    ///     to 15 cats. On average it infects about days × chance × cats in the den × season others. Above 1, the
    ///     sickness spreads through the whole Clan. With 0.006 in leaf-bare: 7 × 0.006 × 15 × 2 ≈ 1.2,
    ///     so many cats catch it in leaf-bare, but not all.
    /// </summary>
    [TuningGroup("Sickness")] public static float WhitecoughContagionPerDay = 0.006f;

    /// <summary>
    ///     With 0.003 in leaf-bare: 10 days × 0.003 × 15 × 2 ≈ 0.9 cats infected per sick cat. Below 1, so most
    ///     outbreaks die out on their own, but some leaf-bares bring a big one.
    /// </summary>
    [TuningGroup("Sickness")] public static float GreencoughContagionPerDay = 0.003f;

    /// <summary>Cats from other dens are infected this much less often than cats in the same den.</summary>
    [TuningGroup("Sickness")] public static float OtherDenContagionShare = 0.1f;

    /// <summary>
    ///     Chance per day that whitecough turns into greencough, before the season. Much higher for weak cats,
    ///     see <see cref="WeakGreencoughMultiplier" />.
    /// </summary>
    [TuningGroup("Sickness")] public static float GreencoughFromWhitecoughPerDay = 0.002f;

    /// <summary>Kits, elders and cats weak from hunger with whitecough get greencough this much more often.</summary>
    [TuningGroup("Sickness")] public static float WeakGreencoughMultiplier = 3f;

    /// <summary>How much worse a sickness gets every day, for an average case.</summary>
    [TuningGroup("Sickness")] public static float WhitecoughSeverityPerDay = 6f;

    [TuningGroup("Sickness")] public static float GreencoughSeverityPerDay = 9f;

    /// <summary>
    ///     How fast a healthy adult fights off the sickness, per day. Recovery is done at 100, so with 15 the
    ///     worst is over after about a week.
    ///     Whether a cat dies depends on recovery against severity: a cat dies if its case gets worse faster than
    ///     recovery / severity (times its recovery multiplier). For greencough (10.5 / 9 = 1.17) that's about
    ///     1 in 5 healthy adults, and with <see cref="FrailRecoveryMultiplier" /> 0.75 about 2 in 3 kits and elders.
    ///     The medicine cat will lower this.
    /// </summary>
    [TuningGroup("Sickness")] public static float WhitecoughRecoveryPerDay = 15f;

    [TuningGroup("Sickness")] public static float GreencoughRecoveryPerDay = 10.5f;

    /// <summary>Once the worst is over, the severity goes down by this much per day.</summary>
    [TuningGroup("Sickness")] public static float HealingPerDay = 10f;

    /// <summary>
    ///     How different one case is from another: with 0.3, a case gets worse between 30% slower and 30% faster
    ///     than usual.
    /// </summary>
    [TuningGroup("Sickness")] public static float CaseSpeedVariation = 0.3f;

    /// <summary>Kits and elders fight sickness worse: their recovery is multiplied by this.</summary>
    [TuningGroup("Sickness")] public static float FrailRecoveryMultiplier = 0.75f;

    /// <summary>
    ///     Cats weak from hunger (<see cref="Cat.IsWeakFromHunger" />) fight sickness worse: their recovery is
    ///     multiplied by this. Careful: hunger and sickness feed each other (sick hunters catch less), so a strong
    ///     penalty quickly kills the whole Clan in leaf-bare. With 0.85, about half of the adults weak from hunger
    ///     die of greencough.
    /// </summary>
    [TuningGroup("Sickness")] public static float StarvingRecoveryMultiplier = 0.85f;

    /// <summary>From this total severity on, a cat is too sick to leave its nest and doesn't hunt.</summary>
    [TuningGroup("Sickness")] public static float ConfinedSeverity = 60f;

    /// <summary>After getting over a sickness, a cat can't catch it again for this many days.</summary>
    [TuningGroup("Sickness")] public static int ImmunityDays = 90;

    // Injuries
    /// <summary>
    ///     Chance per hunting patrol that a cat gets hurt, for a rested cat of average diligence.
    ///     Tired cats get hurt up to twice as often, lazy cats up to 1.5 times as often, very diligent cats less.
    ///     Hunters often go on more than one patrol a day, so with 0.002 a Clan of 30 has about 2 injuries a moon.
    /// </summary>
    [TuningGroup("Injuries")] public static float InjuryChancePerPatrol = 0.002f;

    /// <summary>A new injury's severity is rolled between these. Above <see cref="ConfinedSeverity" /> the cat can't hunt.</summary>
    [TuningGroup("Injuries")] public static float InjuryMinSeverity = 10f;

    [TuningGroup("Injuries")] public static float InjuryMaxSeverity = 65f;

    /// <summary>How fast a wound closes, per day (done at 100). With 20, the worst is over after five days.</summary>
    [TuningGroup("Injuries")] public static float WoundRecoveryPerDay = 20f;

    /// <summary>Chance per day that a wound that hasn't closed yet gets infected. Doubled for cats weak from hunger.</summary>
    [TuningGroup("Injuries")] public static float WoundInfectionPerDay = 0.03f;

    /// <summary>
    ///     An infected wound gets worse by this much per day. It keeps the severity and recovery of the wound, so
    ///     it's mostly deadly when the wound was bad and got infected early.
    /// </summary>
    [TuningGroup("Injuries")] public static float InfectedWoundSeverityPerDay = 5f;

    [TuningGroup("Injuries")] public static float InfectedWoundRecoveryPerDay = 10f;

    // Energy
    [TuningGroup("Energy")] public static float EnergyMax = 100f;
    [TuningGroup("Energy")] public static int TicksUntilExhausted = 5;
    [TuningGroup("Energy")] public static float SleepBelowEnergy = 33f;
    [TuningGroup("Energy")] public static float SleepEnergy = 70f;

    /// <summary>Above this energy, tiredness has (almost) no effect.</summary>
    [TuningGroup("Energy")] public static float EnergyPenaltyStart = 80f;

    /// <summary>Below this energy, a cat can't do anything useful anymore.</summary>
    [TuningGroup("Energy")] public static float EnergyPenaltyFull = 5f;

    public static float EnergyLossPerTick => EnergyMax / TicksUntilExhausted;

    // Hunting
    [TuningGroup("Hunting")] public static int PatrolSize = 4;

    /// <summary>The leader sends patrols until they are expected to catch this much more than needed.</summary>
    [TuningGroup("Hunting")] public static float PatrolPlanningMargin = 1.2f;

    [TuningGroup("Hunting")] public static int AttemptsPerCat = 6;
    [TuningGroup("Hunting")] public static float BaseCatchChance = 0.1f;
    [TuningGroup("Hunting")] public static float CatchChancePerHuntingPoint = 0.006f;
    [TuningGroup("Hunting")] public static float CatchChancePerDiligencePoint = 0.002f;

    /// <summary>
    ///     How much prey the territory has per day, multiplied by <see cref="SeasonCatchMultiplier" />.
    ///     The more is caught in a day, the harder the next catch, until none is left.
    ///     This limits how many cats the territory can feed (an adult eats about 2.7 mice a day).
    /// </summary>
    [TuningGroup("Hunting")] public static float TerritoryPreyPerDay = 100f;

    /// <summary>
    ///     How suddenly prey gets scarce. With 4, hunting is almost normal until half of the day's prey is caught
    ///     (94% chance), then drops quickly (75% caught: 68%, 90% caught: 34%, all caught: 0%).
    /// </summary>
    [TuningGroup("Hunting")] public static float PreyDepletionSharpness = 4f;

    // Seasons
    /// <summary>How much easier or harder hunting is in each season. 1 = normal.</summary>
    [TuningGroup("Seasons")] public static float NewleafCatchMultiplier = 1.0f;

    [TuningGroup("Seasons")] public static float GreenleafCatchMultiplier = 1.3f;
    [TuningGroup("Seasons")] public static float LeafFallCatchMultiplier = 1.0f;
    [TuningGroup("Seasons")] public static float LeafBareCatchMultiplier = 0.8f;

    /// <summary>Cats mostly have kits in newleaf and greenleaf. Leaf-bare kits are rare.</summary>
    [TuningGroup("Seasons")] public static float NewleafConceptionMultiplier = 1.0f;

    [TuningGroup("Seasons")] public static float GreenleafConceptionMultiplier = 1.0f;
    [TuningGroup("Seasons")] public static float LeafFallConceptionMultiplier = 0.3f;
    [TuningGroup("Seasons")] public static float LeafBareConceptionMultiplier = 0.1f;

    /// <summary>Cats get sick and infect each other more often in the cold seasons.</summary>
    [TuningGroup("Seasons")] public static float NewleafSicknessMultiplier = 0.5f;

    [TuningGroup("Seasons")] public static float GreenleafSicknessMultiplier = 0.2f;
    [TuningGroup("Seasons")] public static float LeafFallSicknessMultiplier = 1.0f;
    [TuningGroup("Seasons")] public static float LeafBareSicknessMultiplier = 2.0f;

    // Randomness
    [TuningGroup("Randomness")] public static int RandomSeed = 12345;

    public static float SeasonCatchMultiplier(Season season) {
        return season switch {
            Season.Newleaf => NewleafCatchMultiplier,
            Season.Greenleaf => GreenleafCatchMultiplier,
            Season.LeafFall => LeafFallCatchMultiplier,
            Season.LeafBare => LeafBareCatchMultiplier,
            _ => 1.0f
        };
    }

    public static float SeasonSicknessMultiplier(Season season) {
        return season switch {
            Season.Newleaf => NewleafSicknessMultiplier,
            Season.Greenleaf => GreenleafSicknessMultiplier,
            Season.LeafFall => LeafFallSicknessMultiplier,
            Season.LeafBare => LeafBareSicknessMultiplier,
            _ => 1.0f
        };
    }

    /// <summary>The herb that treats the condition.</summary>
    public static Herb HerbFor(ConditionType type) {
        return type switch {
            ConditionType.Whitecough => Herb.Tansy,
            ConditionType.Greencough => Herb.Catmint,
            ConditionType.Wound => Herb.Cobweb,
            ConditionType.InfectedWound => Herb.Marigold,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    /// <summary>How many herbs can be found in the season, see <see cref="HerbsPerTrip" />.</summary>
    public static float HerbSeasonMultiplier(Herb herb, Season season) {
        if (herb == Herb.Cobweb)
            return 1f;

        var multiplier = season switch {
            Season.Newleaf => NewleafHerbMultiplier,
            Season.Greenleaf => GreenleafHerbMultiplier,
            Season.LeafFall => LeafFallHerbMultiplier,
            Season.LeafBare => LeafBareHerbMultiplier,
            _ => 1.0f
        };
        if (herb == Herb.Catmint && season == Season.LeafBare)
            multiplier *= CatmintLeafBareMultiplier;
        return multiplier;
    }

    public static bool IsContagious(ConditionType type) {
        return type is ConditionType.Whitecough or ConditionType.Greencough;
    }

    /// <summary>Chance per day that a healthy cat catches the sickness on its own, before the season.</summary>
    public static float SicknessChancePerDay(ConditionType type) {
        return type switch {
            ConditionType.Whitecough => WhitecoughChancePerDay,
            ConditionType.Greencough => GreencoughChancePerDay,
            _ => 0f
        };
    }

    /// <summary>Chance per day that a sick cat infects a cat in the same den, before the season.</summary>
    public static float ContagionPerDay(ConditionType type) {
        return type switch {
            ConditionType.Whitecough => WhitecoughContagionPerDay,
            ConditionType.Greencough => GreencoughContagionPerDay,
            _ => 0f
        };
    }

    /// <summary>How much worse it gets every day, for an average case. A plain wound doesn't get worse.</summary>
    public static float SeverityPerDay(ConditionType type) {
        return type switch {
            ConditionType.Whitecough => WhitecoughSeverityPerDay,
            ConditionType.Greencough => GreencoughSeverityPerDay,
            ConditionType.InfectedWound => InfectedWoundSeverityPerDay,
            _ => 0f
        };
    }

    public static float RecoveryPerDay(ConditionType type) {
        return type switch {
            ConditionType.Whitecough => WhitecoughRecoveryPerDay,
            ConditionType.Greencough => GreencoughRecoveryPerDay,
            ConditionType.Wound => WoundRecoveryPerDay,
            ConditionType.InfectedWound => InfectedWoundRecoveryPerDay,
            _ => 0f
        };
    }

    public static float SeasonConceptionMultiplier(Season season) {
        return season switch {
            Season.Newleaf => NewleafConceptionMultiplier,
            Season.Greenleaf => GreenleafConceptionMultiplier,
            Season.LeafFall => LeafFallConceptionMultiplier,
            Season.LeafBare => LeafBareConceptionMultiplier,
            _ => 1.0f
        };
    }
}
