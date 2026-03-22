# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build StyliSH.sln

# Run the demo application
dotnet run --project StyliSH/StyliSH.csproj
```

No test or lint infrastructure is configured yet.

## Architecture

StyliSH is a C# monad library targeting .NET 10. It is split into three projects:

- **StyliSH.Abstractions** — core interfaces and the wrapper type
- **StyliSH.Implementations** — concrete monad implementations
- **StyliSH** — demo/entry point (references both above)
- **StyliSH.Research** — scratch/experimental code, not used by other projects

### Key Abstractions (`StyliSH.Abstractions`)

**Marker type pattern** is used throughout instead of virtual dispatch. Each monad has a companion marker type that holds `static abstract` methods:

- `IMonadMarker<TSelf>` — declares `Pure<TValue>(TValue)`, implemented by marker types (e.g., `IdMarker`, `TaskMarker`, `EitherMarker<TError>`)
- `IMonad<TMarker, TValue>` — the monad instance interface; exposes `Map`, `Bind`, `RawMap`, `RawBind`
- `MonadWrapper<TMarker, TValue>` — type-erased wrapper record struct; allows code to work generically over any monad
- `IMonadUnwrapper<TSelf, TMarker, TValue>` — implemented by concrete types to provide an implicit cast from `MonadWrapper` back to the concrete type

**Transformer abstractions** (`Monads/Transformers/`):
- `ITransformerMarker<TSelf, TOuterMonadMarker>` — marker for composed monads
- `ITransformer<TTransformerMarker, TOuterMonadMarker, TValue>` — transformer instance interface
- `ITransformerRunner<>` — unwraps a transformer back to the outer monad (e.g., `EitherT<TaskMarker, E, A>` → `Task<Either<E, A>>`)

`Unit` is the void-equivalent record struct used as `TValue` for side-effecting computations.

### Concrete Implementations (`StyliSH.Implementations`)

| Type | Marker | Purpose |
|------|--------|---------|
| `IdMonad<TValue>` | `IdMarker` | Identity monad — no effects |
| `Either<TError, TValue>` | `EitherMarker<TError>` | Synchronous error handling (Left = error, Right = success) |
| `TaskMonad<TValue>` | `TaskMarker` | Wraps `Task<T>` for async computations |
| `EitherT<TOuterMarker, TError, TValue>` | `EitherTMarker<TOuterMarker, TError>` | Transformer stacking Either on any outer monad (typically `TaskMarker`) |

### Adding a New Monad

1. Create a marker type implementing `IMonadMarker<TSelf>` with a static `Pure` method.
2. Create the monad struct/record implementing `IMonad<TMarker, TValue>` and `IMonadUnwrapper<TSelf, TMarker, TValue>` (for the implicit cast).
3. Implement `Map` and `Bind` using the marker's static methods for lifting.

### Adding a New Transformer

Follow the `EitherT` pattern: create a marker implementing `ITransformerMarker` and a struct implementing both `ITransformer` and `ITransformerRunner`.
