using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     The health part of the world: cats catch sicknesses, infect each other, get hurt while hunting,
///     and recover or die.
/// </summary>
public partial class World {
	private static readonly ConditionType[] Sicknesses = { ConditionType.Whitecough, ConditionType.Greencough };

	/// <summary>What can happen to a cat while hunting, for the log.</summary>
	private static readonly string[] HuntingInjuries = {
		"a sprained paw", "a thorn in the paw", "a bad fall", "a torn claw", "a wrenched shoulder"
	};

	// Statistics for the moon summary
	private readonly Dictionary<ConditionType, int> _casesThisMoon = new();
	private int _sicknessDeathsThisMoon;

	/// <summary>
	///     Runs every tick, after eating: the medicine cat fills the medicine den, the healers care for the sick,
	///     sicknesses spread and start, whitecough and wounds can get worse, and every sickness and injury runs its course.
	/// </summary>
	private void UpdateHealth() {
		AssignMedicineDen();
		GiveCare();
		SpreadSicknesses();
		CatchSicknesses();
		WorsenWhitecough();
		InfectWounds();
		ProgressConditions();

		if (TimeOfDay == TimeOfDay.Night)
			Clan.HerbStore.Spoil(Tuning.HerbSpoilPerDay);
	}

	/// <summary>
	///     A cat on a hunting patrol can get hurt. Tired cats get hurt more often, and lazy cats are more careless.
	///     How bad it is is rolled between <see cref="Tuning.InjuryMinSeverity" /> and
	///     <see cref="Tuning.InjuryMaxSeverity" />.
	/// </summary>
	private void RollHuntingInjury(Cat cat) {
		var tiredness = 2f - cat.EnergyFactor;
		var carelessness = 1.5f - (float)cat.Diligence / Tuning.SkillMax;
		if (_random.NextDouble() >= Tuning.InjuryChancePerPatrol * tiredness * carelessness)
			return;

		var severity = Tuning.InjuryMinSeverity
					   + (float)_random.NextDouble() * (Tuning.InjuryMaxSeverity - Tuning.InjuryMinSeverity);
		var injury = HuntingInjuries[_random.Next(HuntingInjuries.Length)];
		cat.AddCondition(new Condition(ConditionType.Wound, severity, 1f, description: injury));

		_casesThisMoon[ConditionType.Wound] = _casesThisMoon.GetValueOrDefault(ConditionType.Wound) + 1;
		var confined = cat.IsConfined ? " Too badly hurt to hunt." : "";
		Log($"{cat} was hurt while hunting: {injury}.{confined}");
	}

	/// <summary>
	///     A wound that hasn't closed yet can get infected, more often in cats weak from hunger and less often when a
	///     healer cares for it, especially with cobwebs. The infected wound keeps how bad the wound was and how far it has healed.
	/// </summary>
	private void InfectWounds() {
		foreach (var cat in Clan.Cats) {
			var wound = cat.Conditions.FirstOrDefault(c => c.Type == ConditionType.Wound && !c.IsHealing);
			if (wound == null)
				continue;

			var cobwebs = wound.HerbGivenOnDay == Day ? 1f - Tuning.CobwebInfectionReduction : 1f;
			var chance = Tuning.WoundInfectionPerDay * (cat.IsWeakFromHunger ? 2f : 1f) * InfectionCareFactor(cat)
						 * cobwebs / Tuning.TicksPerDay;
			if (_random.NextDouble() >= chance)
				continue;

			cat.ReplaceCondition(wound,
				new Condition(ConditionType.InfectedWound, wound.Severity, RollCaseSpeed(), wound.Recovery));
			_casesThisMoon[ConditionType.InfectedWound] =
				_casesThisMoon.GetValueOrDefault(ConditionType.InfectedWound) + 1;
			Log($"{cat.Name}'s wound ({wound.Name}) has become infected!", LogLevel.Summary);
		}
	}

