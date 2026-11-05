using System.Collections.Generic;
using System.Linq;
using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>
///     Bringing prey: cats who stay in their den (queens in the nursery, elders, the sick) get their piece brought.
///     Their mate first, then an apprentice, then anyone else who can. Nobody hunts in these tests.
/// </summary>
public class ChoreTests {
	private readonly List<(Chore Chore, Cat Carrier)> _done = new();
	private readonly List<Cat> _ate = new();
	private readonly List<string> _log = new();

	public ChoreTests() {
		TuningSettings.ResetToDefaults();
		// No patrols and no hunting alone, so every cat stays in camp and only the chores cost energy
		Tuning.PatrolEnergyCost = Tuning.EnergyMax + 100f;
		Tuning.MinHuntAloneChance = 0f;
		Tuning.MaxHuntAloneChance = 0f;
	}

	private World NewWorld(int mice, params Cat[] cats) {
		var clan = new Clan("TestClan");
		foreach (var cat in cats)
			clan.AddCat(cat);
		for (var i = 0; i < mice; i++)
			clan.FreshKillPile.Add(new Prey(PreyKind.Mouse, 0));

		var world = new World(clan, 1, (_, message) => _log.Add(message));
		world.ChoreDone += (chore, carrier, _) => _done.Add((chore, carrier));
		world.CatAte += _ate.Add;
		return world;
	}

	private static Cat Hungry(Cat cat) {
		cat.LoseSatiation(Tuning.SatiationMax - Tuning.EatBelowSatiation + 1f);
		return cat;
	}

	private static Cat Warrior(string name, Sex sex = Sex.Tom) {
		return new Cat(name, Rank.Warrior, sex, 30, 50);
	}

	private static Cat Apprentice(string name) {
		return new Cat(name, Rank.Apprentice, Sex.SheCat, 8, 50);
	}

	private static Cat Elder(string name) {
		return new Cat(name, Rank.Elder, Sex.Tom, 130, 50);
	}

	/// <summary>A queen in the nursery: her newborn kit makes her a nursing queen.</summary>
	private static (Cat Queen, Cat Kit) QueenWithKit() {
		var queen = Warrior("Brindleface", Sex.SheCat);
		var kit = new Cat("Ashkit", Rank.Kit, Sex.Tom, 0, 50, mother: queen);
		return (queen, kit);
	}

	[Fact]
	public void BringPrey_AMateInCampBringsItBeforeAnApprentice() {
		var (queen, kit) = QueenWithKit();
		var mate = Warrior("Lionheart");
		queen.BecomeMates(mate);
		var apprentice = Apprentice("Sandpaw");
		var world = NewWorld(10, Hungry(queen), kit, apprentice, mate);

		world.Tick();

		var (chore, carrier) = Assert.Single(_done);
		Assert.Equal(queen, chore.ForCat);
		Assert.Equal(mate, carrier);
		Assert.Contains(queen, _ate);
	}

	[Fact]
	public void BringPrey_AnApprenticeBringsItBeforeAWarrior() {
		var elder = Elder("Halftail");
		var warrior = Warrior("Longtail");
		var apprentice = Apprentice("Sandpaw");
		var world = NewWorld(10, Hungry(elder), warrior, apprentice);

		world.Tick();

		var (_, carrier) = Assert.Single(_done);
		Assert.Equal(apprentice, carrier);
	}

	[Fact]
	public void BringPrey_AWarriorStepsInWhenTheApprenticeHasDoneEnough() {
		var elders = new[] { Elder("Halftail"), Elder("Smallear"), Elder("Patchpelt") };
		var warrior = Warrior("Longtail");
		var apprentice = Apprentice("Sandpaw");
		var world = NewWorld(10, elders.Select(Hungry).Append(warrior).Append(apprentice).ToArray());

		world.Tick();

		Assert.Equal(3, _done.Count);
		Assert.Equal(Tuning.MaxChoresPerTick, _done.Count(d => d.Carrier == apprentice));
		Assert.Equal(3 - Tuning.MaxChoresPerTick, _done.Count(d => d.Carrier == warrior));
	}

	[Fact]
	public void BringPrey_AWarriorStepsInWhenTheApprenticeIsTooTired() {
		var elder = Elder("Halftail");
		var warrior = Warrior("Longtail");
		var apprentice = Apprentice("Sandpaw");
		// Awake (above SleepBelowEnergy), but after this tick's idle cost and the chore it would be under ChoreMinEnergy
		apprentice.LoseEnergy(Tuning.EnergyMax - (Tuning.ChoreMinEnergy + Tuning.IdleEnergyCost + Tuning.ChoreEnergyCost) + 1f);
		var world = NewWorld(10, Hungry(elder), warrior, apprentice);

		world.Tick();

		var (_, carrier) = Assert.Single(_done);
		Assert.Equal(warrior, carrier);
	}

