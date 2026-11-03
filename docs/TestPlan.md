# Test Plan – Closing the Gaps Found by Mutation Testing

Started 2026-11-02. For the next agent: read `CLAUDE.md` first. This file
says what the unit tests should cover, how to check them with mutation
testing, and what is done. Update the status table when you finish a
package.

## Why

The first mutation run (2026-11-02, before this plan) found only **13%**
of the code changes: 173 of 1345. The files with recent tests were fine
(`FreshKillPile` 88%, `Prey` 86%, `Skill` 76%). The rest had big gaps:
`CatchChance` (the heart of "skill matters") had no test, `FeedingPolicy`,
`Condition` and `HerbStore` had none at all, and one test
(`Clan_TellsAboutNewAndDeadCats`) passed by accident when the "already
dead?" check was turned around.

## How to check the tests

- Unit tests: `dotnet test tests/WarriorsSim.Tests`.
- Mutation testing (Stryker.NET, pinned in
  `tools/MutationTesting/dotnet-tools.json`, installed on first run):

  ```
  powershell -ExecutionPolicy Bypass -File tools/MutationTesting/run.ps1                           # all, ~4-5 min
  powershell -ExecutionPolicy Bypass -File tools/MutationTesting/run.ps1 Cats/Cat.cs -Survivors    # one file, ~1 min
  powershell -ExecutionPolicy Bypass -File tools/MutationTesting/run.ps1 -LastReport -Survivors    # show last report again
  ```

  It prints a score per file and, with `-Survivors`, every change no test
  found ("survived": a test ran it but didn't notice; "no test": no test
  even runs that line). The HTML report shows them inside the code.
- How it works: Stryker needs a separate project to mutate, but the test
  project compiles the simulation files directly. So
  `tools/MutationTesting/Sim` builds the simulation as a library and
  `tools/MutationTesting/Tests` compiles the real test files against it.
  New test files are picked up automatically. `Tuning.cs` (only numbers)
  and changed text (log messages) are not mutated.
- Stryker 5.x needs a newer .NET SDK than 8, so 4.8.1 stays pinned until
  the project moves to a newer SDK.
- A full run takes several minutes. Ask before running it when the user is
  on battery; prefer single files then.

## How to write the tests

- **Test rules, not formulas.** A test is a statement about the design:
  "a better hunter catches more", "a starving cat eats before a full one".
  Don't pin exact values that come from balancing ("frailty at 97 moons is
  0.0238"): those tests break on every balance change. Use relations
  (more, less, between) and the Tuning values themselves.
- **One exception: the boundaries of a rule.** "Can't die below the safe
  stacks, always dies at `StarvationStacksToDie`" checks both sides of the
  limit, using the Tuning value, not a copied number.
- **To check that a part of a formula exists without pinning it**, change
  that Tuning value in the test and compare, e.g. "an average-diligence cat
  hunts the same with or without `CatchChancePerDiligencePoint`".
- Every test class calls `TuningSettings.ResetToDefaults()` in its
  constructor. Tables (`FoodNeedByAge`, `PreyKinds`, `LitterSizeWeights`)
  are not reset by it, so never change them in a test.
- Test files mirror the simulation folders (`Cats/`, `Clan/`, `Health/`).
  Names: `Method_WhatShouldHappen`.
- **Don't chase 100%.** Ignore a surviving change when it makes no real
  difference ("equivalent"):
  - `<` vs `<=` on floats that are never exactly equal (`Energy < 1f`,
    `IsEaten`), or where both sides give the same result (`Level >= cap`
    then `Math.Min(cap, ...)`).
  - `ToString`, log text, `StartingClans` (data), `CatNames` lists.
  - Most of `World*.cs` for now, see phase 2.
  When unsure, ask: "if someone made this mistake for real, would I want a
  test to catch it?"

## Phase 1 – rules of single classes

Status: **planned** / **done** / **needs the user**. Scores are the
mutation score of the file before and after.

