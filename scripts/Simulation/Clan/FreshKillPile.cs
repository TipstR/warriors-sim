using System;

namespace WarriorsSim.Simulation;

/// <summary>The Clan's shared food store. For now it only holds mice.</summary>
public class FreshKillPile {
	public int Mice { get; private set; }

	public void Add(int mice) {
		Mice += Math.Max(0, mice);
	}

	/// <summary>Takes one mouse from the pile. Returns false if the pile is empty.</summary>
	public bool TryTakeMouse() {
		if (Mice == 0)
			return false;

		Mice--;
		return true;
	}
}