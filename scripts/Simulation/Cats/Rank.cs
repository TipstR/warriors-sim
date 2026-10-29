namespace WarriorsSim.Simulation;

public enum Rank {
	Kit,
	Apprentice,
	Warrior,
	Deputy,
	Leader,
	Elder,

	/// <summary>Learns healing from the medicine cat instead of hunting from a warrior.</summary>
	MedicineCatApprentice,

	/// <summary>Heals the Clan. Doesn't hunt, takes no mate and has no kits.</summary>
	MedicineCat
}