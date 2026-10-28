# Handover – State and TODO (2026-10-28)

For the next agent, on any machine. Read `CLAUDE.md` first. This file holds
what's in progress, what's planned and the user's decisions, because the
agents' own memory doesn't move between computers.

## Current state (newest first)

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

Last test (100 seeds × 30 years, Balanced leader, after step 1): 6 of 100
Clans died out, ~5 kits and ~2 elders starve per year, ~2 sickness deaths per
run.

## Skill overhaul: next steps

One step at a time, a 100-seed test after each, commit when the user agrees.

2. **Learning by doing (next).** Hunting practice **only per caught mouse**
   (user's choice). Same curve via `Skill.Improve`. Risk: the leader sends the
   best hunters first, so they get even better. Monitor whether a few hunters
   carry the Clan. Measure how top hunters develop over their life: the gap
   between weak and strong should grow again (the curve narrowed it).
3. **Use it or lose it.** Skills decay without practice, but only **down to a
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

**Open question for the user:** "the mentor will learn a lot while having an
apprentice": should mentoring become a skill learned by teaching (replacing
`MentoringGift` + `MoonsMentored`)? Likely yes, confirm first.

After step 4: update the training rules in `docs/Milestone1.md` (they still
describe integer skills and "parts of a point are kept").

## Other TODOs (priority order)

1. **Medicine is too strong.** Sickness kills only ~2 cats per run, catmint
   runs out ~12 times per run with almost no deaths. Goal: few deaths with a
   good medicine cat and catmint; a leaf-bare greencough outbreak without
   catmint kills several kits and elders. Ideas: the den isolates too well
   (cats move in at severity 20 before spreading much), medicine cats could
   catch it from patients, catmint rarer (`CatmintLeafBareMultiplier`).
2. **Balance runner into the repo** (`tools/BalanceRunner/`). The 100-seed
   test program only exists in a scratchpad on the laptop. Put it in the repo
   with written-down targets (e.g. at most 15/100 died out, at most 8 starved
   per year). See "How to test".
3. **Rule tests for the feeding order**, one per leader style, as design
   statements ("a compassionate leader feeds a young kit before a healthy
   warrior"). Assert relations, not exact scores. The user reviews the code
   first and decides which statements are true.
4. **Update `Milestone1.md` / `Vision.md`** for sickness, medicine cats, herbs,
   the medicine den and the soft birth brake. `Tuning.cs` comments hold the
   reasoning for most values.
5. **Check starvation** (~7 per year). `MinCrowdingFactor` is the main lever.
6. **Later, an explorer** that prints the feeding order with each score part
   (needs `FeedingPolicy.Score` to return its parts) and can sweep a weight.
7. **UI is the user's area, don't change it unasked** (only minimal fixes when
   a refactor breaks the build). Useful properties: `cat.Skills`,
   `cat.Conditions`, `IsSick`, `IsConfined`, `InMedicineDen`, `Mentor`,
   `MentoringGift`, `MoonsMentored`, `Clan.HerbStore.Doses(herb)`.
8. **Later ideas the user wants:** warriors becoming medicine cats (like
   Cinderpelt), remembering every apprentice a cat trained, the rule that a
   deputy must have had an apprentice, loners and kittypets joining for new
   blood, "volunteer work" (warriors hunting for sport, practicing skills).

## How to test

- **Unit tests:** `dotnet test tests/WarriorsSim.Tests`.
- **Balance runs:** a console project that compiles
  `scripts/Simulation/**/*.cs` and runs 100 seeds × 30 years (~3 minutes, run
  in the background). Count from the log callback: Clans dying out, deaths by
  cause (`starved to death`, `died of …`, skipping the moon summary lines),
  greencough cases (`caught greencough`, `turned into greencough`), the
  hunting skill of new warriors (`(hunting N` in the ceremony line). 10 seeds
  are too noisy (±4 die-outs per 100 is normal).
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
