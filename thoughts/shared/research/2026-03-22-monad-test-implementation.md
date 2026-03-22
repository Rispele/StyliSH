---
date: 2026-03-22T00:00:00+03:00
researcher: Claude
git_commit: 591b818b7ff55fa8269da4a0bf3e4e6dd289fade
branch: master
repository: StyliSH
topic: "Research codebase for implementing monad and transformer tests"
tags: [research, codebase, monads, transformers, testing]
status: complete
last_updated: 2026-03-22
last_updated_by: Claude
---

# Research: Implementing Tests for Monads and Transformers

**Date**: 2026-03-22
**Researcher**: Claude
**Git Commit**: 591b818b7ff55fa8269da4a0bf3e4e6dd289fade
**Branch**: master
**Repository**: StyliSH

## Research Question
What needs to be tested in the StyliSH monad library, and how should the test project be structured?

## Summary

StyliSH has 4 concrete monad/transformer types that need testing: `IdMonad`, `Either`, `TaskMonad`, and `EitherT`. No test infrastructure exists yet. The project targets .NET 10, so xUnit v3 + FluentAssertions would be the natural choice. Each monad should be tested for the three monad laws (left identity, right identity, associativity) plus type-specific behavior.

## Detailed Findings

### Types to Test

| Type | Marker | File | Key Behaviors |
|------|--------|------|---------------|
| `IdMonad<TValue>` | `IdMarker` | `StyliSH.Implementations/Monads/Id/IdMonad.cs` | Pure wraps value; Map/Bind apply function directly |
| `Either<TError, TValue>` | `EitherMarker<TError>` | `StyliSH.Implementations/Monads/Either/Either.cs` | Left (error) short-circuits; Right (success) chains; Match dispatches |
| `TaskMonad<TValue>` | `TaskMarker` | `StyliSH.Implementations/Monads/Tasks/TaskMonad.cs` | Wraps `Task<T>`; async Map/Bind via ContinueWith |
| `EitherT<TOuterMarker, TError, TValue>` | `EitherTMarker<TOuterMarker, TError>` | `StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs` | Stacks Either on any outer monad; Run unwraps |

### Monad Laws (apply to ALL 4 types)

1. **Left Identity**: `Pure(a).Bind(f) ≡ f(a)`
2. **Right Identity**: `m.Bind(Pure) ≡ m`
3. **Associativity**: `m.Bind(f).Bind(g) ≡ m.Bind(x => f(x).Bind(g))`

### IdMonad Test Cases
- `Pure(42)` produces `IdMonad` with `Value == 42`
- `Map(x => x + 1)` transforms the value
- `Bind` chains computations
- Three monad laws
- `MonadWrapper` round-trip: wrap → unwrap via implicit cast

### Either Test Cases
- `FromValue(v)` creates Right
- `FromError(e)` creates Left
- `Match` dispatches to correct branch
- `Map` on Right transforms; on Left short-circuits
- `Bind` on Right chains; on Left short-circuits
- Three monad laws (for the Right path)
- `EitherMarker.Pure` creates a Right value
- `EitherMarker.Match` static helper works correctly

### TaskMonad Test Cases
- `Pure(v)` creates completed task with value
- `Map` transforms async result
- `Bind` chains async computations
- Three monad laws (async versions)
- `MonadWrapper` round-trip

### EitherT Test Cases
- `FromValue(v)` lifts into outer monad as Right
- `FromError(e)` lifts into outer monad as Left
- `Map` on success transforms; on error short-circuits
- `Bind` on success chains; on error short-circuits
- `Run` unwraps to outer monad containing Either
- Three monad laws
- Test with `IdMarker` as outer (synchronous, easy to assert)
- Test with `TaskMarker` as outer (async, realistic usage)

### MonadWrapper Test Cases
- `Map` delegates to inner monad
- `Bind` (both overloads) delegates correctly
- Implicit cast back to concrete type works

### Infrastructure Needed

**Test project setup:**
```
StyliSH.Tests/
  StyliSH.Tests.csproj  (xunit, net10.0, references Abstractions + Implementations)
  Monads/
    IdMonadTests.cs
    EitherTests.cs
    TaskMonadTests.cs
  Transformers/
    EitherTTests.cs
  MonadWrapperTests.cs
```

**NuGet packages:**
- `Microsoft.NET.Test.Sdk`
- `xunit` (v3)
- `xunit.runner.visualstudio`
- `FluentAssertions` (optional but recommended)

**Project reference:**
- `StyliSH.Abstractions`
- `StyliSH.Implementations`

### Key Implementation Notes

1. **Implicit cast pattern**: Each monad implements `IMonadUnwrapper` with an implicit operator from `MonadWrapper` → concrete type. Tests should verify this round-trip works.

2. **MonadWrapper vs IMonad**: `Map` and `Bind` on `IMonad` return `MonadWrapper`. The wrapper can then be implicitly cast back. Tests should exercise both the wrapper-returning and raw-returning paths.

3. **Either internal constructor**: `Either.Success` is `internal`, but `Pure` and `FromValue`/`FromError` are public. Tests should use the public API.

4. **TaskMonad assertions**: Since `TaskMonad` wraps `Task<T>`, tests need to `await` the inner task. Extract via `((TaskMonad<T>)result).Value` then await.

5. **EitherT.Run**: Returns the outer monad containing `Either<TError, TValue>`. For `IdMarker`, that's `IdMonad<Either<E, V>>`. For `TaskMarker`, it's `TaskMonad<Either<E, V>>`.

6. **Generic test helpers**: Consider a helper method that verifies monad laws generically given a `Pure` function and sample `Bind` functions. This avoids duplicating law tests across each monad.

## Code References
- `StyliSH.Abstractions/Monads/IMonadMarker.cs:6-10` - IMonadMarker with static abstract Pure
- `StyliSH.Abstractions/Monads/IMonad.cs:6-21` - IMonad with Map, Bind, RawMap, RawBind
- `StyliSH.Abstractions/Monads/IMonad.cs:26-35` - IMonadUnwrapper with implicit cast
- `StyliSH.Abstractions/Monads/MonadWrapper.cs:6-24` - MonadWrapper record struct
- `StyliSH.Implementations/Monads/Id/IdMonad.cs:5-29` - IdMonad + IdMarker
- `StyliSH.Implementations/Monads/Either/Either.cs:6-41` - Either + factory methods
- `StyliSH.Implementations/Monads/Either/EitherMarker.cs:5-17` - EitherMarker with Match helper
- `StyliSH.Implementations/Monads/Tasks/TaskMonad.cs:5-37` - TaskMonad + TaskMarker
- `StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs:8-59` - EitherT transformer
- `StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs:7-17` - EitherTMarker

## Architecture Insights
- The marker type pattern with `static abstract` methods means `Pure` is called as `TMarker.Pure(value)` — tests should verify this works for each marker.
- `MonadWrapper` is a record struct, meaning equality comparison is by value — useful for assertions.
- `Either` uses a private constructor pattern with public static factory methods (`FromValue`, `FromError`) and an internal `Success` — tests should only use the public API.
- `EitherT` is the most complex type: it stores `IMonad<TOuterMarker, IMonad<EitherMarker<TError>, TValue>>` internally. The `Run` method downcasts to extract `Either` from the inner layer.

## Open Questions
- Should tests use xUnit v2 or v3? (v3 is the latest, but v2 has broader tooling support)
- Should FluentAssertions be added, or stick with xUnit's built-in Assert?
- Should there be a shared test helper for verifying monad laws generically, or keep tests per-type for clarity?
