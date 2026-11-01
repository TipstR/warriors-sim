using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using WarriorsSim.Simulation;

namespace WarriorsSim.BalanceRunner;

/// <summary>What happened in one seed's run. Skill numbers only count cats born during the run, no medicine cats.</summary>
public class SeedResult {
	public bool DiedOut;
	public int Starved;

	/// <summary>Deaths from sickness or injury (not old age, not starvation).</summary>
	public int SicknessDeaths;

	public readonly List<float> CeremonyHunting = new();

	/// <summary>The age of every apprentice at its warrior ceremony, in moons.</summary>
	public readonly List<float> CeremonyAge = new();

	/// <summary>The oldest any apprentice got before its ceremony (or death).</summary>
	public int OldestApprentice;

	/// <summary>Apprentices who died after the warrior age, before they got their warrior name.</summary>
	public int DiedAsLateApprentice;
	public readonly List<float> HuntingAt48Moons = new();
	public readonly List<float> HuntingAt96Moons = new();

	/// <summary>Once per year: the hunting skill of all living cats aged 12 moons or more, lowest first.</summary>
	public readonly List<List<float>> LivingHuntersByYear = new();

	/// <summary>Once per year: the share of all mice caught by the best 20% of that year's hunters.</summary>
	public readonly List<float> Top20CatchShare = new();

	/// <summary>
	///     Once per year: how far below its peak each living cat's skill is, by group, e.g. "elders" → [3.5, 0, 12].
	///     Hunting for everyone except medicine cats, healing for medicine cats.
	/// </summary>
	public readonly Dictionary<string, List<float>> FadedBelowPeak = new();

	/// <summary>
	///     Every time a hunter (not an elder) got rusty: how many points it lost at most and how many moons it took
	///     until it was back at its peak. Times that hadn't ended when the run ended (or the cat died) don't count.
	/// </summary>
	public readonly List<(float Lost, int Moons)> RustyTimes = new();

	/// <summary>Training patrol lessons (one per apprentice and patrol), and how many of them a helper gave.</summary>
	public int Lessons;
	public int LessonsByHelpers;

	/// <summary>Moons spent as an apprentice, summed over all apprentices (to get lessons per apprentice and moon).</summary>
	public int ApprenticeMoons;

	/// <summary>Once per year: the mentoring skill of every cat that has an apprentice right now.</summary>
	public readonly List<float> MentorsMentoring = new();

	/// <summary>Once per year: the mentoring skill of every warrior, deputy and leader.</summary>
	public readonly List<float> WarriorsMentoring = new();

	public int Adults;
	public int Reached90;
	public int Reached95;

	/// <summary>Fresh-kill: portions caught and rotted into crowfood, meals (all, and of stale prey).</summary>
	public long PortionsCaught;
	public long Crowfood;
	public int Meals;
	public int StaleMeals;

	public int HuntsAlone;
	public long PortionsCaughtAlone;

	/// <summary>How many pieces of each kind of prey were caught.</summary>
	public readonly Dictionary<PreyKind, int> CaughtByKind = new();

	/// <summary>Cats hunting alone, by hunting skill (under 45, 45–70, 70+): trips and the portions they brought.</summary>
	public readonly int[] TripsBySkill = new int[3];
	public readonly float[] PortionsBySkill = new float[3];

	/// <summary>
	///     By season, daytime ticks only: summed over the ticks, how many cats with a fun need (warriors, deputy,
	///     leader) were idle in camp, how many there were, and their fun. DayTicks counts the ticks.
	/// </summary>
	public readonly long[] IdleWarriors = new long[4];
	public readonly long[] Warriors = new long[4];
	public readonly double[] WarriorFun = new double[4];
	public readonly int[] DayTicks = new int[4];
}

/// <summary>Runs ThunderClan with one seed and collects statistics from the log and from the cats.</summary>
public static class SeedRun {
	private static readonly Regex PatrolLine = new(@"Hunting patrol caught [^(]* \((.*?)\)\. Pile");
	private static readonly Regex CeremonyLine = new(@"has become a warrior: .*\(hunting (\d+)");

	private static readonly Regex MoonPreyLine = new(@"portions caught: (\d+), crowfood thrown out: (\d+) portions, "
													  + @"stale meals: (\d+), hunts alone: (\d+) \((\d+) portions\)");

	private static readonly Regex HuntAloneLine = new(@"\| (.+) went hunting alone and caught [^(]* \(([\d.]+) portions\)");
	private static int TicksPerMoon => Tuning.TicksPerDay * Tuning.DaysPerMoon;
	private static int TicksPerYear => TicksPerMoon * 12;

