using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>Prey goes bad piece by piece, the first cats get the best piece, and big prey is shared.</summary>
public class FreshKillPileTests {
	private const int Day = Tuning.TicksPerDay;

	public FreshKillPileTests() {
		TuningSettings.ResetToDefaults();
		Tuning.PreyFreshDays = 1f;
		Tuning.PreyStaleDays = 1f;
	}

	private static Prey Mouse(int tick) {
		return new Prey(PreyKind.Mouse, tick);
	}

	[Fact]
	public void Prey_IsFreshThenStaleThenCrowfood() {
		var prey = Mouse(10);

		Assert.Equal(Freshness.Fresh, prey.FreshnessAt(10));
		Assert.Equal(Freshness.Fresh, prey.FreshnessAt(10 + Day - 1));
		Assert.Equal(Freshness.Stale, prey.FreshnessAt(10 + Day));
		Assert.Equal(Freshness.Stale, prey.FreshnessAt(10 + 2 * Day - 1));
		Assert.Equal(Freshness.Crowfood, prey.FreshnessAt(10 + 2 * Day));
	}

	[Fact]
	public void Prey_StaleGivesLessFoodThanFresh() {
		var prey = Mouse(0);

		Assert.True(prey.FoodPerPortionAt(Day) < prey.FoodPerPortionAt(0));
		Assert.Equal(0f, prey.FoodPerPortionAt(2 * Day));
	}

	[Fact]
	public void EatPortion_TheFreshestPieceComesFirst() {
		var pile = new FreshKillPile();
		pile.Add(Mouse(0));
		pile.Add(Mouse(5));
		pile.Add(Mouse(2));

		Assert.Equal(5, pile.EatPortion(5, out _).CaughtAtTick);
		Assert.Equal(2, pile.EatPortion(5, out _).CaughtAtTick);
		Assert.Equal(0, pile.EatPortion(5, out _).CaughtAtTick);
		Assert.Null(pile.EatPortion(5, out _));
	}

	[Fact]
	public void EatPortion_OfEquallyFreshPiecesTheBiggestComesFirst() {
		var pile = new FreshKillPile();
		pile.Add(new Prey(PreyKind.Shrew, 0));
		pile.Add(new Prey(PreyKind.Rabbit, 0));

		Assert.Equal(PreyKind.Rabbit, pile.EatPortion(0, out _).Kind);
	}

	[Fact]
	public void EatPortion_ARabbitFeedsThreeCats() {
		var pile = new FreshKillPile();
		pile.Add(new Prey(PreyKind.Rabbit, 0));

		for (var i = 0; i < 3; i++) {
			Assert.Equal(PreyKind.Rabbit, pile.EatPortion(0, out var food).Kind);
			Assert.Equal(Tuning.PortionSatiation, food);
		}

		Assert.Equal(0, pile.Pieces);
	}

	[Fact]
	public void EatPortion_AStartedFreshPieceIsFinishedFirst() {
		var pile = new FreshKillPile();
		pile.Add(new Prey(PreyKind.Rabbit, 0));
		pile.EatPortion(0, out _);
		pile.Add(Mouse(1)); // fresher, but the rabbit was started

		Assert.Equal(PreyKind.Rabbit, pile.EatPortion(1, out _).Kind);
	}

	[Fact]
	public void EatPortion_AShrewIsHalfAMeal() {
		var pile = new FreshKillPile();
		pile.Add(new Prey(PreyKind.Shrew, 0));

		pile.EatPortion(0, out var food);

		Assert.Equal(Tuning.PortionSatiation / 2f, food);
	}

	[Fact]
	public void ThrowOutCrowfood_RemovesOnlyRottenPieces() {
		var pile = new FreshKillPile();
		pile.Add(Mouse(0));
		pile.Add(Mouse(Day));
		pile.Add(Mouse(2 * Day));

		var thrownOut = pile.ThrowOutCrowfood(2 * Day);

		Assert.Single(thrownOut);
		Assert.Equal(2, pile.Pieces);
		Assert.Equal(1, pile.Count(Freshness.Stale, 2 * Day));
		Assert.Equal(1, pile.Count(Freshness.Fresh, 2 * Day));
	}

	[Fact]
	public void Describe_NamesThePreyByKind() {
		var prey = new[] { Mouse(0), Mouse(0), new Prey(PreyKind.Rabbit, 0), new Prey(PreyKind.Shrew, 0) };

		Assert.Equal("a shrew, 2 mice and a rabbit", Prey.Describe(prey));
		Assert.Equal("nothing", Prey.Describe(new Prey[0]));
	}

	[Fact]
	public void Describe_OtherKindsGetAnS() {
		var prey = new[] { new Prey(PreyKind.Rabbit, 0), new Prey(PreyKind.Rabbit, 0) };

		Assert.Equal("2 rabbits", Prey.Describe(prey));
	}

	[Fact]
	public void FormatPortions_WithADotOnEveryComputer() {
		Assert.Equal("1.5", Prey.FormatPortions(1.5f));
		Assert.Equal("2", Prey.FormatPortions(2f));
	}

	[Fact]
	public void Count_OnlyThePiecesThatAreThisFresh() {
		var pile = new FreshKillPile();
		pile.Add(Mouse(0));
		pile.Add(Mouse(Day));
		pile.Add(Mouse(Day + 1));

		Assert.Equal(2, pile.Count(Freshness.Fresh, Day + 1));
		Assert.Equal(1, pile.Count(Freshness.Stale, Day + 1));
		Assert.Equal(0, pile.Count(Freshness.Crowfood, Day + 1));
	}

	[Fact]
	public void EatPortion_AStartedPieceThatWentStaleIsNotFinishedFirst() {
		var pile = new FreshKillPile();
		pile.Add(new Prey(PreyKind.Rabbit, 0));
		pile.EatPortion(0, out _);
		pile.Add(Mouse(Day));

		Assert.Equal(PreyKind.Mouse, pile.EatPortion(Day, out _).Kind);
	}
}
