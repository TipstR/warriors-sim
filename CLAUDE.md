# Warriors Sim – Notes for Claude

A Godot 4.7 / C# game inspired by the *Warriors* books. The first goal is a
simulation of a Clan that runs on its own and creates stories.

Read `docs/Vision.md` for the vision and the current state, and
`docs/Milestone1.md` for the rules of the first simulation.

## The user

- Has a clear vision but is **new to game and simulation development**.
  Explain things plainly, recommend a concrete way instead of listing every
  option, and keep steps small enough to build and test.
- Often writes parts of the code themselves (especially UI). When they say
  they want to do something themselves, explain and don't change the file.

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

- `scripts/Simulation/` – the simulation, plain C# without Godot code.
  - `World.cs` runs everything tick by tick (4 ticks per day).
  - `Tuning.cs` holds all balancing values as static fields with
    `[TuningGroup]`. `TuningSettings.cs` lists and resets them.
  - `Cat.cs`, `Clan.cs`, `FeedingPolicy.cs` (leader styles),
    `StartingClans.cs` (ThunderClan from *Into the Wild*), `CatNames.cs`.
- `scripts/UI/` – Godot scripts.
  - `SimulationUi.cs` + `scenes/Simulation_UI.tscn`: the dev tool and main
    scene (tabs: Clan, Tuning, Simulation). The user built the layout in the
    editor, the code finds nodes by unique name (`%Name`).
  - `Dashboard.cs` + `scenes/dashboard.tscn`: an older dashboard built in code.
- `docs/` – vision, milestone rules, the user's own notes.
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
  4 spaces, `var` where Rider uses it. Match the surrounding code.
- **Build:** `dotnet build` in the project folder.
- **Balance changes must be tested with several random seeds and long runs**
  (e.g. 10 seeds, 30 years), not with a single run. A small console project
  outside the repo that compiles `scripts/Simulation/*.cs` works well for this,
  because the simulation has no Godot dependency.
- Godot can be started headless (`--headless --path . -s test.gd`) to check
  scenes, or with a window to save a screenshot of the viewport.
- Commit and push only when the user asks. Split commits by feature.

## Lessons from balancing

- The simulation tips over easily. Every downward spiral (hunger → weakness →
  less prey) needs a counterweight (sharing, famine rules, elders and queens
  helping).
- Without a prey limit the Clan grows forever, without births it dies out, and
  a birth brake that is too strong lets the Clan age until it collapses.
- Feeding everyone evenly makes everyone die at the same moment. Starvation
  death is a growing risk instead of a fixed limit for that reason.
- Right now cats only die of hunger and old age. Predators, sickness and Clan
  battles will need retuning of births, crowding and old age later.