| # | Package | Test file | Status | Score |
|---|---|---|---|---|
| 1 | Clan: deaths, mates, leader, eating order tie | `Clan/ClanTests.cs` | **done** | 20% → 100% |
| 2 | Cat: catch chance, energy, health | `Cats/HuntingTests.cs` | **done** | Cat.cs 28% → 97% (2–5 together) |
| 3 | Cat: food need, eating, starvation | `Cats/HungerTests.cs` | **done** | |
| 4 | Cat: family, pregnancy, names, ranks, dens | `Cats/FamilyTests.cs`, `Cats/RankTests.cs` | **done** | |
| 5 | Cat + Condition: sickness | `Health/ConditionTests.cs`, `Health/SicknessTests.cs` | **done** | Condition 0% → 100% |
| 6 | Herb store | `Health/HerbStoreTests.cs` | **done** | 0% → 92% |
| 7 | Feeding order per leader style | `Clan/FeedingPolicyTests.cs` | written, **needs the user** | 0% → 89%+ |
| 8 | Small gaps: Skill, FreshKillPile, Prey | existing files | **done** | 76/88/86% → 91/94/93% |

### 1. Clan

- Recording a death moves the cat from `Cats` to `Dead` right away and
  tells `CatDied` once.
- A second `RecordDeath` of the same cat changes nothing. Check after the
  *first* call, that's what the old test missed.
- The dead cat's mate is free again (`Mate == null`).
- `Leader`/`Deputy` find the cat with that rank, null if there is none.
- `ApprenticeOf(mentor)` finds only that mentor's apprentice.
- `StarvingCatCount` counts cats from `FamineMinStacks` on.
- Same feeding score: the hungrier cat eats first.

### 2. Hunting (Cat)

- A better hunter catches more. A diligent cat catches more than a lazy one.
- Average diligence is neither bonus nor penalty (compare with
  `CatchChancePerDiligencePoint` = 0).
- A fully frail old cat hunts at `FrailHuntingMultiplier` of a prime cat
  with the same skills.
- A sick cat hunts worse, in proportion to its severity (`HealthFactor`).
- `EnergyFactor`: 1 from `EnergyPenaltyStart` up, 0 at
  `EnergyPenaltyFull` and below, half way gives half (S-curve), a little
  tiredness hurts less than a straight line would.
- A tired cat catches less, an exhausted one nothing.

### 3. Hunger (Cat)

- Food need by age comes from `FoodNeedByAge`, interpolated between points,
  first/last value outside the table.
- Pregnant queens need `PregnancyExtraFoodNeed` more, nursing queens
  `MilkFoodNeedPerKit` per kit more.
- Eating never goes above `SatiationMax`, losing never below 0.
- A meal of exactly one tick's food (`SatiationLossPerTick`) removes a
  starvation stack, less doesn't (the existing test only uses a mouse and a
  shrew, far from the limit).
- Starvation stacks lower `MaxEnergy`; adding one cuts the current energy
  to the new maximum; sleep never goes above it.
- `StarvationDeathChance`: 0 up to `StarvationSafeStacks`, grows above,
  1 at `StarvationStacksToDie`; a frail cat's chance is higher.
- `IsHungry`, `IsStarving`, `IsTired`, `IsWeakFromHunger` at their limits.

### 4. Family and ranks (Cat)

- `IsCloseKinOf`: parents, children, full and half siblings are kin;
  unrelated cats and cats with unknown parents (null == null!) are not.
- Mates: both sides know each other.
- Pregnancy: lasts `PregnancyDays`, `AdvancePregnancy` is true exactly once
  at the birth, the queen is in the nursery only for the last
  `NurseryDaysBeforeBirth` days; not pregnant: nothing happens.
- `IsQueen` for pregnant or nursing cats.
- `CanEatPrey`: young kits only drink milk.
- `ChangeRank` names: kit/paw/star suffixes, warrior suffix only when
  given, deputies and elders keep their name.
- Who can hunt, help in a famine, mentor; which den a cat sleeps in.

### 5. Sickness

- `Condition.Progress`: severity grows by gain × case speed, recovery grows;
  once recovery is full the cat is healing and severity only drops, down to
  cured; severity stops at the maximum = deadly.
- Coughs are contagious until healing, wounds never.
- Cat: `IsSick`, `IsConfined` from `ConfinedSeverity` on, total severity
  capped, `HealthFactor`, `Spreads(type)` only for that sickness.
- `CanCatch`: not while it has it, not until its immunity day has passed,
  other sicknesses unaffected; `ReplaceCondition` gives no immunity.

### 6. Herb store

- Starts empty, adds doses, ignores negative amounts, `TryUse` needs a
  whole dose and takes exactly one, `Spoil` takes the share from every herb.

