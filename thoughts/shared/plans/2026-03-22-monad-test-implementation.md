# Monad & Transformer Test Suite Implementation Plan

## Context

StyliSH is a C# monad library with 4 concrete types (`IdMonad`, `Either`, `TaskMonad`, `EitherT`) and no test infrastructure. We need to add a test project that verifies monad laws and type-specific behavior for all implementations.

## Current State

- No test project exists
- 4 monad/transformer types in `StyliSH.Implementations`
- .NET 10, solution file at `StyliSH.sln`
- `Either.Success` is `internal` — tests must use public API (`FromValue`/`FromError`/`Pure`)

## Desired End State

A `StyliSH.Tests` NUnit project with:
- Generic monad law verification helpers
- Per-type test classes covering all behaviors
- All tests passing via `dotnet test`

## What We're NOT Doing

- No integration tests or benchmarks
- No test coverage for `StyliSH.Research` (scratch code)
- No CI/CD pipeline setup

## Implementation Approach

**Phase 1**: Create test project + generic monad law helpers
**Phase 2**: Test each monad type (IdMonad, Either, TaskMonad)
**Phase 3**: Test EitherT transformer

---

## Phase 1: Test Project Setup & Generic Law Helpers

### Overview
Create the NUnit test project, add it to the solution, and implement generic monad law verification.

### Changes Required:

#### 1. Create test project
```bash
dotnet new nunit -n StyliSH.Tests -o StyliSH.Tests --framework net10.0
dotnet sln StyliSH.sln add StyliSH.Tests/StyliSH.Tests.csproj
dotnet add StyliSH.Tests/StyliSH.Tests.csproj reference StyliSH.Abstractions/StyliSH.Abstractions.csproj
dotnet add StyliSH.Tests/StyliSH.Tests.csproj reference StyliSH.Implementations/StyliSH.Implementations.csproj
dotnet add StyliSH.Tests/StyliSH.Tests.csproj package FluentAssertions
```

#### 2. Generic Monad Law Helper
**File**: `StyliSH.Tests/MonadLawTests.cs`

A static helper class with methods that verify the 3 monad laws for any monad type. Each law test accepts:
- A `Pure` function: `Func<TValue, MonadWrapper<TMarker, TValue>>`
- An `equals` function: `Func<MonadWrapper<TMarker, TValue>, MonadWrapper<TMarker, TValue>, bool>` (for types like `TaskMonad` where structural equality needs awaiting)
- Sample values and bind functions

Laws to verify:
1. **Left Identity**: `Pure(a).Bind(f)` equals `f(a)`
2. **Right Identity**: `m.Bind(Pure)` equals `m`
3. **Associativity**: `m.Bind(f).Bind(g)` equals `m.Bind(x => f(x).Bind(g))`

For `IdMonad` and `Either`: equality via `MonadWrapper` record struct equality (value-based).
For `TaskMonad`: custom equality that awaits both tasks and compares results.
For `EitherT`: equality via `Run` to unwrap, then compare the outer monad.

### Success Criteria:

#### Automated Verification:
- [ ] `dotnet build StyliSH.Tests/StyliSH.Tests.csproj` succeeds
- [ ] Project appears in solution: `dotnet sln list`

---

## Phase 2: Monad Tests (IdMonad, Either, TaskMonad)

### Overview
Add test classes for the three base monad types.

### Changes Required:

#### 1. IdMonad Tests
**File**: `StyliSH.Tests/Monads/IdMonadTests.cs`

Tests:
- `Pure_WrapsValue` — `IdMarker.Pure(42)` produces `IdMonad<int>` with `Value == 42`
- `Map_TransformsValue` — `Pure(5).Map(x => x * 2)` yields value `10`
- `Bind_ChainsComputation` — `Pure(3).Bind(x => IdMarker.Pure(x + 1))` yields `4`
- `MonadWrapper_RoundTrip` — wrap then implicit cast back to `IdMonad<int>`
- `LeftIdentity` / `RightIdentity` / `Associativity` — via generic helpers

#### 2. Either Tests
**File**: `StyliSH.Tests/Monads/EitherTests.cs`

