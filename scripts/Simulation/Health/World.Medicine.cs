using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     The medicine part of the world: medicine cats are chosen and trained, and healers care for the sick and injured.
/// </summary>
public partial class World {
	/// <summary>
	///     The healing skill of the care each cat got this tick. Care speeds up recovery and protects wounds from
	///     infection. Usually the healer's skill, for a cat instructed by a sick medicine cat see
	///     <see cref="Tuning.InstructedHealingShare" />.
	/// </summary>
	private readonly Dictionary<Cat, float> _careSkill = new();

	/// <summary>How many moons in a row the Clan has been without a full medicine cat.</summary>
	private int _moonsWithoutMedicineCat;

	/// <summary>The day each missing herb was last logged, so the log says it once a day instead of every tick.</summary>
	private readonly Dictionary<Herb, int> _herbMissingLoggedOnDay = new();

	/// <summary>The cat's healing skill. Short for <c>cat.Skills[SkillType.Healing]</c>, which this file needs often.</summary>
	private static Skill Healing(Cat cat) {
		return cat.Skills[SkillType.Healing];
	}

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
	///     Once a moon: whether one of the kits at its apprentice ceremony becomes a medicine cat apprentice instead.
	///     The Clan wants <see cref="Tuning.TargetHealers" /> healers (so a medicine cat is trained before the old one
	///     dies), but never more than one apprentice at a time, because every healer is a cat that doesn't hunt.
	///     The kit with the best healing gift is chosen, if the gift is good enough
	///     (<see cref="Tuning.MedicineApprenticeMinGift" />). After <see cref="Tuning.MoonsUntilDesperateForHealer" />
	///     moons without a medicine cat, the Clan is desperate and takes its most promising kit, however small its gift.
	///     Returns the chosen kit, or null.
	/// </summary>
	private Cat ChooseMedicineApprentice() {
		var medicineCats = Clan.Cats.Count(c => c.Rank == Rank.MedicineCat);
		_moonsWithoutMedicineCat = medicineCats == 0 ? _moonsWithoutMedicineCat + 1 : 0;

		var hasApprentice = Clan.Cats.Any(c => c.Rank == Rank.MedicineCatApprentice);
		if (hasApprentice || medicineCats >= Tuning.TargetHealers)
			return null;

		// AgeOneMoon makes the kits one moon older before their ceremony
		var bestKit = Clan.Cats
			.Where(c => c.Rank == Rank.Kit && c.AgeMoons + 1 >= Tuning.ApprenticeAgeMoons)
			.OrderByDescending(c => Healing(c).Gift)
			.FirstOrDefault();
		if (bestKit == null)
			return null;

		return Healing(bestKit).Gift >= Tuning.MedicineApprenticeMinGift || IsDesperateForHealer ? bestKit : null;
	}

	private bool IsDesperateForHealer => _moonsWithoutMedicineCat >= Tuning.MoonsUntilDesperateForHealer;

