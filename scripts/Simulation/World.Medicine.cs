using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     The medicine part of the world: medicine cats are chosen and trained, and healers care for the sick and injured.
/// </summary>
public partial class World {
    /// <summary>Who cared for which cat this tick. Care speeds up recovery and protects wounds from infection.</summary>
    private readonly Dictionary<Cat, Cat> _caredForBy = new();

    /// <summary>
    ///     Like the other gifts, half of the healing gift comes from the parents. But the luck part is rare
    ///     (see <see cref="Tuning.HealingGiftRarity" />), so most kits have little gift for healing.
    /// </summary>
    private int RollHealingGift(int motherGift, int? fatherGift) {
        var parents = fatherGift.HasValue ? (motherGift + fatherGift.Value) / 2f : motherGift;
        var luck = Tuning.SkillMax * MathF.Pow((float)_random.NextDouble(), Tuning.HealingGiftRarity);
        var gift = luck + Tuning.GiftInheritance * (parents - Tuning.AverageHealingGift);
        return Math.Clamp((int)MathF.Round(gift), Tuning.SkillMin, Tuning.SkillMax);
    }

    /// <summary>
    ///     Whether one of the kits that become apprentices this moon should become a medicine cat apprentice instead:
    ///     only if the Clan has no medicine cat apprentice yet, and every medicine cat is getting old or there is none.
    ///     A Clan needs only one medicine cat and one apprentice, every extra one is a cat that doesn't hunt.
    ///     Then the kit with the best healing gift is chosen, if its gift is good enough
    ///     (<see cref="Tuning.MedicineApprenticeMinGift" />). Without any medicine cat, the Clan takes the best it has.
    ///     Returns the chosen kit, or null.
    /// </summary>
    private Cat ChooseMedicineApprentice() {
        if (Clan.Cats.Any(c => c.Rank == Rank.MedicineCatApprentice))
            return null;

        var medicineCats = Clan.Cats.Where(c => c.Rank == Rank.MedicineCat).ToList();
        var noMedicineCat = medicineCats.Count == 0;
        if (medicineCats.Any(m => m.AgeMoons < Tuning.SeekMedicineApprenticeAgeMoons))
            return null;

        // AgeOneMoon makes the kits one moon older before their ceremony
        var best = Clan.Cats
            .Where(c => c.Rank == Rank.Kit && c.AgeMoons + 1 >= Tuning.ApprenticeAgeMoons)
            .OrderByDescending(c => c.HealingGift)
            .FirstOrDefault();

        if (best == null || (!noMedicineCat && best.HealingGift < Tuning.MedicineApprenticeMinGift))
            return null;
        return best;
    }

    private void MakeMedicineApprentice(Cat kit) {
        var kitName = kit.Name;
        kit.ChangeRank(Rank.MedicineCatApprentice);

        var mentor = BestMedicineCat();
        if (mentor != null)
            kit.SetMentor(mentor);

        var mentorText = mentor != null
            ? $"{mentor.Name} will teach the ways of healing."
            : "There is no medicine cat left to teach them.";
        Log($"{kitName} has become a medicine cat apprentice: {kit.Name}. {mentorText}", LogLevel.Summary);
    }

    private void MakeMedicineCat(Cat apprentice) {
        var apprenticeName = apprentice.Name;
        var otherNames = Clan.Cats.Where(c => c != apprentice).Select(c => c.Name);
        apprentice.ChangeRank(Rank.MedicineCat,
            CatNames.RandomWarriorSuffix(apprentice.Prefix, otherNames, _random));
        Log($"{apprenticeName} has become a medicine cat: {apprentice.Name} (healing {apprentice.Healing}).",
            LogLevel.Summary);
    }

    /// <summary>
    ///     A medicine cat apprentice learns every moon, like a warrior apprentice, but healing instead of hunting.
    ///     If its mentor has died, another medicine cat takes over. Without one, it learns a little on its own.
    /// </summary>
    private void TrainMedicineApprentice(Cat apprentice) {
        var mentor = apprentice.Mentor;
        if (mentor == null || mentor.Rank != Rank.MedicineCat || !Clan.Cats.Contains(mentor)) {
            mentor = BestMedicineCat();
            if (mentor != null) {
                apprentice.SetMentor(mentor);
                Log($"{mentor.Name} is {apprentice.Name}'s new mentor.", LogLevel.Summary);
            }
        }

        var mentorFactor = mentor?.MentorFactor ?? Tuning.NoMentorFactor;
        apprentice.ImproveHealing(
            Tuning.MedicineApprenticeHealingGainPerMoon * apprentice.HealingLearningFactor * mentorFactor);
        mentor?.MentorOneMoon();
    }

