using System.Collections.Generic;
using System.Linq;
using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>
///     Who eats first when there isn't enough prey, for each leader style. Every test is a design statement, written
///     from what the code does on 2026-11-03. The user still has to confirm them (see docs/TestPlan.md, package 7):
///     if a statement is wrong, the code changes, not just the test. The tests compare cats, they never check exact
///     scores, so balancing the weights keeps them green as long as the statements stay true.
/// </summary>
public class FeedingPolicyTests {
	public FeedingPolicyTests() {
		TuningSettings.ResetToDefaults();
	}

	private static Cat Warrior(float hunting = 50, string name = "Longtail") {
		return new Cat(name, Rank.Warrior, Sex.Tom, 30, 50, Skills(hunting));
	}

	private static Cat Kit(int ageMoons = 1) {
		return new Cat("Cloudkit", Rank.Kit, Sex.Tom, ageMoons, 50);
	}

	private static Cat Elder(float hunting = 70) {
		return new Cat("Halftail", Rank.Elder, Sex.Tom, 130, 50, Skills(hunting));
	}

	private static Cat Queen() {
		var queen = new Cat("Brindleface", Rank.Warrior, Sex.SheCat, 30, 50, Skills(50));
		queen.IsNursing = true;
		return queen;
	}

	private static Cat Sick(Cat cat) {
		cat.AddCondition(new Condition(ConditionType.Wound, 30f, 1f));
		return cat;
	}

	private static Cat Starving(Cat cat, int stacks = 0) {
		cat.LoseSatiation(Tuning.SatiationMax);
		for (var i = 0; i < stacks; i++)
			cat.AddStarvationStack();
		return cat;
	}

	private static Dictionary<SkillType, Skill> Skills(float hunting, float healing = 0, int gift = 50) {
		return new Dictionary<SkillType, Skill> {
			[SkillType.Hunting] = new(hunting, gift),
			[SkillType.Healing] = new(healing, gift)
		};
	}

	private static void AssertEatsBefore(FeedingPolicy policy, Cat first, Cat second, bool famine = false) {
		Assert.True(policy.Score(first, famine) > policy.Score(second, famine),
			$"{first} ({policy.Score(first, famine):0.00}) should eat before {second} ({policy.Score(second, famine):0.00})");
	}

	// Balanced: starving cats first, then a mix of everything. In a famine, the hunters come first.

	[Fact]
	public void Balanced_AStarvingCatEatsBeforeAnyoneWhoIsNot() {
		var starving = Starving(Warrior(10));

		AssertEatsBefore(FeedingPolicy.Balanced, starving, Kit());
		AssertEatsBefore(FeedingPolicy.Balanced, starving, Queen());
		AssertEatsBefore(FeedingPolicy.Balanced, starving, Warrior(95), famine: true);
	}

	[Fact]
	public void Balanced_OfTwoStarvingCatsTheOneCloserToDeathEatsFirst() {
		var nearDeath = Starving(Warrior(), Tuning.StarvationSafeStacks + 2);
		var justStarving = Starving(Warrior(), 1);

		AssertEatsBefore(FeedingPolicy.Balanced, nearDeath, justStarving);
	}

	[Fact]
	public void Balanced_AStarvingCatEatsBeforeOneThatJustAteButIsStillCloseToDeath() {
		var justAte = Starving(Warrior(), Tuning.StarvationStacksToDie - 1);
		justAte.Eat(Tuning.SatiationLossPerTick * 0.5f);

		AssertEatsBefore(FeedingPolicy.Balanced, Starving(Warrior()), justAte);
	}

	[Fact]
	public void Balanced_AnElderEatsBeforeAGoodHunterButNotInAFamine() {
		var elder = Elder();
		var hunter = Warrior(80);

		AssertEatsBefore(FeedingPolicy.Balanced, elder, hunter);
		AssertEatsBefore(FeedingPolicy.Balanced, hunter, elder, famine: true);
	}

	// Compassionate: protects the weak, even when it costs the Clan (a Firestar).

	[Fact]
	public void Compassionate_AStarvingCatEatsBeforeAnyoneWhoIsNot() {
		AssertEatsBefore(FeedingPolicy.Compassionate, Starving(Warrior(10)), Kit());
	}

	[Fact]
	public void Compassionate_TheWeakEatBeforeTheBestHunter() {
		var hunter = Warrior(95);

		AssertEatsBefore(FeedingPolicy.Compassionate, Kit(), hunter);
		AssertEatsBefore(FeedingPolicy.Compassionate, Queen(), hunter);
		AssertEatsBefore(FeedingPolicy.Compassionate, Sick(Warrior()), hunter);
		AssertEatsBefore(FeedingPolicy.Compassionate, Elder(), hunter);
	}

	[Fact]
	public void Compassionate_QueensAndTheSickNeedItMostThenTheFrail() {
		AssertEatsBefore(FeedingPolicy.Compassionate, Queen(), Elder());
		AssertEatsBefore(FeedingPolicy.Compassionate, Sick(Warrior()), Elder());
	}

	[Fact]
	public void Compassionate_EvenInAFamineAYoungKitEatsBeforeTheBestHunter() {
		AssertEatsBefore(FeedingPolicy.Compassionate, Kit(), Warrior(95), famine: true);
	}

	[Fact]
	public void Compassionate_ByHowWeakACatReallyIsNotByRank() {
		// A newborn is frailer than a kit about to become an apprentice
		AssertEatsBefore(FeedingPolicy.Compassionate, Kit(0), Kit(5));
	}