	private void MakeMedicineApprentice(Cat kit) {
		var kitName = kit.Name;

		if (Healing(kit).Gift < Tuning.MedicineApprenticeMinGift) {
			var leader = Clan.Leader?.Name ?? "the Clan";
			Log($"{Clan.Name} has been without a medicine cat for {_moonsWithoutMedicineCat} moons. "
				+ $"Desperate, {leader} chooses {kitName}, although the kit shows little gift for healing.",
				LogLevel.Summary);
		}

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
		Log($"{apprenticeName} has become a medicine cat: {apprentice.Name} (healing {Healing(apprentice).Level:0}).",
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
		Healing(apprentice).Improve(Tuning.MedicineApprenticeHealingGainPerMoon * mentorFactor);
		mentor?.Skills[SkillType.Mentoring].Improve(Tuning.MedicineMentoringPracticePerMoon);
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

			// A cat that comes back hurt from a patrol was still out hunting this tick
			if (cat.InMedicineDen && cat.Activity == Activity.InCamp)
				cat.Activity = Activity.Resting;

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
			.OrderByDescending(c => Healing(c).Level)
			.FirstOrDefault();
	}

	/// <summary>
	///     During the day, the healers care for the sick and injured, the patients in the medicine den first, then
	///     the sickest. Each healer cares for up to <see cref="Tuning.PatientsPerHealer" /> cats per tick, the best
	///     healer for the sickest, and a sick healer cares for itself too. A medicine cat too sick to get up only cares
	///     for itself, and instructs another cat to care for the others (see <see cref="Tuning.InstructedHealingShare" />).
	///     Without any medicine cat, the Clan's best healer among the other cats helps, if it has learned enough.
	///     Healers get better with practice, and their patients learn a little too.
	/// </summary>
	private void GiveCare() {
		_careSkill.Clear();
		if (TimeOfDay == TimeOfDay.Night)
			return;

		var caregivers = ChooseCaregivers();

		foreach (var gatherer in ChooseGatherers(caregivers.Select(c => c.Cat).ToList()))
			if (caregivers.RemoveAll(c => c.Cat == gatherer) > 0)
				GatherHerbs(gatherer);

		var patients = new Queue<Cat>(Clan.Cats
			.Where(c => c.IsSick && !_careSkill.ContainsKey(c))
			.OrderByDescending(c => c.InMedicineDen)
			.ThenByDescending(c => c.TotalSeverity));

		foreach (var (caregiver, skill) in caregivers) {
			var cared = new List<Cat>();
			while (cared.Count < Tuning.PatientsPerHealer && patients.Count > 0) {
				var patient = patients.Dequeue();
				Care(caregiver, patient, skill);
				cared.Add(patient);
			}

			if (cared.Count == 0)
				continue;

			// A helper who was out on patrol cares for them when it is back, so it still counts as patrolling
			if (caregiver.Activity is Activity.InCamp or Activity.Sleeping)
				caregiver.Activity = Activity.Caring;
			Log($"{caregiver.Name} cared for {Names(cared)}.");
		}
	}

	/// <summary>
	///     The cats who care for the sick this tick and how well, best first. A sick healer cares for itself right
	///     away, so it isn't left out when the other patients keep it busy. Medicine cats who are too sick to get up
	///     pass their place on to an instructed helper.
	/// </summary>
	private List<(Cat Cat, float Skill)> ChooseCaregivers() {
		var caregivers = new List<(Cat Cat, float Skill)>();
		var medicineCats = Clan.Cats
			.Where(c => c.IsMedicineCat)
			.OrderByDescending(c => Healing(c).Level)
			.ToList();

		foreach (var healer in medicineCats) {
			if (healer.IsSick)
				Care(healer, healer, Healing(healer).Level);

			if (!healer.IsConfined) {
				caregivers.Add((healer, Healing(healer).Level));
				continue;
			}

			// Too sick to get up: it tells another cat what to do for the others
			var helper = Clan.Cats
				.Where(c => !c.IsMedicineCat && c.Rank != Rank.Kit && !c.IsSick && caregivers.All(g => g.Cat != c))
				.OrderByDescending(c => Healing(c).Level)
				.ThenByDescending(c => Healing(c).Gift)
				.FirstOrDefault();
			if (helper == null)
				continue;

			caregivers.Add((helper, Math.Max(Healing(helper).Level, Healing(healer).Level * Tuning.InstructedHealingShare)));
			Log($"{healer.Name} is too sick to get up and tells {helper.Name} how to care for the sick.");
		}

		if (medicineCats.Count == 0) {
			var bestHealer = Clan.Cats
				.Where(c => c.Rank != Rank.Kit && !c.IsConfined && Healing(c).Level >= Tuning.MinHealingToCare)
				.OrderByDescending(c => Healing(c).Level)
				.FirstOrDefault();
			if (bestHealer != null) {
				if (bestHealer.IsSick)
					Care(bestHealer, bestHealer, Healing(bestHealer).Level);
				caregivers.Add((bestHealer, Healing(bestHealer).Level));
			}
		}

		return caregivers.OrderByDescending(c => c.Skill).ToList();
	}

	/// <summary>
	///     One cat cares for another (or for itself): the patient gets herbs and care of the given skill.
	///     The caregiver gets better with practice, a cat who isn't a medicine cat only up to its healing gift.
	///     The patient learns a little by watching.
	/// </summary>
	private void Care(Cat caregiver, Cat patient, float skill) {
		_careSkill[patient] = skill;
		GiveHerbs(caregiver, patient);

		var caregiverCap = caregiver.IsMedicineCat ? Tuning.SkillMax : Healing(caregiver).Gift;
		Healing(caregiver).Improve(Tuning.HealingPracticePerCare, caregiverCap);

		if (patient != caregiver && !patient.IsMedicineCat)
			Healing(patient).Improve(Tuning.PatientHealingGainPerCare,
				Healing(patient).Gift * Tuning.PatientHealingCapShare);
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
		gatherer.Activity = Activity.GatheringHerbs;
		Healing(gatherer).Practice();
		var skill = 0.5f + 0.5f * Healing(gatherer).Level / Tuning.SkillMax;
		foreach (var herb in Enum.GetValues<Herb>()) {
			var doses = (float)_random.NextDouble() * 2f * Tuning.HerbsPerTrip
						* Tuning.HerbSeasonMultiplier(herb, Season) * skill;
			Clan.HerbStore.Add(herb, doses);
		}

		Log($"{gatherer.Name} went gathering herbs. Store: {Clan.HerbStore}");
	}

	/// <summary>
	///     Gives the patient one dose of the right herb for each of its conditions, once a day, while the store has
	///     some. Conditions that are already healing don't need herbs anymore. The healers try again every tick, in
	///     case someone has gathered more, but a missing herb is only logged once a day.
	/// </summary>
	private void GiveHerbs(Cat caregiver, Cat patient) {
		foreach (var condition in patient.Conditions) {
			if (condition.IsHealing || condition.HerbGivenOnDay == Day)
				continue;

			var herb = Tuning.HerbFor(condition.Type);
			if (!Clan.HerbStore.TryUse(herb)) {
				if (_herbMissingLoggedOnDay.GetValueOrDefault(herb) != Day) {
					_herbMissingLoggedOnDay[herb] = Day;
					Log($"There is no {HerbName(herb)} left for {patient.Name}'s {condition.Name}.");
				}

				continue;
			}

			condition.HerbGivenOnDay = Day;
			if (Clan.HerbStore.Doses(herb) < 1f)
				Log($"{caregiver.Name} has used the last {HerbName(herb)}.", LogLevel.Summary);
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
		return _careSkill.TryGetValue(cat, out var skill)
			? 1f + Tuning.CareRecoveryBonus * skill / Tuning.SkillMax
			: 1f;
	}

	/// <summary>How much less likely a wound gets infected this tick thanks to care, 1 = no care.</summary>
	private float InfectionCareFactor(Cat cat) {
		return _careSkill.TryGetValue(cat, out var skill)
			? 1f - Tuning.CareInfectionReduction * skill / Tuning.SkillMax
			: 1f;
	}

	private string MedicineSummary() {
		var healers = Clan.Cats.Where(c => c.IsMedicineCat).Select(c => $"{c.Name} {Healing(c).Level:0}").ToList();
		return $"healers: {(healers.Count > 0 ? string.Join(", ", healers) : "none")}, herbs: {Clan.HerbStore}";
	}
}
