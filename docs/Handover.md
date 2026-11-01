# Handover – State and TODO (2026-11-01)

For the next agent, on any machine. Read `CLAUDE.md` first. This file holds
what's in progress, what's planned and the user's decisions, because the
agents' own memory doesn't move between computers.

## Current state (newest first)

- **Cat constructor takes the skills as a dictionary** (user's idea,
  2026-11-01): `new Cat(name, rank, sex, ageMoons, diligence, skills, mother,
  father)` with `skills` a `Dictionary<SkillType, Skill>`. Skills that aren't
  in it get a default (see `Cat.Skills`), and the cat copies the `Skill`
  objects so two cats never share one. `StartingClans.cs` now has one block
  per cat with all its skills in it (the user may clean this up later).
  `Cat.SetSkill` is commented out because nothing uses it; the user hasn't
  decided yet whether a tool will need it. Balance runs identical
  before/after.
- **The simulation can be watched in a 2D camp** (2026-10-29 to 10-30).
  Goal (user): watch the running simulation, camera following a chosen cat,
  no player yet. Later it must play like an RPG: you are one cat, the rest
  is simulated and must feel alive.
  - *Simulation side:* `cat.Activity` (what the cat did last tick: InCamp,
    Sleeping, Resting, Hunting, Training, GatheringHerbs, Caring; where in
    camp comes from `cat.Den`), `World.PatrolSent` (a `Patrol`: cats,
    training or not, mice), `World.CatAte`, `Clan.CatAdded` /
    `Clan.CatDied`. Logs identical before and after.
  - *Godot side:* `game/camp/camp.tscn` (run with F6) owns the `World` and
    ticks it every `SecondsPerTick` (15 s, a day is a minute; safe to change
    for testing, the results stay the same). After each tick `Camp.cs` plans
    a timetable: every cat takes up its activity at its own random time,
    patrols leave and come back at different times, hungry cats go to the
    pile spread over the tick. All visual, Godot's random, never the
    simulation's. Click a cat to follow it. Every cat is a
    `game/cat/cat_view.tscn` (`CatView.cs`: walking, wandering near its
    spot). `cat_sprite.tscn` + `CatSprite.cs` hold the animations and are
    shared with `Player.tscn`, so new animations are added once.
  - *World at real pixel size:* 32-pixel cats, 16-pixel tiles, the camera
    zooms ×2 instead of scaling sprites. The ground is the `Ground`
    TileMapLayer with placeholder tiles (`assets/tiles/`, 11 tiles: grass,
    undergrowth, sand, dirt, den floor, rock, water, gorse, cliff, tree
    trunk, bramble). The user painted the camp after the books (a ravine,
    Highrock, the dens around the clearing, the gorse tunnel).
  - *Walls and paths:* rock, water, gorse, cliff, tree trunk and bramble
    have a collision in the TileSet. The `Navigation` region bakes the
    walkable area from them when the camp starts (layers in the group
    `navigation_obstacles`), and every cat follows a path
    (`NavigationAgent2D`). A random spot only counts if it is near on foot,
    otherwise cats walked around the whole camp to a spot behind a thin wall.
  - Pelt colours wait until the user has retextured the animations (the
    palettes for the book cats are in `assets/cat/palettes/`).

- **Skill overhaul done (step 4: minimum for the warrior ceremony).** At
  12 moons an apprentice only becomes a warrior with at least
  `MinHuntingForWarrior` (40) hunting (user chose 40 over 50: 50 lifted the
  whole Clan to 60 average, which is "easier hunting for everyone"). A late
  apprentice trains `MotivationPerMoon` (10%) harder per late moon. 17% are
  late, almost always by 1–2 moons, latest at ~19 moons. The bonus barely
  matters (0–15% gave the same results): late apprentices are only a few
  points short and still learn 8–12 points a moon. The long cases come from
  bad luck (no mentor, sickness, famine). Long Ravenpaw-like apprenticeships
  would need a different lever (bad mentors, few lessons), not the bonus.
  New warriors ~56 (weakest 10% 42). `docs/Milestone1.md` describes the new
  skill rules.
- **Training patrols + mentoring as a skill.** `SkillType.Mentoring` (every
  cat starts at `StartingMentoring` 20, the old mentoring gift is its gift)
  replaces `MentoringGift`, `MoonsMentored` and the "inexperienced mentor"
  rule. `Cat.MentorFactor` comes from the mentoring level. **Mentoring never
  fades** (user: a warrior waiting for an apprentice can't practice it, and a
  rusty mentor would never get picked again). Outside a famine the leader
  first sends every available apprentice with a teacher, two pairs per patrol,
  even when no prey is needed (`World.SendTrainingPatrols`): its mentor, or a
  random helper, preferably one without an apprentice. Then normal patrols
  as needed. Each lesson: apprentice hunting + `HuntingLessonGain` (0.158) ×
  teacher's `MentorFactor`, teacher mentoring + `MentoringPracticePerLesson`
  (0.06, helpers half). Camp training `ApprenticeHuntingGainPerMoon` halved to
  5.25. Medicine mentors learn `MedicineMentoringPracticePerMoon`. Log lines
  for training patrols and for cats feeling rusty / back to their old self.
  Results: ~37 lessons per apprentice and moon (26% by helpers), new
  warriors ~54 (best 10% 75+, weakest 10% 36), **1 in 17 cats reaches 90+**
  (was 1 in 29: great mentors make great hunters). The user keeps this
  balance for now, to tune later.
