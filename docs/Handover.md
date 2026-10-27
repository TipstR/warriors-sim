# Handover – State and TODO (2026-10-27)

Written at the end of a work session on the laptop, so work can continue on
another machine with another agent. Read `CLAUDE.md` first.

## What was done in this session

1. **Docs brought up to date** (commit `09de888`). `Milestone1.md` describes
   the rules as they are in the code, not the original plan.
2. **Gifts and mentors** (commit `17bfd81`).
   - Every kit is born with a hunting gift and a mentoring gift (0–100,
     50 = average): half from the parents, half luck.
   - Apprentices learn from a mentor. Learning speed = gift × mentor quality,
     and mentor quality = mentoring gift × experience (moons mentored).
   - New warriors now range from below 20 to 100 hunting, instead of all 48.
   - The base training gain was raised from 8 to 8.7, because the Clan's
     survival is very sensitive to the *average* hunting skill.
3. **Sickness, injuries, medicine cats, medicine den, herbs** (this commit,
   not documented in `Milestone1.md` yet):
   - `Condition.cs`: one sickness or injury. Severity races against recovery:
     at recovery 100 the cat heals, at severity 100 it dies.
   - `World.Health.cs`:
     - whitecough and greencough, contagious within the same den (`Den.cs`)
     - whitecough can turn into greencough
     - hunting injuries (sprain, thorn, fall…) that can get infected
   - `World.Medicine.cs`:
     - medicine cat career (new ranks `MedicineCatApprentice`, `MedicineCat`;
       they don't hunt, mate or have kits)
     - a rare healing gift (most cats 0–30), healing skill
     - choosing an apprentice (best healing gift, only when the Clan needs one)
     - care: the best healers care for the sickest; warriors learn a little
       healing as patients
     - medicine den with 5 nests (the sickest get them; patients rest and are
       isolated)
     - herb store (`Herb.cs`): tansy, catmint, cobwebs, marigold; gathered,
       seasonal, spoiling; catmint is very rare in leaf-bare
   - Spottedleaf starts as medicine cat. Cinderkit has a high healing gift.
   - Sick cats hunt worse and eat earlier under the warrior code. Very sick
     cats (severity ≥ 60) can't hunt.
4. **Soft birth brake** (`Tuning.MinCrowdingFactor` = 0.2). The crowding brake
   used to stop all births in a full Clan. Once medicine cats kept everyone
   alive, about half of all Clans grew old together and died out. Now births
   never stop completely, and hunger thins the Clan instead.

The user's design decisions for this feature are in the last section.

## Test results (100 seeds × 30 years, Balanced leader)

| Version                         | Died out | Starved/year | Sickness deaths/run |
|---------------------------------|----------|--------------|---------------------|
| Gifts & mentors, no sickness    | 20       | ~1.3         | –                   |
| Sickness, no medicine cat       | 6–8      | ~3           | ~70                 |
| Medicine cats, hard birth brake | 50–59    | ~2.5         | 1–14                |
| **Now** (soft brake 0.2)        | **12**   | **~7**       | **~2**              |
| Soft brake 0.3                  | 4        | ~11          | ~2                  |
| Soft brake 0.15                 | 16       | ~5           | ~1.5                |

## TODO for the next agent

In order of priority:

1. **The user has to push from the laptop first.** `git push origin master`
   failed there because the saved GitHub login was invalid. Without the push,
   the main PC doesn't have these commits.
2. **Commit the `.uid` files.** Godot creates them when it opens the project,
   for `Condition.cs`, `Den.cs`, `Herb.cs`, `World.Health.cs` and
   `World.Medicine.cs`.
3. **Medicine is too strong.** Sickness kills only ~2 cats per run, and
   catmint runs out ~12 times per run with almost no deaths. Goal: with a good
   medicine cat and catmint, few die; a greencough outbreak in leaf-bare
   without catmint kills several kits and elders.
   - The den isolates too well. Sick cats move in at severity 20, often
     before they've spread anything. Ideas: contagious earlier, medicine cats
     catch it from patients and spread it, or a weaker isolation.
   - Make catmint run out for real in bad winters. Maybe lower
     `CatmintLeafBareMultiplier`, or let herb stocks spoil faster.
   - Later: the medicine cat stocks up in leaf-fall (a personality decision).
4. **Update `docs/Milestone1.md` and `docs/Vision.md`** for everything in
   point 3 of the summary and for the soft brake. The rule descriptions there
   end with gifts and mentors. The comments in `Tuning.cs` hold the reasoning
   for most values.
5. **Check starvation (~7 cats per year, ~3 of them kits).** The user chose
   "hunger decides" over a managed birth rule, but the amount should feel
   right. `MinCrowdingFactor` is the main lever. With the Balanced leader, weak
   warriors are fed after apprentices and elders, so they starve too.
6. **Medicine cat apprentices die often** (~8 apprentices per run for 2–3
   medicine cats). Find out why (starvation?) and whether it's wanted.
7. **Healing skill reaches 100 quickly** through practice
   (`HealingPracticePerCare`). Maybe slow it down.
8. **UI (the user does UI themselves, don't change it unasked).** Useful
   properties:
   - `Cat`: `Conditions` (each has `Name`, `Severity`, `Recovery`, `Type`),
     `IsSick`, `IsConfined`, `InMedicineDen`, `Healing`, `HealingGift`,
     `HuntingGift`, `MentoringGift`, `Mentor`, `MoonsMentored`
   - `Clan.HerbStore.Doses(herb)`
9. **Later ideas the user wants:**
   - warriors becoming medicine cats (like Cinderpelt)
   - remembering every apprentice a cat trained ("I trained him")
   - the warrior code rule that a deputy must have had an apprentice
   - loners and kittypets joining for new blood (small Clans run out of
     possible mates)

## How to test

Build a console project outside the repo that compiles
`scripts/Simulation/*.cs`. The simulation has no Godot dependency. Count
these from the log callback (summary lines, except where noted):

- Clans dying out
- deaths by cause: lines with `starved to death` and `died of …` (skip the
  moon summary lines, which also contain "died of")
- greencough cases: `caught greencough` and `turned into greencough`
- whitecough cases: `caught whitecough` (detail level)
- the hunting skill of new warriors, from `(hunting N` in the warrior ceremony
  line

Use 100 seeds × 30 years for balance decisions. One run takes ~3 minutes, and
10 seeds are too noisy (±4 die-outs per 100 is normal noise). Ask before many
long runs when the user is on battery.

## Design decisions by the user (sickness and medicine)

- Named sicknesses from the books, contagious within dens.
- Hunting injuries for now. One severity per injury, health per limb comes
  later.
- The medicine cat is its own career, like the books.
- Healing gift: **rare**. The kit with the best gift becomes apprentice when
  the Clan needs one.
- Warriors can learn some healing, especially cats often in the medicine den.
  Without a medicine cat, the best of them gives basic care.
- Herb store with seasons. Medicine den with limited nests, sickest first.
- Population: **soft brake, hunger decides**.
- Commit only once balanced. The user asked for this commit at the end of the
  session anyway, with medicine still too strong (TODO 3).