### 7. Feeding order (needs the user)

Handover TODO 2: rule tests per leader style, as design statements. The
tests are written from what the code does now. **The user reviews the list
in `Clan/FeedingPolicyTests.cs` and decides which statements are true.**
A statement the user rejects means the code changes, not just the test.

### 8. Small gaps

- Skill: gifted cats learn faster; a higher level learns less from the same
  lesson.
- FreshKillPile: `Count` with mixed freshness; of two started pieces the
  fresher is finished first.
- Prey: plural of other kinds ("2 rabbits"), `FormatPortions` uses a dot on
  every computer (the user's PC is German).

## Phase 2 – world rules (not started)

`World` and its partial files hold most of the code, all private. They can
only be tested by building a small Clan by hand, ticking the world and
looking at the result. Do this after phase 1, and keep it to rules that
hold for every seed (run a few seeds in a loop inside the test). Ideas:

- A Clan without prey gets starvation stacks, and famine starts and ends by
  `FamineStarvingCats`.
- An apprentice becomes a warrior at 12 moons only with
  `MinHuntingForWarrior` hunting.
- Training patrols only outside a famine; a teacher is the mentor or a
  helper.
- Medicine cats put the worst patients in the den first; a herb is used
  only for its own condition.
- A cat that got over a sickness can't catch it again during
  `ImmunityDays`.
- Kits are born after `PregnancyDays`; kin never become mates.

Some of this may need small hooks in `World` (e.g. a way to start a world
at a chosen tick or season). Ask the user before changing the simulation
just for tests.

## Results

(Fill in after each package: score before → after, anything surprising.)

- **2026-11-02, paused after packages 1 and 2** (user asked to stop for
  now). 68 tests pass (43 before). `Clan.cs` 20% → 90%; the two left are
  the constructor (only the name) and `OrderByDescending` in
  `CatsInEatingOrder`, which package 7 covers (a test where the scores
  differ). `Cat.cs` was not measured after package 2: run
  `run.ps1 Cats/Cat.cs -Survivors` and continue with package 3.
- **2026-11-03, phase 1 written** (packages 3–8). 217 tests pass. One
  mutation run over the 8 changed files: **96%** (342 of 357), was about
  13% overall. Survivors left on purpose (they make no real difference):
  - `Cat.cs`: `IsExhausted` (`< 1` vs `<= 1`), `Interpolate` at the exact
    table points (both branches give the same value), the `- 0.01f` meal
    limit in `Eat`, `ToString`.
  - `Skill.cs`: `Level >= cap` and `Level > floor` (the `Math.Min`/`Max`
    after them gives the same result).
  - `FreshKillPile.ChooseNext`: `MaxBy` vs `MinBy` among started fresh
    pieces. Two started fresh pieces can't exist (a started piece is always
    finished first), so this can't be tested and doesn't matter.
  - `Prey.cs`: `IsEaten` limit, `First` vs `FirstOrDefault`; `Herb.ToString`.
  - `FeedingPolicy`: `nearDeath` divided vs multiplied. The tests only
    compare cats, and both versions give the same order in every statement.
- **Next:** the user reviews the statements of package 7 (below). Then
  phase 2, or a full run of the tool to see the score over all files.

### Statements for the user to confirm (package 7)

Each is one test in `Clan/FeedingPolicyTests.cs`, written from what the
code does now.

- Balanced: a starving cat eats before anyone who isn't (kits, queens, the
  best hunter in a famine); of two starving cats the one closer to death
  first; a starving cat before one that just ate but is still close to
  death; **an elder before a good hunter, but in a famine the hunter
  first**.
- Compassionate: a starving cat first; kits, queens, the sick and elders
  before the best hunter, a young kit even in a famine; queens and the sick
  before the frail (an elder); a newborn before an older kit.
- Ruthless: the better hunter first; even a starving kit after a weak
  warrior; an elder after every hunter; a hunter too sick to hunt after a
  weak healthy one; apprentices judged by their gift; medicine cats (and
  their apprentices) judged by healing.
- By the code: kits, queens, elders, the sick, leader, deputy, warriors,
  apprentices, even when the last are starving and even in a famine; the
  medicine cat with the leader, its apprentice right after; a sick elder
  still eats as an elder.