- **Skill overhaul, step 3 of 4 done: use it or lose it.** A skill not
  practiced in a moon loses `SkillFadePerMoon` (1) point, never below
  `SkillFadeFloorShare` (half) of its `Peak`. Practice: going hunting (even
  without a catch), caring or gathering herbs, any learning (so apprentices
  in training never fade). Below its peak a cat relearns `RelearnFactor` (10)
  times faster, "like riding a bike" (user): rusty hunters lose ~2 points and
  are back at their peak after ~3 moons. In practice only elders fade much
  (~6 points), because almost every cat hunts every moon; fading will matter
  more once cats split their time between tasks. No effect on the balance.
- **Balance runner in the repo** (`6bea04b`): `tools/BalanceRunner`, see
  "How to test".
- **Skill overhaul, step 2 of 4 done** (`57d1701`): every caught mouse
  improves hunting by `HuntingPracticePerMouse` (0.002) via `Skill.Improve`.
  Average hunter ~54 at the ceremony, ~65 at 96 moons, gap between the best
  and worst 10% grew from 31 to 37 points, 1 in 29 cats reaches 90+ (was
  1 in 408). The best 20% of hunters catch 36% of the mice (was 34%), so a
  few top hunters don't carry the Clan. Died out 7 → 1 of 100, starvation
  unchanged (the prey limit decides it). The user accepted both for now.
- **Skill overhaul, step 1 of 4 done** (`34adcba`): skill levels are floats,
  every level is harder (`Skill.Improve`: amount × gift factor ×
  (1 − level/100)^`SkillDifficulty`, 0.5). Training gain 10.5 → average new
  warrior ~54 hunting, about 1 in 450 starts with 90+. Medicine apprentice
  gain 7.5.
- **Skill refactor** (`1d76940`): `cat.Skills[SkillType.Hunting].Level` /
  `.Gift`. Proven identical logs before/after. (Setup was `cat.SetSkill(...)`
  then, now the constructor takes the skills, see above.)
- **Unit tests** (`4f6f2d7`): `tests/WarriorsSim.Tests`, xUnit, see CLAUDE.md.
- **Frailty by age, not rank** (`33d523b`, `57a8dca`, `478fe33`): used for
  starvation, sickness recovery, greencough risk, "feed the vulnerable"
  (compassion) and old cats' hunting (`FrailHuntingMultiplier`). Ranks only
  count for the warrior code's eating order.
- **Medicine cats** (main PC, `5be9644`): eat next to the leader, care for
  themselves, two healers wanted, desperation after 6 moons without one.
- Earlier: sickness, injuries, medicine den, herbs, soft birth brake, gifts and
  mentors (see git log and `docs/Milestone1.md`).

Last test (100 seeds × 30 years, Balanced leader, after step 4): 0–1 of 100
Clans died out, ~7.2 starved per year, ~2 sickness deaths per run.

The skill overhaul (steps 1–4, training patrols, mentoring) is finished.
Open for later: 1 in 17 cats reaching 90+ (was 1 in 29 before training
patrols) may need tuning; the user wants to tune balance later.

## Other TODOs (priority order)

1. **Medicine is too strong.** Sickness kills only ~2 cats per run, catmint
   runs out ~12 times per run with almost no deaths. Goal: few deaths with a
   good medicine cat and catmint; a leaf-bare greencough outbreak without
   catmint kills several kits and elders. Ideas: the den isolates too well
   (cats move in at severity 20 before spreading much), medicine cats could
   catch it from patients, catmint rarer (`CatmintLeafBareMultiplier`).