	[Fact]
	public void BringPrey_AQueenNobodyBringsFoodFetchesItHerselfAfterOneTick() {
		var (queen, kit) = QueenWithKit();
		var world = NewWorld(10, Hungry(queen), kit);

		world.Tick();

		Assert.DoesNotContain(queen, _ate);
		Assert.Equal(queen, Assert.Single(world.Clan.Chores).ForCat);

		world.Tick();

		Assert.Contains(queen, _ate);
		Assert.Empty(_done);
		Assert.Empty(world.Clan.Chores);
	}

	[Fact]
	public void BringPrey_AHunterBackFromPatrolCanCarry() {
		TuningSettings.ResetToDefaults();
		Tuning.MinHuntAloneChance = 0f;
		Tuning.MaxHuntAloneChance = 0f;
		var elder = Elder("Halftail");
		var warrior = Warrior("Longtail");
		// One mouse for two cats: the leader sends the warrior hunting. He's back before the Clan eats.
		var world = NewWorld(1, Hungry(elder), warrior);

		world.Tick();

		Assert.Equal(Activity.Hunting, warrior.Activity);
		Assert.Equal(warrior, Assert.Single(_done).Carrier);
		Assert.Contains(elder, _ate);
	}

	[Fact]
	public void BringPrey_AMedicineCatKeepsEnergyForCaringForTheSick() {
		var elder = Elder("Halftail");
		elder.AddCondition(new Condition(ConditionType.Wound, 10f, 1f));
		var medicineCat = new Cat("Spottedleaf", Rank.MedicineCat, Sex.SheCat, 40, 50);
		// Enough for the chore alone, but not if it keeps back the energy for caring afterwards
		medicineCat.LoseEnergy(Tuning.EnergyMax - (Tuning.ChoreMinEnergy + Tuning.IdleEnergyCost + Tuning.ChoreEnergyCost));
		var world = NewWorld(10, Hungry(elder), medicineCat);

		world.Tick();

		Assert.Empty(_done);
		Assert.DoesNotContain(elder, _ate);
	}

	[Fact]
	public void BringPrey_AStarvingCatWhoWaitsCountsAsPassedOver() {
		var elder = Elder("Halftail");
		elder.LoseSatiation(Tuning.SatiationMax);
		var warrior = Warrior("Longtail");
		// Hungry and awake, but too tired to carry
		warrior.LoseEnergy(Tuning.EnergyMax - (Tuning.ChoreMinEnergy + 5f));
		var world = NewWorld(10, elder, Hungry(warrior));
		world.Clan.FeedingPolicy = FeedingPolicy.ByTheCode;

		world.Tick();

		Assert.True(elder.IsStarving);
		Assert.Equal(new[] { warrior }, _ate);
		Assert.Contains(_log, l => l.Contains("while the starving Halftail got nothing"));
	}

	[Fact]
	public void BringPrey_AConfinedCatNobodyBringsFoodGoesHungry() {
		var sick = Warrior("Longtail");
		sick.AddCondition(new Condition(ConditionType.Wound, Tuning.ConfinedSeverity + 10f, 1f));
		var apprentice = Apprentice("Sandpaw");
		// Too tired to carry, but not tired enough to sleep
		apprentice.LoseEnergy(Tuning.EnergyMax - (Tuning.ChoreMinEnergy + 5f));
		var world = NewWorld(10, Hungry(sick), apprentice);

		world.Tick();
		world.Tick();

		Assert.True(sick.IsConfined);
		Assert.DoesNotContain(sick, _ate);
		Assert.Equal(sick, Assert.Single(world.Clan.Chores).ForCat);
	}

	[Fact]
	public void BringPrey_TheEatingOrderStillDecidesWhoGetsThePiece() {
		// The leader who feeds the best hunters first gives the only mouse to the warrior, not to the elder
		var elder = Elder("Halftail");
		var hunter = new Cat("Longtail", Rank.Warrior, Sex.Tom, 30, 50,
			new Dictionary<SkillType, Skill> { [SkillType.Hunting] = new(80, 50) });
		var apprentice = Apprentice("Sandpaw");
		var world = NewWorld(1, Hungry(elder), Hungry(hunter), apprentice);
		world.Clan.FeedingPolicy = FeedingPolicy.Ruthless;

		world.Tick();

		Assert.Equal(new[] { hunter }, _ate);
		Assert.Empty(_done);
	}

	[Fact]
	public void BringPrey_ByTheCodeTheElderGetsTheOnlyPieceBrought() {
		var elder = Elder("Halftail");
		var hunter = new Cat("Longtail", Rank.Warrior, Sex.Tom, 30, 50,
			new Dictionary<SkillType, Skill> { [SkillType.Hunting] = new(80, 50) });
		var apprentice = Apprentice("Sandpaw");
		var world = NewWorld(1, Hungry(elder), Hungry(hunter), apprentice);
		world.Clan.FeedingPolicy = FeedingPolicy.ByTheCode;

		world.Tick();

		Assert.Equal(new[] { elder }, _ate);
		Assert.Equal(apprentice, Assert.Single(_done).Carrier);
	}
}
