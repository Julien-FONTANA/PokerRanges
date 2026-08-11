# What this changes

<!-- The diff shows the what. Use these few lines for the why. -->

Closes #

## Checklist

- [ ] `dotnet test` is green locally (CI runs the same suite on Windows)
- [ ] Behaviour changes come with a test, named as a sentence describing the behaviour
- [ ] The layering holds: no poker rules in `App`, no UI knowledge in `Core` or `Data`
- [ ] New comments are in French and explain *why*, not *what*
- [ ] New user-facing strings go through `Language.Pick(english, french)`

## If it touches the advice engine

- [ ] Advice is still deterministic — same situation, same answer
- [ ] Any new randomness is seeded from `PostflopOptions.RandomSeed`

## If it adds or changes a chart

- [ ] `source` says honestly where the ranges come from
- [ ] No `Fold` action is written — folding is the remaining weight
