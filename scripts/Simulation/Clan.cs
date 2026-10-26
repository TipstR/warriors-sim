using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

public class Clan {
    private readonly List<Cat> _cats = new();
    private readonly List<Cat> _dead = new();

    public Clan(string name) {
        Name = name;
    }

    public string Name { get; }
    /// <summary>The living cats of the Clan.</summary>
    public IReadOnlyList<Cat> Cats => _cats;

    /// <summary>Cats that have died, in the order they died.</summary>
    public IReadOnlyList<Cat> Dead => _dead;
    public FreshKillPile FreshKillPile { get; } = new();

    /// <summary>The Clan's leader, or null if the Clan has none.</summary>
    public Cat Leader => _cats.FirstOrDefault(c => c.Rank == Rank.Leader);

    public void AddCat(Cat cat) {
        _cats.Add(cat);
    }

    /// <summary>Moves a cat from the living to the dead.</summary>
    public void RecordDeath(Cat cat) {
        if (_cats.Remove(cat))
            _dead.Add(cat);
    }

    /// <summary>How the Clan decides who eats first. Later this will come from the leader's personality.</summary>
    public FeedingPolicy FeedingPolicy { get; set; } = Tuning.DefaultFeedingPolicy;

    /// <summary>
    ///     Cats in the order they get to eat when there isn't enough prey for everyone, decided by the
    ///     <see cref="FeedingPolicy" />. With the same score, the hungriest cat eats first.
    /// </summary>
    public IEnumerable<Cat> CatsInEatingOrder() {
        return _cats.OrderByDescending(FeedingPolicy.Score).ThenBy(c => c.Satiation);
    }
}