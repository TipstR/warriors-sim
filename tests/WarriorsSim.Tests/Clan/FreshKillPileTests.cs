using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>Prey goes bad piece by piece, and whoever eats first gets the freshest piece.</summary>
public class FreshKillPileTests {
	private const int Day = Tuning.TicksPerDay;

	public FreshKillPileTests() {
		TuningSettings.ResetToDefaults();
		Tuning.PreyFreshDays = 1f;
		Tuning.PreyStaleDays = 1f;
	}

	[Fact]
	public void Prey_IsFreshThenStaleThenCrowfood() {
		var prey = new Prey(10);

		Assert.Equal(Freshness.Fresh, prey.FreshnessAt(10));
		Assert.Equal(Freshness.Fresh, prey.FreshnessAt(10 + Day - 1));
		Assert.Equal(Freshness.Stale, prey.FreshnessAt(10 + Day));
		Assert.Equal(Freshness.Stale, prey.FreshnessAt(10 + 2 * Day - 1));
		Assert.Equal(Freshness.Crowfood, prey.FreshnessAt(10 + 2 * Day));
	}

	[Fact]
	public void Prey_StaleGivesLessFoodThanFresh() {
		var prey = new Prey(0);

		Assert.True(prey.FoodAt(Day) < prey.FoodAt(0));
		Assert.Equal(0f, prey.FoodAt(2 * Day));
	}

	[Fact]
	public void TakeBest_TakesTheFreshestPiece() {
		var pile = new FreshKillPile();
		pile.Add(new Prey(0));
		pile.Add(new Prey(5));
		pile.Add(new Prey(2));

		Assert.Equal(5, pile.TakeBest().CaughtAtTick);
		Assert.Equal(2, pile.TakeBest().CaughtAtTick);
		Assert.Equal(0, pile.TakeBest().CaughtAtTick);
		Assert.Null(pile.TakeBest());
	}

	[Fact]
	public void ThrowOutCrowfood_RemovesOnlyRottenPieces() {
		var pile = new FreshKillPile();
		pile.Add(new Prey(0));
		pile.Add(new Prey(Day));
		pile.Add(new Prey(2 * Day));

		var thrownOut = pile.ThrowOutCrowfood(2 * Day);

		Assert.Equal(1, thrownOut);
		Assert.Equal(2, pile.Mice);
		Assert.Equal(1, pile.Count(Freshness.Stale, 2 * Day));
		Assert.Equal(1, pile.Count(Freshness.Fresh, 2 * Day));
	}
}
