using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>Chores: small tasks the Clan's cats take from the Clan's chores (<see cref="Clan.Chores" />).</summary>
public partial class World {
	/// <summary>How many chores each cat did this tick, see <see cref="Tuning.MaxChoresPerTick" />.</summary>
	private readonly Dictionary<Cat, int> _choresThisTick = new();

	/// <summary>
	///     The cats who can still carry prey in this feeding, in the Clan's order. Built once before feeding
	///     (<see cref="StartChores" />); a cat drops out when it has done enough chores or is too tired.
	/// </summary>
	private readonly List<Cat> _carriers = new();

	/// <summary>
	///     Energy a medicine cat keeps back in this feeding for its work afterwards (caring for the sick, gathering
	///     herbs), which is paid later in the tick.
	/// </summary>
	private float _healerEnergyReserve;

	// Statistics for the moon summary
	private int _choresByMatesThisMoon;
	private int _choresByApprenticesThisMoon;
	private int _choresByOthersThisMoon;
	private int _fetchedThemselvesThisMoon;
	private int _wentHungryInNestThisMoon;

	/// <summary>
	///     Called when a cat has done a chore, with the chore, the cat who did it and, for bringing prey, the piece it
	///     carried. The cat it was for is <see cref="Chore.ForCat" />.
	/// </summary>
	public event Action<Chore, Cat, Prey> ChoreDone;

	/// <summary>How a cat who stays in its den gets its piece this tick.</summary>
	private enum PreyDelivery {
		/// <summary>Walks to the pile like everyone else (it doesn't stay in its den, or was out hunting).</summary>
		Walks,

		/// <summary>Another cat brings it.</summary>
		Brought,

		/// <summary>Nobody brought it last tick or this tick, so it goes to the pile itself.</summary>
		FetchesItself,

		/// <summary>Nobody can bring it this tick. The chore stays open.</summary>
		Waits
	}

	/// <summary>
	///     Decides how the cat gets the piece the eating order gave it. A cat who stays in its den puts up a chore, and
	///     the first cat on the ladder takes it: its mate, then an apprentice (it's their duty), then any other cat
	///     who can (warriors, medicine cats, ...). If nobody can, the cat waits; if it already waited a tick and can
	///     walk, it fetches the piece itself. A confined cat can't, it goes hungry.
	/// </summary>
	private PreyDelivery ArrangePrey(Cat cat, out Chore chore, out Cat carrier) {
		chore = null;
		carrier = null;
		// In a famine, elders and queens hunt too, and come back past the pile
		if (!cat.StaysInDen || cat.Activity is Activity.Hunting or Activity.Training)
			return PreyDelivery.Walks;

		chore = Clan.FindChore(ChoreKind.BringPrey, cat);
		if (chore == null) {
			chore = new Chore(ChoreKind.BringPrey, cat, TickNumber);
			Clan.PostChore(chore);
		}

		carrier = FindCarrier(cat);
		if (carrier != null)
			return PreyDelivery.Brought;
		// Nights don't count: nobody eats then, so a cat who waited in the evening gets another try at dawn
		if (chore.OpenedAtTick < TickNumber && !cat.IsConfined)
			return PreyDelivery.FetchesItself;
		return PreyDelivery.Waits;
	}

	/// <summary>
	///     Before feeding: who could carry prey this tick. Carriers must be awake, not stay in their den themselves
	///     and not be kits. Patrols are back before the Clan eats, so hunters can carry too (if they have the energy
	///     left). Medicine cats keep back the energy for caring for the sick and gathering herbs, because they do
	///     that after feeding.
	/// </summary>
	private void StartChores() {
		_choresThisTick.Clear();
		var reserveForCaring = Clan.Cats.Any(c => c.IsSick)
			? Tuning.CaringEnergyPerPatient * Tuning.PatientsPerHealer
			: 0f;
		var reserveForGathering = HerbStoreIsLow() ? Tuning.GatheringEnergyCost - Tuning.IdleEnergyCost : 0f;
		_healerEnergyReserve = Math.Max(reserveForCaring, reserveForGathering);

		_carriers.Clear();
		_carriers.AddRange(Clan.Cats.Where(c => !_sleeping.Contains(c) && !c.StaysInDen && c.Rank != Rank.Kit
												&& CanTakeChore(c)));
	}

	/// <summary>
	///     Who brings the cat its prey: its mate, if it can (he wants to); otherwise the apprentice with the most energy
	///     left (it's their duty); otherwise any other cat who can, again the one with the most energy left. Null if
	///     nobody can.
	/// </summary>
	private Cat FindCarrier(Cat forCat) {
		if (forCat.Mate != null && _carriers.Contains(forCat.Mate))
			return forCat.Mate;

		return _carriers.Where(c => c.Rank == Rank.Apprentice).MaxBy(EnergyLeftThisTick)
			   ?? _carriers.MaxBy(EnergyLeftThisTick);
	}