	public static SeedResult Run(int seed, int years) {
		var result = new SeedResult();
		var catchesThisYear = new Dictionary<string, float>();
		var clan = StartingClans.CreateThunderClan();
		// The starting cats got their skills by hand, so they don't say anything about learning
		var startingCats = clan.Cats.ToHashSet();
		var seenAt48 = new HashSet<Cat>();
		var seenAt96 = new HashSet<Cat>();
		var rusty = new Dictionary<Cat, (float Lost, int Moons)>();
		var apprentices = new HashSet<Cat>();

		var world = new World(clan, seed, (_, line) => ReadLogLine(line, result, catchesThisYear));
		world.CatAte += _ => result.Meals++;
		world.PatrolSent += patrol => CountCatch(patrol, result);

		for (var tick = 1; tick <= TicksPerYear * years; tick++) {
			// The season and time of the tick that is about to run
			var season = (int)world.Season;
			var isDay = world.TimeOfDay != TimeOfDay.Night;
			world.Tick();
			if (clan.Cats.Count == 0) {
				result.DiedOut = true;
				break;
			}

			if (isDay)
				CountIdleWarriors(clan, season, result);

			if (tick % TicksPerMoon == 0) {
				foreach (var cat in clan.Cats.Where(c => !startingCats.Contains(c) && !c.IsMedicineCat)) {
					var hunting = cat.Skills[SkillType.Hunting].Level;
					if (cat.AgeMoons >= 48 && seenAt48.Add(cat))
						result.HuntingAt48Moons.Add(hunting);
					if (cat.AgeMoons >= 96 && seenAt96.Add(cat))
						result.HuntingAt96Moons.Add(hunting);
				}

				TrackRustyHunters(clan, rusty, result);
				result.ApprenticeMoons += clan.Cats.Count(c => c.Rank == Rank.Apprentice);
				TrackCeremonies(clan, apprentices, result);
			}

			if (tick % TicksPerYear == 0) {
				EndYear(clan, result, catchesThisYear);
				catchesThisYear.Clear();
			}
		}

		// The best level each cat ever had, because skills can fade
		foreach (var cat in clan.Dead.Concat(clan.Cats)) {
			if (startingCats.Contains(cat) || cat.IsMedicineCat || cat.AgeMoons < 12)
				continue;
			var hunting = cat.Skills[SkillType.Hunting].Peak;
			result.Adults++;
			if (hunting >= 90)
				result.Reached90++;
			if (hunting >= 95)
				result.Reached95++;
		}

		return result;
	}

	private static void CountIdleWarriors(Clan clan, int season, SeedResult result) {
		result.DayTicks[season]++;
		foreach (var cat in clan.Cats.Where(c => c.HasFunNeed)) {
			result.Warriors[season]++;
			result.WarriorFun[season] += cat.Fun;
			if (cat.Activity == Activity.InCamp)
				result.IdleWarriors[season]++;
		}
	}

	private static void CountCatch(Patrol patrol, SeedResult result) {
		foreach (var prey in patrol.Caught)
			result.CaughtByKind[prey.Kind] = result.CaughtByKind.GetValueOrDefault(prey.Kind) + 1;

		// A patrol of one is a cat hunting alone (or a leftover cat the leader sent; rare)
		if (patrol.Cats.Count != 1 || patrol.IsTraining)
			return;
		var hunting = patrol.Cats[0].Skills[SkillType.Hunting].Level;
		var band = hunting < 45 ? 0 : hunting < 70 ? 1 : 2;
		result.TripsBySkill[band]++;
		result.PortionsBySkill[band] += patrol.PortionsCaught;
	}

	private static void ReadLogLine(string line, SeedResult result, Dictionary<string, float> catchesThisYear) {
		if (line.Contains("cats are left.")) {
			if (line.Contains("starved to death"))
				result.Starved++;
			else if (line.Contains("died of") && !line.Contains("died of old age"))
				result.SicknessDeaths++;
			return;
		}

		// "Training patrol: Graypaw with Lionheart, Ravenpaw with Longtail (helping out)."
		if (line.Contains("| Training patrol: ")) {
			var pairs = line[(line.IndexOf("Training patrol: ") + "Training patrol: ".Length)..].Split(", ");
			result.Lessons += pairs.Length;
			result.LessonsByHelpers += pairs.Count(p => p.Contains("(helping out)"));
			return;
		}

		var moonPrey = MoonPreyLine.Match(line);
		if (moonPrey.Success) {
			result.PortionsCaught += int.Parse(moonPrey.Groups[1].Value);
			result.Crowfood += int.Parse(moonPrey.Groups[2].Value);
			result.StaleMeals += int.Parse(moonPrey.Groups[3].Value);
			result.HuntsAlone += int.Parse(moonPrey.Groups[4].Value);
			result.PortionsCaughtAlone += int.Parse(moonPrey.Groups[5].Value);
			return;
		}

		// Catches of cats hunting alone count for the best hunters' share too
		var alone = HuntAloneLine.Match(line);
		if (alone.Success) {
			var name = alone.Groups[1].Value;
			catchesThisYear[name] = catchesThisYear.GetValueOrDefault(name)
									+ float.Parse(alone.Groups[2].Value, CultureInfo.InvariantCulture);
			return;
		}

		var ceremony = CeremonyLine.Match(line);
		if (ceremony.Success) {
			result.CeremonyHunting.Add(int.Parse(ceremony.Groups[1].Value));
			return;
		}

		// "(Graypaw 2, Sandstorm 4.5)", in portions. Names change at ceremonies, so a cat may count as two hunters
		// that year.
		var patrol = PatrolLine.Match(line);
		if (!patrol.Success)
			return;
		foreach (var part in patrol.Groups[1].Value.Split(", ")) {
			var space = part.LastIndexOf(' ');
			var name = part[..space];
			catchesThisYear[name] = catchesThisYear.GetValueOrDefault(name)
									+ float.Parse(part[(space + 1)..], CultureInfo.InvariantCulture);
		}
	}