	/// <summary>
	///     Every sick cat can infect the cats it shares a den with, and less often the cats in other dens.
	///     The more sick cats there are, the more likely a healthy cat catches it.
	///     Cats who are over the worst don't infect others anymore.
	/// </summary>
	private void SpreadSicknesses() {
		foreach (var type in Sicknesses) {
			var sick = Clan.Cats.Where(c => c.Spreads(type)).ToList();
			if (sick.Count == 0)
				continue;

			var chancePerSickCat = Tuning.ContagionPerDay(type) * Tuning.SeasonSicknessMultiplier(Season)
								   / Tuning.TicksPerDay;

			foreach (var cat in Clan.Cats.Where(c => CanCatch(c, type)).ToList()) {
				var sickInSameDen = sick.Where(s => s.Den == cat.Den).ToList();
				var exposure = sickInSameDen.Count + (sick.Count - sickInSameDen.Count) * Tuning.OtherDenContagionShare;
				// The chance that at least one of the sick cats infects this cat
				var chance = 1f - MathF.Pow(1f - chancePerSickCat, exposure);
				if (_random.NextDouble() >= chance)
					continue;

				var source = sickInSameDen.Count > 0 ? sickInSameDen : sick;
				GetSick(cat, type, source[_random.Next(source.Count)]);
			}
		}
	}

	/// <summary>Now and then a cat gets sick on its own, more often in the cold seasons.</summary>
	private void CatchSicknesses() {
		foreach (var type in Sicknesses) {
			var chance = Tuning.SicknessChancePerDay(type) * Tuning.SeasonSicknessMultiplier(Season)
						 / Tuning.TicksPerDay;

			foreach (var cat in Clan.Cats.Where(c => CanCatch(c, type)).ToList())
				if (_random.NextDouble() < chance)
					GetSick(cat, type, null);
		}
	}

	/// <summary>A cat with greencough doesn't catch whitecough on top of it.</summary>
	private bool CanCatch(Cat cat, ConditionType type) {
		if (type == ConditionType.Whitecough && cat.HasCondition(ConditionType.Greencough))
			return false;
		return cat.CanCatch(type, Day);
	}

	/// <param name="source">The cat it caught the sickness from, or null if it got sick on its own.</param>
	private void GetSick(Cat cat, ConditionType type, Cat source) {
		var name = Condition.NameOf(type);
		var from = source != null ? $" from {source.Name}" : "";

		// Greencough takes the place of a whitecough the cat is still fighting. A whitecough that is already healing
		// stays, and the greencough starts fresh next to it.
		var whitecough = cat.Conditions.FirstOrDefault(c => c.Type == ConditionType.Whitecough && !c.IsHealing);
		if (type == ConditionType.Greencough && whitecough != null)
			cat.ReplaceCondition(whitecough, Worsen(whitecough));
		else
			cat.AddCondition(new Condition(type, 0f, RollCaseSpeed()));

		_casesThisMoon[type] = _casesThisMoon.GetValueOrDefault(type) + 1;
		Log($"{cat} has caught {name}{from}.",
			type == ConditionType.Greencough ? LogLevel.Summary : LogLevel.Detail);
	}

	/// <summary>
	///     Whitecough can turn into greencough, especially in the cold seasons and in weak cats
	///     (frail because of their age, or weak from hunger). Not once the cat is getting better.
	/// </summary>
	private void WorsenWhitecough() {
		var seasonChance = Tuning.GreencoughFromWhitecoughPerDay * Tuning.SeasonSicknessMultiplier(Season)
						   / Tuning.TicksPerDay;

		foreach (var cat in Clan.Cats) {
			var whitecough = cat.Conditions.FirstOrDefault(c => c.Type == ConditionType.Whitecough && !c.IsHealing);
			if (whitecough == null)
				continue;

			// A cat weak from hunger counts as fully frail here
			var weakness = cat.IsWeakFromHunger
				? Tuning.WeakGreencoughMultiplier
				: cat.FrailtyMultiplier(Tuning.WeakGreencoughMultiplier);
			var chance = seasonChance * weakness;
			if (_random.NextDouble() >= chance)
				continue;

			cat.ReplaceCondition(whitecough, Worsen(whitecough));
			_casesThisMoon[ConditionType.Greencough] = _casesThisMoon.GetValueOrDefault(ConditionType.Greencough) + 1;
			Log($"{cat.Name}'s whitecough has turned into greencough!", LogLevel.Summary);
		}
	}

