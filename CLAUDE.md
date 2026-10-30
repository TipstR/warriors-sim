# Warriors Sim – Notes for Claude

A Godot 4.7 / C# game inspired by the *Warriors* books. The first goal is a
simulation of a Clan that runs on its own and creates stories.

Read `docs/Vision.md` for the vision and the current state, and
`docs/Milestone1.md` for the rules of the first simulation.
**`docs/Handover.md` has the latest state and a TODO list** (sickness and
medicine cats are newer than the other docs).

## The user

- Has a clear vision but is **new to game and simulation development**.
  Explain things plainly, recommend a concrete way instead of listing every
  option, and keep steps small enough to build and test.
- Often writes parts of the code themselves (especially UI). When they say
  they want to do something themselves, explain and don't change the file.
- **Ask questions as plain text, never as multiple choice** (no question
  tool). The user wants to answer in their own words. Put numbered questions
  with context and a recommendation at the end, then wait for the answer.
- Don't just agree: if the user's idea has a problem, say so.

## Design principles

- **Emergent storytelling with a RimWorld-like feeling**, not a kids' game.
  Stories come from systems interacting, not from scripts. Consequences stick
  (death, losses), systems affect each other, cats should remember things.
- **Skill matters.** A new apprentice can't feed the Clan, a very good hunter
  can. Keep the gap between weak and strong cats large. Fix food shortages
  with other means (more hunters, training, rules), not by making hunting
  easier for everyone.
- **The player plays one cat at a time but can switch to any cat**, like in
  The Sims. The story is that of all Clans across generations (inspired by
  CK3). So every cat must be a full character.
- **Everything is still provisional.** "[Decided]" in `docs/Vision.md` only
  means "the user said so", not that it is final.

## Project structure

The simulation (pure C#, no scenes) is sorted by topic. The Godot part is
sorted by feature: a scene, its script and its assets live together in one
folder, files in snake_case except C# scripts (named like their class).
Namespaces don't follow folders: all simulation code is `WarriorsSim.Simulation`.
When moving a script, move its `.uid` file with it.

- `scripts/Simulation/` – the simulation, plain C# without Godot code.
  - `Core/`: `World.cs` runs everything tick by tick (4 ticks per day). It is a
    partial class split by topic (`Health/World.Health.cs`,
    `Health/World.Medicine.cs`). `Tuning.cs` holds all balancing values as
    static fields with `[TuningGroup]`, `TuningSettings.cs` lists and resets
    them. Also `TimeOfDay`, `Season`, `LogLevel`.
  - `Cats/`: `Cat.cs`, `CatNames.cs`, `Rank`, `Sex`, `Skill.cs` (a skill's level
    and gift; every cat has one per `SkillType` in `cat.Skills`).
  - `Clan/`: `Clan.cs`, `FreshKillPile.cs`, `FeedingPolicy.cs` (leader styles),
    `StartingClans.cs` (ThunderClan from *Into the Wild*), `Den`.
  - `Health/`: `Condition.cs` (a sickness or injury), `Herb.cs` (herb store),
    sickness and medicine cats.
- `ui/simulation_ui/` – `simulation_ui.tscn` + `SimulationUi.cs`: the dev tool
  and main scene (tabs: Clan, Tuning, Simulation). The user built the layout
  in the editor, the code finds nodes by unique name (`%Name`).
- `assets/` – shared assets (later `sprites/`, `audio/`, `fonts/`). Assets that
  belong to one feature go into that feature's folder.
- `tests/WarriorsSim.Tests/` – unit tests (xUnit). The project compiles
  `scripts/Simulation/**/*.cs` directly, so the tests don't need Godot. Test
  files mirror the simulation folders (e.g. `Cats/FrailtyTests.cs`). The Godot
  project ignores `tests/` (`Compile Remove` in the `.csproj`, `.gdignore`).
- `tools/BalanceRunner/` – console program for balance tests (many seeds,
  many years). Compiles the simulation directly, like the tests; the Godot
  project ignores `tools/` too.
- `docs/` – vision, milestone rules, handover, the user's own notes.
- `logs/` – simulation logs (ignored by git).

## Working rules

- **Rider and Godot keep their own copy of open files.** If the user has
  unsaved changes while Claude edits a file, the user's save overwrites
  Claude's change. Ask the user to save in Rider (Ctrl+Shift+S) before editing,
  and to close or reload scenes in Godot. If a change "disappeared", check the
  file on disk and redo it.
- **Scenes (`.tscn`) can be edited as text.** Prefer containers and anchors,
  give nodes the user needs in code a unique name. Commit `.uid` files.
- **Code style** is set in `.editorconfig`: braces on the same line,
  tabs for indentation (the user's choice), `var` where Rider uses it. Match
  the surrounding code.
- **Build:** `dotnet build` in the project folder (builds the game and the
  tests).
- **Tests:** `dotnet test tests/WarriorsSim.Tests`, or the Unit Tests window in
  Rider. `Tuning` is static and shared by all tests, so tests run one after
  another, and every test class calls `TuningSettings.ResetToDefaults()` in its
  constructor.
- **Balance changes must be tested with many random seeds and long runs**
  (100 seeds × 30 years; 10 seeds are too noisy), not with a single run. Use
  `dotnet run -c Release --project tools/BalanceRunner -- 100 30` (about
  25 seconds, seeds run in parallel). Extra arguments change Tuning values for
  that run, e.g. `HuntingPracticePerMouse=0.004` or `Policy=Compassionate`. It
  prints the statistics and checks the targets in `Report.cs`. Add new
  statistics there when a step needs them. Ask before many long runs when the
  user is on battery.
- Godot can be started headless (`--headless --path . -s test.gd`) to check
  scenes, or with a window to save a screenshot of the viewport.
- **After changing an asset from outside (e.g. a PNG), tell the user to
  reimport or reload the project in Godot.** If a headless Godot imported it
  first, the open editor thinks it is up to date and keeps showing the old
  version.
- Commit and push only when the user asks. Split commits by feature.

## Lessons from balancing

- The simulation tips over easily. Every downward spiral (hunger → weakness →
  less prey) needs a counterweight (sharing, famine rules, elders and queens
  helping).
- Without a prey limit the Clan grows forever, without births it dies out, and
  a birth brake that is too strong lets the Clan age until it collapses.
- Feeding everyone evenly makes everyone die at the same moment. Starvation
  death is a growing risk instead of a fixed limit for that reason.
- The food balance is very sensitive to the *average* hunting skill: 2–3
  points less made several times as many Clans die out. The spread of skill
  barely matters. Every cat that doesn't hunt (medicine cats, sick cats)
  counts.
- The birth brake must never stop births completely
  (`Tuning.MinCrowdingFactor`). When medicine cats stopped sickness from
  killing, half the Clans grew old together and died out. A young Clan comes
  from births continuing and hunger thinning, not from fewer deaths.
- Contagion is about how many cats one sick cat infects (days contagious ×
  chance × cats in its den × season). Above 1 the whole Clan gets sick, so
  small values matter a lot.
- Penalties that feed each other (hunger makes sickness deadlier, sickness
  makes hunters weaker) spiral quickly. Keep each one mild.
