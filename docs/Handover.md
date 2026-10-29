# Handover – State and TODO (2026-10-29)

For the next agent, on any machine. Read `CLAUDE.md` first. This file holds
what's in progress, what's planned and the user's decisions, because the
agents' own memory doesn't move between computers.

## Current state (newest first)

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
  `.Gift`, `cat.SetSkill(...)` for setup. Proven identical logs before/after.
- **Unit tests** (`4f6f2d7`): `tests/WarriorsSim.Tests`, xUnit, see CLAUDE.md.
- **Frailty by age, not rank** (`33d523b`, `57a8dca`, `478fe33`): used for
  starvation, sickness recovery, greencough risk, "feed the vulnerable"
  (compassion) and old cats' hunting (`FrailHuntingMultiplier`). Ranks only
  count for the warrior code's eating order.
- **Medicine cats** (main PC, `5be9644`): eat next to the leader, care for
  themselves, two healers wanted, desperation after 6 moons without one.
- Earlier: sickness, injuries, medicine den, herbs, soft birth brake, gifts and
  mentors (see git log and `docs/Milestone1.md`).

Last test (100 seeds × 30 years, Balanced leader, after step 2): 1 of 100
Clans died out, 7.7 starved per year, ~2 sickness deaths per run.

## Skill overhaul: next steps

One step at a time, a 100-seed test after each, commit when the user agrees.

3. **Use it or lose it (next).** Skills decay without practice, but only **down to a
   floor** (e.g. half the best level the cat ever had). **Not for apprentices
   in training** (training counts as practice), otherwise a weak learner could
   never reach the ceremony minimum.
4. **Minimum skill for the warrior ceremony** (40 or 50, tunable). Apprentices
   **stay apprentices until good enough**, no exception. User: "the minimum
   must be achievable by any cat". Counterweight: a **motivation bonus** for
   apprentices past the warrior age (e.g. +15% learning per extra moon,
   `MotivationPerMoon`, plus a log line like "Ravenpaw trains harder than
   ever"). The user doesn't want to tune around a target length: test first
   and see whether anyone gets close to a long apprenticeship. Measure the age
   at the ceremony (average, max) and how many cats reach 90+/95+ per skill.

**Mentoring becomes a skill** (user confirmed: "mentoring must be a skill"),
learned by teaching, replacing `MentoringGift` + `MoonsMentored`. Its own
step after step 4, unless it is combined with training patrols (below).

**Under discussion: training patrols.** The user wants the leader to send
mentors with their apprentices (two pairs per patrol) when no patrol is
needed. Measured: a patrol is needed in 98–100% of daytime ticks, because the
Clan always lives at its food limit, so that rule would almost never fire.
Proposed instead: see the conversation of 2026-10-29 / the user's answer.

After step 4: update the training rules in `docs/Milestone1.md` (they still
describe integer skills and "parts of a point are kept").

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
6. **UI is the user's area, don't change it unasked** (only minimal fixes when
   a refactor breaks the build). Useful properties: `cat.Skills`,
   `cat.Conditions`, `IsSick`, `IsConfined`, `InMedicineDen`, `Mentor`,
   `MentoringGift`, `MoonsMentored`, `Clan.HerbStore.Doses(herb)`.
7. **Later ideas the user wants:** warriors becoming medicine cats (like
   Cinderpelt), remembering every apprentice a cat trained, the rule that a
   deputy must have had an apprentice, loners and kittypets joining for new
   blood, "volunteer work" (warriors hunting for sport, practicing skills).

## How to test

- **Unit tests:** `dotnet test tests/WarriorsSim.Tests`.
- **Balance runs:** `dotnet run -c Release --project tools/BalanceRunner --
  100 30 [Name=value ...]` (~25 seconds). Prints Clans dying out, deaths by
  cause, hunting skill over a cat's life, how many reach 90+/95+, and checks
  the targets in `Report.cs`. Add statistics there when a step needs them
  (e.g. greencough cases: `caught greencough`, `turned into greencough`; age
  at the ceremony for step 4). 10 seeds are too noisy (±4 die-outs per 100 is
  normal).
- **Pure refactors:** save the full log (every level) of a few seeds before
  the change and compare it byte by byte afterwards. Same seed must give the
  same log.
- Ask before many long runs when the user is on battery.

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
  best. Learning by doing per caught mouse. Decay down to a floor.
  Apprentices stay apprentices until they reach the minimum, which every cat
  must be able to reach.
- Balance doesn't have to be perfect before committing; commit working steps.