    /// <summary>
    ///     The medicine cat decides who gets a nest in the medicine den: the sickest cats first, from
    ///     <see cref="Tuning.MedicineDenMinSeverity" /> on. Patients stay until they are almost healthy. When the den is
    ///     full, a sicker cat takes the nest of a less sick one, who has to recover in its own den. Without a medicine
    ///     cat, nobody looks after the den.
    /// </summary>
    private void AssignMedicineDen() {
        var hasMedicineCat = Clan.Cats.Any(c => c.IsMedicineCat);
        var wanted = !hasMedicineCat
            ? new HashSet<Cat>()
            : Clan.Cats
                .Where(c => !c.IsMedicineCat && NeedsNest(c))
                .OrderByDescending(c => c.TotalSeverity)
                .Take(Tuning.MedicineDenNests)
                .ToHashSet();

        foreach (var cat in Clan.Cats) {
            var wasInDen = cat.InMedicineDen;
            cat.InMedicineDen = wanted.Contains(cat);

            if (cat.InMedicineDen && !wasInDen)
                Log($"{cat} has been taken into the medicine den.");
            else if (!cat.InMedicineDen && wasInDen && NeedsNest(cat))
                Log($"The medicine den is full. {cat} has to make room for a sicker cat.");
        }
    }

    private static bool NeedsNest(Cat cat) {
        var minSeverity = cat.InMedicineDen ? Tuning.MedicineDenLeaveSeverity : Tuning.MedicineDenMinSeverity;
        return cat.IsSick && cat.TotalSeverity >= minSeverity;
    }

    /// <summary>Patients in the medicine den rest and recover faster, 1 = not in the den.</summary>
    private static float MedicineDenFactor(Cat cat) {
        return cat.InMedicineDen ? 1f + Tuning.MedicineDenRecoveryBonus : 1f;
    }

    private Cat BestMedicineCat() {
        return Clan.Cats
            .Where(c => c.Rank == Rank.MedicineCat)
            .OrderByDescending(c => c.Healing)
            .FirstOrDefault();
    }

    /// <summary>
    ///     During the day, the healers care for the sick and injured, the patients in the medicine den first, then
    ///     the sickest. Each healer cares for up to
    ///     <see cref="Tuning.PatientsPerHealer" /> cats per tick, the best healer for the sickest. The healers are the
    ///     medicine cats and their apprentices. Without them, the Clan's best healer among the other cats helps, if it
    ///     has learned enough. Healers get better with practice, and their patients learn a little too.
    /// </summary>
    private void GiveCare() {
        _caredForBy.Clear();
        if (TimeOfDay == TimeOfDay.Night)
            return;

        var healers = Clan.Cats
            .Where(c => c.IsMedicineCat && !c.IsConfined)
            .OrderByDescending(c => c.Healing)
            .ToList();
        if (healers.Count == 0) {
            var bestHealer = Clan.Cats
                .Where(c => c.Rank != Rank.Kit && !c.IsConfined && c.Healing >= Tuning.MinHealingToCare)
                .OrderByDescending(c => c.Healing)
                .FirstOrDefault();
            if (bestHealer != null)
                healers.Add(bestHealer);
        }

        foreach (var gatherer in ChooseGatherers(healers)) {
            healers.Remove(gatherer);
            GatherHerbs(gatherer);
        }

        var patients = new Queue<Cat>(Clan.Cats
            .Where(c => c.IsSick)
            .OrderByDescending(c => c.InMedicineDen)
            .ThenByDescending(c => c.TotalSeverity));

        foreach (var healer in healers) {
            var cared = new List<Cat>();
            while (cared.Count < Tuning.PatientsPerHealer && patients.Count > 0) {
                var patient = patients.Dequeue();
                // A healer can't care for itself, the next healer can
                if (patient == healer)
                    continue;

                _caredForBy[patient] = healer;
                cared.Add(patient);
                GiveHerbs(patient);
                healer.ImproveHealing(Tuning.HealingPracticePerCare * healer.HealingLearningFactor);
                if (!patient.IsMedicineCat)
                    patient.ImproveHealing(Tuning.PatientHealingGainPerCare * patient.HealingLearningFactor,
                        (int)(patient.HealingGift * Tuning.PatientHealingCapShare));
            }

            if (cared.Count > 0)
                Log($"{healer.Name} cared for {Names(cared)}.");
        }
    }