	// Ruthless: feeds whoever keeps the Clan alive. The weak die first (a Tigerclaw).

	[Fact]
	public void Ruthless_TheBetterHunterEatsFirst() {
		AssertEatsBefore(FeedingPolicy.Ruthless, Warrior(80), Warrior(40));
	}

	[Fact]
	public void Ruthless_EvenAStarvingKitEatsAfterAWeakWarrior() {
		AssertEatsBefore(FeedingPolicy.Ruthless, Warrior(20), Starving(Kit(4), Tuning.StarvationSafeStacks));
	}

	[Fact]
	public void Ruthless_AnElderEatsAfterEveryHunterHoweverSkilled() {
		AssertEatsBefore(FeedingPolicy.Ruthless, Warrior(10), Elder(90));
	}

	[Fact]
	public void Ruthless_AHunterTooSickToHuntEatsAfterAWeakHealthyOne() {
		var confined = Warrior(90);
		confined.AddCondition(new Condition(ConditionType.Wound, Tuning.ConfinedSeverity, 1f));

		AssertEatsBefore(FeedingPolicy.Ruthless, Warrior(20), confined);
	}

	[Fact]
	public void Ruthless_AnApprenticeIsJudgedByWhatItPromisesToBecome() {
		var giftedApprentice = new Cat("Firepaw", Rank.Apprentice, Sex.Tom, 7, 50, Skills(20, gift: 90));
		var plainApprentice = new Cat("Ravenpaw", Rank.Apprentice, Sex.Tom, 7, 50, Skills(20, gift: 30));

		AssertEatsBefore(FeedingPolicy.Ruthless, giftedApprentice, Warrior());
		AssertEatsBefore(FeedingPolicy.Ruthless, Warrior(), plainApprentice);
	}

	[Fact]
	public void Ruthless_AMedicineCatIsJudgedByHealing() {
		var healer = new Cat("Spottedleaf", Rank.MedicineCat, Sex.SheCat, 40, 50, Skills(0, healing: 80));
		var giftedApprentice =
			new Cat("Cinderpaw", Rank.MedicineCatApprentice, Sex.SheCat, 8, 50, Skills(0, healing: 10, gift: 90));
		var poorHealer = new Cat("Yellowfang", Rank.MedicineCat, Sex.SheCat, 40, 50, Skills(90, healing: 10));

		AssertEatsBefore(FeedingPolicy.Ruthless, healer, Warrior());
		AssertEatsBefore(FeedingPolicy.Ruthless, giftedApprentice, Warrior());
		AssertEatsBefore(FeedingPolicy.Ruthless, Warrior(), poorHealer);
	}

	// By the code: follows the warrior code to the letter, no matter who starves, even in a famine.

	[Fact]
	public void ByTheCode_TheWarriorCodesOrderEvenWhenTheLastAreStarving() {
		var clan = new Clan("ThunderClan") { FeedingPolicy = FeedingPolicy.ByTheCode, IsInFamine = true };
		var deputy = new Cat("Redtail", Rank.Deputy, Sex.Tom, 60, 50);
		var leader = new Cat("Bluestar", Rank.Leader, Sex.SheCat, 80, 50);
		var apprentice = Starving(new Cat("Ravenpaw", Rank.Apprentice, Sex.Tom, 7, 50), Tuning.StarvationSafeStacks);
		var warrior = Starving(Warrior(95));
		var sickWarrior = Sick(Warrior(name: "Darkstripe"));
		var expected = new[] { Kit(), Queen(), Elder(), sickWarrior, leader, deputy, warrior, apprentice };
		// Added in reverse, and the ones that should eat last are the hungriest, so only the code's order can win
		foreach (var cat in expected.Reverse())
			clan.AddCat(cat);

		Assert.Equal(expected, clan.CatsInEatingOrder().ToArray());
	}

	[Fact]
	public void ByTheCode_TheMedicineCatEatsWithTheLeaderItsApprenticeRightAfter() {
		var leader = new Cat("Bluestar", Rank.Leader, Sex.SheCat, 80, 50);
		var medicineCat = new Cat("Spottedleaf", Rank.MedicineCat, Sex.SheCat, 40, 50);
		var medicineApprentice = new Cat("Cinderpaw", Rank.MedicineCatApprentice, Sex.SheCat, 8, 50);
		var deputy = new Cat("Redtail", Rank.Deputy, Sex.Tom, 60, 50);

		Assert.Equal(FeedingPolicy.ByTheCode.Score(leader, false), FeedingPolicy.ByTheCode.Score(medicineCat, false));
		AssertEatsBefore(FeedingPolicy.ByTheCode, medicineCat, medicineApprentice);
		AssertEatsBefore(FeedingPolicy.ByTheCode, medicineApprentice, deputy);
	}

	[Fact]
	public void ByTheCode_ASickElderStillEatsAsAnElder() {
		AssertEatsBefore(FeedingPolicy.ByTheCode, Sick(Elder()), Sick(Warrior()));
	}

	[Fact]
	public void Presets_AllFourLeaderStylesByName() {
		Assert.Equal(4, FeedingPolicy.Presets.Count);
		Assert.Same(FeedingPolicy.Balanced, FeedingPolicy.Presets["Balanced"]);
		Assert.Same(FeedingPolicy.Compassionate, FeedingPolicy.Presets["Compassionate"]);
		Assert.Same(FeedingPolicy.Ruthless, FeedingPolicy.Presets["Ruthless"]);
		Assert.Same(FeedingPolicy.ByTheCode, FeedingPolicy.Presets["By the code"]);
	}
}