	/// <summary>
	///     Turns whitecough into greencough. The cat keeps how sick it is and how far its body has already
	///     fought the cough, but greencough gets worse faster.
	/// </summary>
	private Condition Worsen(Condition whitecough) {
		return new Condition(ConditionType.Greencough, whitecough.Severity, RollCaseSpeed(), whitecough.Recovery);
	}

	/// <summary>Every sickness gets worse or better. A cat recovers, or dies when a sickness becomes deadly.</summary>
	private void ProgressConditions() {
		// ToList() makes copies, because cats die and conditions are removed while looping
		foreach (var cat in Clan.Cats.ToList()) {
			foreach (var condition in cat.Conditions.ToList()) {
				var type = condition.Type;
				condition.Progress(
					Tuning.SeverityPerDay(type) / Tuning.TicksPerDay,
					Tuning.RecoveryPerDay(type) * RecoveryMultiplier(cat) * HerbFactor(condition) / Tuning.TicksPerDay,
					Tuning.HealingPerDay / Tuning.TicksPerDay);

				if (condition.IsDeadly) {
					Clan.RecordDeath(cat);
					_deathsThisMoon++;
					_sicknessDeathsThisMoon++;
					Log($"{cat} died of {condition.Name}. {Clan.Cats.Count} cats are left.", LogLevel.Summary);
					break;
				}

				if (condition.IsCured) {
					// Only sicknesses make a cat immune for a while, a healed wound doesn't
					var immuneUntilDay = Tuning.IsContagious(type) ? Day + Tuning.ImmunityDays : Day;
					cat.RemoveCondition(condition, immuneUntilDay);
					var important = type is ConditionType.Greencough or ConditionType.InfectedWound;
					Log($"{cat} has recovered from {condition.Name}.", important ? LogLevel.Summary : LogLevel.Detail);
				}
			}
		}
	}

	/// <summary>
	///     How well the cat fights sickness, compared to a healthy adult (1).
	///     Frail cats and cats weak from hunger fight worse, care from a healer and rest in the medicine den help.
	/// </summary>
	private float RecoveryMultiplier(Cat cat) {
		var multiplier = CareFactor(cat) * MedicineDenFactor(cat);
		multiplier *= cat.FrailtyMultiplier(Tuning.FrailRecoveryMultiplier);
		if (cat.IsWeakFromHunger)
			multiplier *= Tuning.StarvingRecoveryMultiplier;
		return multiplier;
	}

	/// <summary>Some cases are worse than others, see <see cref="Tuning.CaseSpeedVariation" />.</summary>
	private float RollCaseSpeed() {
		return 1f + ((float)_random.NextDouble() * 2f - 1f) * Tuning.CaseSpeedVariation;
	}

	private string HealthSummary() {
		var cases = string.Join(", ", Enum.GetValues<ConditionType>()
			.Select(t => $"{t}: {_casesThisMoon.GetValueOrDefault(t)}"));
		return $"new cases ({cases}), died of sickness or injury: {_sicknessDeathsThisMoon}, "
			   + $"sick or injured now: {Clan.Cats.Count(c => c.IsSick)}, {MedicineSummary()}";
	}

	private void ResetHealthStatistics() {
		_casesThisMoon.Clear();
		_sicknessDeathsThisMoon = 0;
	}
}
