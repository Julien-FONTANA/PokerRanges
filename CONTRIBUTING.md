# Contributing to PokerRanges

Thanks for taking an interest. This is a small, opinionated project, so this
document tries to be specific rather than generic: what to work on, how the code
is organised, and the conventions that are easy to miss.

**The repository is written in English** — code, comments and documentation
alike. The **user interface** is a separate matter: it ships in both French and
English, and that is a product feature, not a leftover.

## Getting set up

You need the **.NET 10 SDK**. Nothing else — no database, no services, no
API keys.

```bash
git clone https://github.com/Julien-FONTANA/PokerRanges.git
cd PokerRanges
dotnet test          # 372 tests, all should pass
dotnet run --project src/PokerRanges.App
```

If `dotnet test` is red on a clean checkout, that is a bug — please open an
issue rather than working around it.

## Where the value is

If you want to help but do not know where to start, these are the gaps that
matter most, roughly in order of impact per hour spent:

1. **Preflop charts.** The biggest limitation is data, not code. Several
   situations have no chart of their own and fall back to a neighbouring one:
   facing a 3-bet, facing a 4-bet, squeeze, and facing limps. Depths are covered
   only at 10, 25 and 100bb. Adding charts is pure JSON — see below.
2. **ICM.** Every expectation is computed in chips. Near a bubble or a final
   table that is the wrong currency. This is the largest conceptual gap.
3. **Side pots.** Unequal stacks are modelled, but a multiway all-in showdown
   does not yet split the pot into several parts.
4. **Multi-street planning.** Expectation is computed as though the hand ended
   on the current street.

Smaller, self-contained tasks: cross-checking the equity engine against
published reference numbers, and property-based tests over the betting replay
(pot conservation, no negative stacks).

## Adding or fixing a preflop chart

Charts live in `src/PokerRanges.Data/Charts/*.json`. They are embedded in the
assembly and copied on first run to `%APPDATA%\PokerRanges\charts\`, where the
user can edit them.

```json
{
  "source": "Where these ranges come from — be honest about it",
  "charts": [
    {
      "context": "VersusThreeBet",
      "playersLeftToAct": 3,
      "relation": "InPosition",
      "depthInBigBlinds": 40,
      "actions": [
        { "kind": "Raise", "sizeInBigBlinds": 22, "range": "QQ+, AKs, A5s" },
        { "kind": "Call", "range": "99-JJ, AQs, KQs, AKo" }
      ]
    }
  ]
}
```

Rules worth knowing:

- **Never write a `Fold` action.** Folding is whatever weight is left once the
  other actions are subtracted, so no hand can be forgotten. A chart that
  declares `Fold` is rejected at load time.
- `context` must match a value of `PreflopContext`, `relation` a value of
  `FacingRelation`.
- `playersLeftToAct` is how many players act after the hero on the first betting
  round. It is the dimension that really indexes an opening chart: opening with
  three players behind is the same problem at a five-handed and an eight-handed
  table.
- Say where the ranges come from in `source`. "Adapted from a solver output at
  40bb" and "my own feel" are both fine answers; pretending is not.

Chart files are validated by tests in `tests/PokerRanges.Data.Tests` — a
malformed range or an unknown context fails the build.

## How the code is laid out

```
src/PokerRanges.Core    The domain. Cards, ranges, evaluator, equity, pot
                        engine, preflop and postflop advice.
src/PokerRanges.Data    Chart loading and persistence.
src/PokerRanges.App     Avalonia + MVVM.
```

The layering is strict and worth preserving:

- **Core knows nothing about the UI** and depends only on logging abstractions.
- **Data knows nothing about the UI.**
- **App contains no poker rules.** If you find yourself writing poker logic in a
  view model, it belongs in Core.

## Conventions

- **Comments explain *why*, not *what*.** A comment restating the code will be
  asked to go; a comment explaining a decision that would otherwise look
  arbitrary is welcome. English, like the rest of the repository.
- **Warnings are errors** (`TreatWarningsAsErrors`), and code style is enforced
  at build time. Nullable reference types are on.
- **Test names are sentences describing behaviour**, not method names:
  `AShortStackedButtonIsToldToJam`, not `TestJam`. Read a few before adding
  yours.
- **The engine must stay deterministic.** Monte-Carlo draws use a fixed seed, so
  the same situation always yields the same advice. If you add randomness,
  seed it from `PostflopOptions.RandomSeed`.
- **New UI strings need both languages.** They go through
  `Language.Pick(english, french)` — there is no fallback that silently ships
  one language.

## Tests

Every behaviour change needs a test. The suite is fast and there is no reason
not to.

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"   # if you want the coverage numbers
```

One test asserts a wall-clock budget — compact mode must answer in under a
second. It runs uninstrumented in CI, in a pass separate from coverage
collection, because instrumentation makes it time the profiler rather than the
application. If you touch the advice pipeline, watch that one.

## Pull requests

- Branch off `main`.
- Keep the change focused. A PR that fixes a bug and reformats three files is
  hard to review and harder to revert.
- Make sure `dotnet test` is green before opening it. CI runs the same thing on
  Windows, so a local failure is a CI failure.
- Explain *why* in the description. The what is visible in the diff.

Commit messages are written in the imperative and explain the reasoning behind
the change, not just its shape. Commits before August 2026 are in French, from
when the whole project was; new ones are in English like everything else.

## Reporting things

- A bug, or advice that looks wrong: open an issue using the matching template.
  For wrong advice, the template asks for the full situation (stacks, position,
  action history, board, opponent profile) — without it the result cannot be
  reproduced, because advice depends on all of it.
- A security issue: do **not** open an issue. See [SECURITY.md](SECURITY.md).