	private static void EndYear(Clan clan, SeedResult result, Dictionary<string, float> catchesThisYear) {
		if (catchesThisYear.Count >= 5) {
			var catches = catchesThisYear.Values.OrderByDescending(n => n).ToList();
			var best = Math.Max(1, catches.Count / 5);
			result.Top20CatchShare.Add(catches.Take(best).Sum() / catches.Sum());
		}

		var hunters = clan.Cats
			.Where(c => !c.IsMedicineCat && c.AgeMoons >= 12)
			.Select(c => c.Skills[SkillType.Hunting].Level)
			.OrderBy(level => level)
			.ToList();
		if (hunters.Count >= 5)
			result.LivingHuntersByYear.Add(hunters);

		foreach (var cat in clan.Cats.Where(c => c.Rank is Rank.Warrior or Rank.Deputy or Rank.Leader)) {
			var mentoring = cat.Skills[SkillType.Mentoring].Level;
			result.WarriorsMentoring.Add(mentoring);
			if (clan.ApprenticeOf(cat) != null)
				result.MentorsMentoring.Add(mentoring);
		}

		foreach (var cat in clan.Cats.Where(c => c.Rank != Rank.Kit)) {
			var skill = cat.Skills[cat.IsMedicineCat ? SkillType.Healing : SkillType.Hunting];
			var group = FadeGroup(cat);
			if (!result.FadedBelowPeak.TryGetValue(group, out var list))
				result.FadedBelowPeak[group] = list = new List<float>();
			list.Add(skill.Peak - skill.Level);
		}
	}

	/// <summary>Checked every moon: who became a warrior since the last moon, at what age, and who died before.</summary>
	private static void TrackCeremonies(Clan clan, HashSet<Cat> apprentices, SeedResult result) {
		foreach (var cat in apprentices.ToList()) {
			if (!clan.Cats.Contains(cat)) {
				if (cat.AgeMoons > Tuning.WarriorAgeMoons)
					result.DiedAsLateApprentice++;
				apprentices.Remove(cat);
			}
			else if (cat.Rank != Rank.Apprentice) {
				result.CeremonyAge.Add(cat.AgeMoons);
				apprentices.Remove(cat);
			}
		}

		foreach (var cat in clan.Cats.Where(c => c.Rank == Rank.Apprentice)) {
			apprentices.Add(cat);
			result.OldestApprentice = Math.Max(result.OldestApprentice, cat.AgeMoons);
		}
	}

	private static void TrackRustyHunters(Clan clan, Dictionary<Cat, (float Lost, int Moons)> rusty, SeedResult result) {
		foreach (var cat in clan.Cats.Where(c => !c.IsMedicineCat && c.Rank != Rank.Elder)) {
			var hunting = cat.Skills[SkillType.Hunting];
			var lost = hunting.Peak - hunting.Level;
			var wasRusty = rusty.TryGetValue(cat, out var time);

			if (lost >= 0.5f)
				rusty[cat] = (Math.Max(time.Lost, lost), time.Moons + 1);
			else if (wasRusty && lost < 0.1f) {
				result.RustyTimes.Add(time);
				rusty.Remove(cat);
			}
		}
	}

	private static string FadeGroup(Cat cat) {
		if (cat.IsMedicineCat)
			return "medicine cats (healing)";
		if (cat.IsNursing)
			return "nursing queens";
		return cat.Rank switch {
			Rank.Leader or Rank.Deputy => "leader and deputy",
			Rank.Elder => "elders",
			Rank.Apprentice => "apprentices",
			_ => "warriors"
		};
	}
}