2. **Rule tests for the feeding order**, one per leader style, as design
   statements ("a compassionate leader feeds a young kit before a healthy
   warrior"). Assert relations, not exact scores. The user reviews the code
   first and decides which statements are true.
3. **Update `Milestone1.md` / `Vision.md`** for sickness, medicine cats, herbs,
   the medicine den and the soft birth brake. `Tuning.cs` comments hold the
   reasoning for most values.
4. **Check starvation** (~7 per year). `MinCrowdingFactor` is the main lever.
5. **Later, an explorer** that prints the feeding order with each score part
   (needs `FeedingPolicy.Score` to return its parts) and can sweep a weight.
6. **Next steps for the camp** (none started, the user picks): darker at
   night, a camp log of the important events, speed buttons, prey visible on
   the pile, pelt palettes per cat once the animations are retextured. Bigger
   ones: activities that get their times from the simulation (a patrol to
   Sunningrocks takes longer than one to the Owl Tree, see Vision 6.5),
   Highrock walkable on top for Clan meetings, a sand–grass terrain set
   (autotiling) as a placeholder so the user can learn the terrain brush.
   The camp itself (`camp.tscn`, tiles, markers) is the user's: when asked to
   enlarge or change it, scale their shape, never redesign it.
7. **UI is the user's area, don't change it unasked** (only minimal fixes when
   a refactor breaks the build). Useful properties: `cat.Skills`,
   `cat.Conditions`, `IsSick`, `IsConfined`, `InMedicineDen`, `Mentor`,
   `MentorFactor`, `cat.Skills[SkillType.Mentoring]`, `Skill.Peak`,
   `Clan.HerbStore.Doses(herb)`.
8. **Later ideas the user wants:** warriors becoming medicine cats (like
   Cinderpelt), remembering every apprentice a cat trained, the rule that a
   deputy must have had an apprentice, loners and kittypets joining for new
   blood, "volunteer work" (warriors hunting for sport, practicing skills).
   User 2026-10-29: **diligent cats decide for themselves to practice**
   hunting without a patrol, or ask the leader to come along on the next
   patrol (`Diligence` decides). This is the counterweight to fading once
   cats have more tasks than hunting.

## How to test

- **Unit tests:** `dotnet test tests/WarriorsSim.Tests`.
- **Balance runs:** `dotnet run -c Release --project tools/BalanceRunner --
  100 30 [Name=value ...]` (~25 seconds). Prints Clans dying out, deaths by
  cause, hunting skill over a cat's life, how many reach 90+/95+, and checks
  the targets in `Report.cs`. Add statistics there when a step needs them
  (e.g. greencough cases: `caught greencough`, `turned into greencough`).
  10 seeds are too noisy (±4 die-outs per 100 is
  normal).
- **Pure refactors:** save the full log (every level) of a few seeds before
  the change and compare it byte by byte afterwards. Same seed must give the
  same log.
- Ask before many long runs when the user is on battery.
- **The camp:** run `game/camp/camp.tscn` (F6 in Godot). Debug → Visible
  Navigation shows the walkable area. For automated checks, a temporary
  GDScript (`--path . -s test.gd`) can load the camp, read the cat views and
  save screenshots; delete it afterwards. GDScript can't read the C# `Cat`
  object, use the name label (`%NameLabel`) instead.

## Design decisions by the user

- Sickness: named sicknesses from the books, contagious within dens. Hunting
  injuries, one severity each (health per limb later).
- Medicine cat is its own career. Healing gift is rare. Warriors learn some
  healing as patients. Medicine cats eat next to the leader, care for
  themselves, two healers whenever possible, never more than one apprentice.
- Population: soft birth brake, hunger decides.
- Frailty depends on age, not rank. Compassion protects the truly weak
  (queens, the sick, the frail); ranks are only for the warrior code. Old cats
  hunt worse but mentor as well as before.
- Skills: floats, every level harder, but 90+ must be reachable for the very
  best. Learning by doing per caught mouse. Decay down to a floor, and a
  faded skill comes back quickly ("feeling rusty after a sickness", not a
  RimWorld-like cap; "hunting for a cat is like riding a bike"). Mentoring
  is a skill learned by teaching and never fades. Apprentices learn on
  training patrols with their mentor or a helper, except in a famine.
  Apprentices stay apprentices until they reach the minimum (40), which every
  cat must be able to reach.
- Balance doesn't have to be perfect before committing; commit working steps.
- Observing first, no player cat yet, but it must later play like an RPG
  (one cat, the rest simulated and alive). The camera follows a chosen cat.
  The camp is its own scene, one tick takes 15 seconds.
- Art: 16×16 tiles, real pixel size (32-pixel cats), the camera zooms.
  Blocking tiles: rock, water, gorse, cliff, tree trunk, bramble.
- Code style: tabs (in `.editorconfig`).

Proposed by Claude, not answered by the user yet:

- The world: one connected forest without loading screens, built from one
  scene per territory; separate scenes only for special places (Moonstone,
  Twolegplace). No streaming until it's needed. Open: how big a territory
  should feel (one or five minutes to cross), hand-drawn or generated maps.
- Time: 4 ticks per day stay the planning rhythm. Varied timings come from
  activities with their own start and duration, not from more ticks. A real
  world to walk in will need much longer days (10–20 minutes, like
  Stardew). Speed (seconds per tick) can change freely; the same seed must
  give the same story at every speed.