Tests:
- `FromValue_CreatesRight` — `Match` calls `onSuccess`
- `FromError_CreatesLeft` — `Match` calls `onError`
- `Pure_CreatesRight` — `EitherMarker<string>.Pure(42)` is a Right
- `Map_OnRight_TransformsValue`
- `Map_OnLeft_ShortCircuits` — value unchanged, error preserved
- `Bind_OnRight_ChainsComputation`
- `Bind_OnLeft_ShortCircuits`
- `EitherMarker_Match_DispatchesCorrectly`
- `MonadWrapper_RoundTrip`
- `LeftIdentity` / `RightIdentity` / `Associativity` — via generic helpers (Right path)

#### 3. TaskMonad Tests
**File**: `StyliSH.Tests/Monads/TaskMonadTests.cs`

Tests (all async):
- `Pure_CreatesCompletedTask` — `TaskMarker.Pure(42)` has result `42`
- `Map_TransformsAsyncResult` — `Pure(5).Map(x => x * 2)` awaits to `10`
- `Bind_ChainsAsyncComputation`
- `MonadWrapper_RoundTrip`
- `LeftIdentity` / `RightIdentity` / `Associativity` — with async equality comparator

### Success Criteria:

#### Automated Verification:
- [ ] `dotnet test StyliSH.Tests` — all monad tests pass

---

## Phase 3: EitherT Transformer Tests

### Overview
Test the `EitherT` transformer with both `IdMarker` (sync, easy assertions) and `TaskMarker` (async, realistic usage).

### Changes Required:

#### 1. EitherT Tests
**File**: `StyliSH.Tests/Transformers/EitherTTests.cs`

**With IdMarker as outer:**
- `FromValue_WithId_CreatesRightInId` — `Run` yields `IdMonad<Either<string, int>>` that matches Right
- `FromError_WithId_CreatesLeftInId` — `Run` yields Left
- `Map_OnSuccess_TransformsValue`
- `Map_OnError_ShortCircuits`
- `Bind_OnSuccess_ChainsComputation`
- `Bind_OnError_ShortCircuits`
- `LeftIdentity` / `RightIdentity` / `Associativity`

**With TaskMarker as outer:**
- `FromValue_WithTask_CreatesRightInTask` — `Run` yields `TaskMonad<Either<string, int>>`, await and match Right
- `FromError_WithTask_CreatesLeftInTask`
- `Map_WithTask_TransformsAsyncResult`
- `Bind_WithTask_ChainsAsyncComputation`

### Success Criteria:

#### Automated Verification:
- [ ] `dotnet test StyliSH.Tests` — all tests pass (monads + transformer)

---

## Final Verification

```bash
dotnet build StyliSH.sln
dotnet test StyliSH.Tests
```

## Key Files to Create
| File | Purpose |
|------|---------|
| `StyliSH.Tests/StyliSH.Tests.csproj` | Test project (NUnit + FluentAssertions) |
| `StyliSH.Tests/MonadLawTests.cs` | Generic monad law verification helpers |
| `StyliSH.Tests/Monads/IdMonadTests.cs` | IdMonad tests |
| `StyliSH.Tests/Monads/EitherTests.cs` | Either tests |
| `StyliSH.Tests/Monads/TaskMonadTests.cs` | TaskMonad tests |
| `StyliSH.Tests/Transformers/EitherTTests.cs` | EitherT transformer tests |

## Key Files to Reference (existing)
- `StyliSH.Abstractions/Monads/IMonad.cs` — IMonad + IMonadUnwrapper interfaces
- `StyliSH.Abstractions/Monads/IMonadMarker.cs` — IMonadMarker with static abstract Pure
- `StyliSH.Abstractions/Monads/MonadWrapper.cs` — MonadWrapper record struct
- `StyliSH.Implementations/Monads/Id/IdMonad.cs` — IdMonad + IdMarker
- `StyliSH.Implementations/Monads/Either/Either.cs` — Either type
- `StyliSH.Implementations/Monads/Either/EitherMarker.cs` — EitherMarker with Match
- `StyliSH.Implementations/Monads/Tasks/TaskMonad.cs` — TaskMonad + TaskMarker
- `StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs` — EitherT transformer
- `StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs` — EitherTMarker

## References
- Research: `thoughts/shared/research/2026-03-22-monad-test-implementation.md`