    /// <summary>
    ///     When the store is low on any herb, the medicine cat apprentice goes gathering. The medicine cat only goes
    ///     itself if there's no apprentice and nobody is lying in the medicine den. Gatherers don't care for anyone
    ///     this tick. Cats who only learned a little healing never gather.
    /// </summary>
    private List<Cat> ChooseGatherers(List<Cat> healers) {
        var store = Clan.HerbStore;
        if (Enum.GetValues<Herb>().All(h => store.Doses(h) >= Tuning.HerbStoreTarget))
            return new List<Cat>();

        var apprentices = healers.Where(c => c.Rank == Rank.MedicineCatApprentice).ToList();
        if (apprentices.Count > 0)
            return apprentices;

        var anyPatients = Clan.Cats.Any(c => c.InMedicineDen);
        return anyPatients
            ? new List<Cat>()
            : healers.Where(c => c.Rank == Rank.MedicineCat).Take(1).ToList();
    }

    /// <summary>
    ///     A gathering trip finds some of every herb, depending on the season and on how well the gatherer knows
    ///     where to look (its healing skill).
    /// </summary>
    private void GatherHerbs(Cat gatherer) {
        var skill = 0.5f + 0.5f * gatherer.Healing / Tuning.SkillMax;
        foreach (var herb in Enum.GetValues<Herb>()) {
            var doses = (float)_random.NextDouble() * 2f * Tuning.HerbsPerTrip
                        * Tuning.HerbSeasonMultiplier(herb, Season) * skill;
            Clan.HerbStore.Add(herb, doses);
        }

        Log($"{gatherer.Name} went gathering herbs. Store: {Clan.HerbStore}");
    }

    /// <summary>
    ///     Gives the patient one dose of the right herb for each of its conditions, once a day, while the store has
    ///     some. Conditions that are already healing don't need herbs anymore.
    /// </summary>
    private void GiveHerbs(Cat patient) {
        foreach (var condition in patient.Conditions) {
            if (condition.IsHealing || condition.HerbGivenOnDay == Day)
                continue;

            var herb = Tuning.HerbFor(condition.Type);
            if (!Clan.HerbStore.TryUse(herb)) {
                Log($"There is no {HerbName(herb)} left for {patient.Name}'s {condition.Name}.");
                continue;
            }

            condition.HerbGivenOnDay = Day;
            if (Clan.HerbStore.Doses(herb) < 1f)
                Log($"The medicine cat has used the last {HerbName(herb)}.", LogLevel.Summary);
        }
    }

    /// <summary>How much faster the condition heals this tick thanks to herbs, 1 = no herb today.</summary>
    private float HerbFactor(Condition condition) {
        return condition.HerbGivenOnDay == Day ? 1f + Tuning.HerbRecoveryBonus : 1f;
    }

    private static string HerbName(Herb herb) {
        return herb == Herb.Cobweb ? "cobwebs" : herb.ToString().ToLowerInvariant();
    }

    /// <summary>How much faster a cat recovers this tick thanks to care, 1 = no care.</summary>
    private float CareFactor(Cat cat) {
        return _caredForBy.TryGetValue(cat, out var healer)
            ? 1f + Tuning.CareRecoveryBonus * healer.Healing / Tuning.SkillMax
            : 1f;
    }

    /// <summary>How much less likely a wound gets infected this tick thanks to care, 1 = no care.</summary>
    private float InfectionCareFactor(Cat cat) {
        return _caredForBy.TryGetValue(cat, out var healer)
            ? 1f - Tuning.CareInfectionReduction * healer.Healing / Tuning.SkillMax
            : 1f;
    }

    private string MedicineSummary() {
        var healers = Clan.Cats.Where(c => c.IsMedicineCat).Select(c => $"{c.Name} {c.Healing}").ToList();
        return $"healers: {(healers.Count > 0 ? string.Join(", ", healers) : "none")}, herbs: {Clan.HerbStore}";
    }
}
