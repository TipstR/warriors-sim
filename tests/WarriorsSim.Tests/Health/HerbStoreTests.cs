using WarriorsSim.Simulation;
using Xunit;

namespace WarriorsSim.Tests;

/// <summary>The medicine cat's herb store: doses come in by gathering, go out by treating, and slowly spoil.</summary>
public class HerbStoreTests {
	public HerbStoreTests() {
		TuningSettings.ResetToDefaults();
	}

	[Fact]
	public void New_TheStoreIsEmpty() {
		var store = new HerbStore();

		Assert.Equal(0f, store.Doses(Herb.Catmint));
		Assert.False(store.TryUse(Herb.Catmint));
	}

	[Fact]
	public void Add_EveryHerbHasItsOwnDoses() {
		var store = new HerbStore();

		store.Add(Herb.Cobweb, 2.5f);
		store.Add(Herb.Cobweb, 1f);
		store.Add(Herb.Tansy, 4f);

		Assert.Equal(3.5f, store.Doses(Herb.Cobweb), 4);
		Assert.Equal(4f, store.Doses(Herb.Tansy), 4);
		Assert.Equal(0f, store.Doses(Herb.Marigold));
	}

	[Fact]
	public void Add_NegativeAmountsDoNothing() {
		var store = new HerbStore();
		store.Add(Herb.Cobweb, 2f);

		store.Add(Herb.Cobweb, -5f);

		Assert.Equal(2f, store.Doses(Herb.Cobweb), 4);
	}

	[Fact]
	public void TryUse_TakesExactlyOneDose() {
		var store = new HerbStore();
		store.Add(Herb.Marigold, 2.5f);

		Assert.True(store.TryUse(Herb.Marigold));

		Assert.Equal(1.5f, store.Doses(Herb.Marigold), 4);
	}

	[Fact]
	public void TryUse_NeedsAWholeDose() {
		var store = new HerbStore();
		store.Add(Herb.Marigold, 1.5f);

		Assert.True(store.TryUse(Herb.Marigold));
		Assert.False(store.TryUse(Herb.Marigold));

		Assert.Equal(0.5f, store.Doses(Herb.Marigold), 4);
	}

	[Fact]
	public void TryUse_ExactlyOneDoseIsEnough() {
		var store = new HerbStore();
		store.Add(Herb.Catmint, 1f);

		Assert.True(store.TryUse(Herb.Catmint));
	}

	[Fact]
	public void Spoil_EveryHerbLosesTheShare() {
		var store = new HerbStore();
		store.Add(Herb.Catmint, 10f);
		store.Add(Herb.Tansy, 4f);

		store.Spoil(0.1f);

		Assert.Equal(9f, store.Doses(Herb.Catmint), 4);
		Assert.Equal(3.6f, store.Doses(Herb.Tansy), 4);
	}
}