	/// <summary>
	///     Under <see cref="Tuning.MaxChoresPerTick" /> and with enough energy to keep <see cref="Tuning.ChoreMinEnergy" />
	///     after the chore (a medicine cat also keeps back <see cref="_healerEnergyReserve" />).
	/// </summary>
	private bool CanTakeChore(Cat cat) {
		var reserve = cat.IsMedicineCat ? _healerEnergyReserve : 0f;
		return _choresThisTick.GetValueOrDefault(cat) < Tuning.MaxChoresPerTick
			   && EnergyLeftThisTick(cat) - reserve - Tuning.ChoreEnergyCost >= Tuning.ChoreMinEnergy;
	}

	/// <summary>The cat's energy after paying for what it did this tick so far (paid for real in UpdateEnergy).</summary>
	private float EnergyLeftThisTick(Cat cat) {
		return cat.Energy - _energyCostThisTick.GetValueOrDefault(cat, Tuning.IdleEnergyCost);
	}

	/// <summary>
	///     The carrier has brought the piece: it pays for the chore, the chore is done (taken down after feeding, in
	///     <see cref="RemoveFinishedBringPreyChores" />).
	/// </summary>
	private void FinishBringingPrey(Chore chore, Cat carrier, Prey piece) {
		var forCat = chore.ForCat;
		AddEnergyCost(carrier, Tuning.ChoreEnergyCost);
		_choresThisTick[carrier] = _choresThisTick.GetValueOrDefault(carrier) + 1;
		if (!CanTakeChore(carrier))
			_carriers.Remove(carrier);

		var what = piece.Stats.Portions > 1f ? $"some {piece.Name}" : $"a {piece.Name}";
		if (carrier == forCat.Mate) {
			_choresByMatesThisMoon++;
			Log($"{carrier.Name} brought {His(carrier)} mate {forCat.Name} {what}.");
		}
		else {
			if (carrier.Rank == Rank.Apprentice)
				_choresByApprenticesThisMoon++;
			else
				_choresByOthersThisMoon++;
			Log($"{carrier.Name} brought {forCat.Name} {what}.");
		}

		ChoreDone?.Invoke(chore, carrier, piece);
	}

	/// <summary>Nobody brought the cat its prey, so it got up and fetched it itself.</summary>
	private void FetchedItself(Cat cat) {
		_fetchedThemselvesThisMoon++;

		var she = He(cat);
		var how = cat.InMedicineDen ? $"so {she} left the medicine den to fetch it"
			: cat.IsNursing ? $"so {she} left {His(cat)} kits to fetch it"
			: cat.IsInNursery ? $"so {she} left the nursery to fetch it"
			: $"so {she} went to the pile {Himself(cat)}";
		Log($"Nobody brought {cat.Name} food, {how}.", LogLevel.Summary);
	}

	/// <summary>Logs the cats who got no piece this tick because nobody could bring it. Their chores stay open.</summary>
	private void LogWaiting(List<Cat> waiting) {
		var walkers = waiting.Where(c => !c.IsConfined).ToList();
		if (walkers.Count > 0)
			Log($"Nobody could bring {Names(walkers)} food yet.");

		foreach (var cat in waiting.Where(c => c.IsConfined)) {
			_wentHungryInNestThisMoon++;
			Log($"Nobody could bring {cat.Name} food. Too sick to get up, {cat.Name} went hungry.", LogLevel.Summary);
		}
	}

	/// <summary>
	///     After feeding: only the chores of cats still waiting stay up. This is the only place chores are taken
	///     down: done ones, and those of cats who don't need their prey brought anymore (they fetched it themselves,
	///     aren't hungry anymore or don't stay in their den anymore).
	/// </summary>
	private void RemoveFinishedBringPreyChores(HashSet<Cat> stillWaiting) {
		foreach (var chore in Clan.Chores.Where(c => c.Kind == ChoreKind.BringPrey && !stillWaiting.Contains(c.ForCat))
					 .ToList())
			Clan.RemoveChore(chore);
	}

	private void ResetChoreStatistics() {
		_choresByMatesThisMoon = 0;
		_choresByApprenticesThisMoon = 0;
		_choresByOthersThisMoon = 0;
		_fetchedThemselvesThisMoon = 0;
		_wentHungryInNestThisMoon = 0;
	}

	/// <summary>"chores done: 40 (mates 5, apprentices 30, others 5), fetched it themselves: 2, ..."</summary>
	private string ChoreSummary() {
		var done = _choresByMatesThisMoon + _choresByApprenticesThisMoon + _choresByOthersThisMoon;
		return $"chores done: {done} (mates {_choresByMatesThisMoon}, apprentices {_choresByApprenticesThisMoon}, "
			   + $"others {_choresByOthersThisMoon}), fetched it themselves: {_fetchedThemselvesThisMoon}, "
			   + $"went hungry in their nest: {_wentHungryInNestThisMoon}";
	}

	private static string He(Cat cat) {
		return cat.Sex == Sex.SheCat ? "she" : "he";
	}

	private static string His(Cat cat) {
		return cat.Sex == Sex.SheCat ? "her" : "his";
	}

	private static string Himself(Cat cat) {
		return cat.Sex == Sex.SheCat ? "herself" : "himself";
	}
}
