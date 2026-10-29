using System;
using System.Collections.Generic;
using System.Linq;

namespace WarriorsSim.Simulation;

/// <summary>
///     Warrior names have a prefix that stays for life (Gray) and a suffix that changes with rank:
///     Graykit, Graypaw, Graystripe, and as leader Graystar.
/// </summary>
public static class CatNames {
	public const string KitSuffix = "kit";
	public const string ApprenticeSuffix = "paw";
	public const string LeaderSuffix = "star";

	/// <summary>Suffixes a cat can get at its warrior ceremony.</summary>
	public static readonly string[] WarriorSuffixes = {
		"claw", "heart", "storm", "stripe", "tail", "pelt", "fur", "leaf", "wind", "whisker",
		"foot", "fang", "flower", "face", "step", "cloud", "frost", "nose", "ear", "eye", "breeze", "shade"
	};

	/// <summary>Prefixes for newborn kits.</summary>
	public static readonly string[] KitPrefixes = {
		"Acorn", "Amber", "Ash", "Badger", "Beech", "Berry", "Birch", "Blossom", "Bramble", "Briar", "Bright",
		"Brindle", "Cedar", "Cherry", "Cinder", "Cloud", "Copper", "Dapple", "Dawn", "Dove", "Dusk", "Eagle",
		"Ember", "Fallow", "Fern", "Finch", "Flame", "Fox", "Gorse", "Hawk", "Hazel", "Heather", "Holly",
		"Honey", "Ice", "Ivy", "Jay", "Juniper", "Lark", "Larch", "Lily", "Lion", "Mallow", "Maple", "Mist",
		"Moss", "Mouse", "Night", "Nettle", "Oak", "Owl", "Pebble", "Pine", "Poppy", "Quail", "Rain", "Rook",
		"Rose", "Rowan", "Rush", "Sage", "Sedge", "Shrew", "Snow", "Sorrel", "Sparrow", "Speckle", "Spider",
		"Squirrel", "Stone", "Swift", "Tawny", "Thistle", "Thrush", "Tiny", "Wasp", "Willow", "Wren"
	};

	private static readonly string[] AllSuffixes =
		WarriorSuffixes.Append(KitSuffix).Append(ApprenticeSuffix).Append(LeaderSuffix)
			.OrderByDescending(s => s.Length)
			.ToArray();

	/// <summary>
	///     Splits a full name into prefix and suffix, e.g. "Tigerclaw" into "Tiger" and "claw".
	///     Names without a known suffix (e.g. "One-Eye" works, "Fluffy" doesn't) keep the whole name as prefix.
	/// </summary>
	public static (string Prefix, string Suffix) Split(string name) {
		foreach (var suffix in AllSuffixes)
			if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
				return (name[..^suffix.Length], name[^suffix.Length..]);

		return (name, "");
	}

	/// <summary>Picks a kit prefix that no living Clanmate has.</summary>
	public static string RandomKitPrefix(IEnumerable<string> takenPrefixes, Random random) {
		var taken = takenPrefixes.ToHashSet(StringComparer.OrdinalIgnoreCase);
		var free = KitPrefixes.Where(p => !taken.Contains(p)).ToList();
		var options = free.Count > 0 ? free : KitPrefixes.ToList();
		return options[random.Next(options.Count)];
	}

	/// <summary>Picks a warrior suffix that doesn't give the cat the same name as a living Clanmate.</summary>
	public static string RandomWarriorSuffix(string prefix, IEnumerable<string> takenNames, Random random) {
		var taken = takenNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
		// No "Cloudcloud" either
		var free = WarriorSuffixes
			.Where(s => !taken.Contains(prefix + s) && !s.Equals(prefix, StringComparison.OrdinalIgnoreCase))
			.ToList();
		var options = free.Count > 0 ? free : WarriorSuffixes.ToList();
		return options[random.Next(options.Count)];
	}
}
