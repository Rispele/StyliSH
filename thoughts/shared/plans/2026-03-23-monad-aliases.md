---
date: 2026-03-23T00:00:00+03:00
author: Claude (claude-opus-4-6)
branch: master
repository: StyliSH
topic: "Monad Aliases — Implementation Plan"
tags: [plan, monads, aliases, newtype, transformers]
status: draft
last_updated: 2026-03-23
last_updated_by: Claude (claude-opus-4-6)
---

# Monad Aliases — Implementation Plan

## Overview

Реализация newtype-подобного механизма алиасов для монад и трансформеров. Алиас — это отдельный тип с собственным маркером, делегирующий всю логику внутренней монаде. `MonadWrapper` алиаса приводится только к алиасу, не к оригинальному типу.

## Current State Analysis

- Маркерный паттерн (`IMonadMarker<TSelf>` + `IMonadUnwrapper`) уже обеспечивает изоляцию типов через маркеры
- Конкретные реализации: `IdMonad`, `Either`, `TaskMonad`, `EitherT`
- `FromError`/`FromValue` — статические методы на конкретных типах (`Either`, `EitherT`), не формализованы интерфейсом
- Алиасов нет — фича в фазе проектирования

### Key Discoveries:
- `MonadWrapper<TMarker, TValue>` жёстко привязан к маркеру → отдельный маркер для алиаса = полная изоляция типов
- `RawMap`/`RawBind` можно делегировать inner монаде, заворачивая результат обратно в алиас
- `ITransformerRunner` требует знания `TInnerMonad` → `TransformerAlias` будет иметь 5 type parameters
- C# `global using` **не поддерживает** open generic aliases → пользователи работают с полными generic типами или создают свои обёртки

## Desired End State

1. `MonadAlias<TAliasMarker, TInnerMarker, TValue>` — универсальная обёртка для алиасов монад
2. `TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>` — универсальная обёртка для алиасов трансформеров
3. `IEitherLikeMarker<TSelf, TError>` — интерфейс, формализующий `FromError`/`FromValue` для Either-подобных маркеров
4. Существующие `EitherMarker` и `EitherTMarker` реализуют `IEitherLikeMarker`
5. Тесты: законы монад для алиасов, изоляция типов, `FromError`/`FromValue`

### Verification:
- `dotnet build StyliSH.sln` — проект собирается
- `dotnet test StyliSH.Tests` — все тесты проходят (старые + новые)
- Алиас-монады удовлетворяют трём законам монад (left identity, right identity, associativity)
- `MonadWrapper<AliasMarker, T>` не приводится к оригинальному типу (cast throws `InvalidCastException`)

## What We're NOT Doing

- Pattern matching (`Match`) на алиасах через интерфейс — доступен через `.Inner`
- Специализированные `EitherTAlias` / `EitherAlias` — только универсальные типы
- Convenience extensions для упрощения создания алиасов (может быть в будущем)
- Lift/Hoist операции для конверсии между алиасом и оригиналом

## Implementation Approach

Два инфраструктурных типа (`MonadAlias`, `TransformerAlias`) в `StyliSH.Abstractions`, один интерфейс (`IEitherLikeMarker`) там же. Ретрофит существующих маркеров. Тесты на примере `DomainResult` / `DomainResultT`.

---

## Phase 1: IEitherLikeMarker Interface

### Overview
Формализовать `FromError`/`FromValue` в интерфейсе. Ретрофитить `EitherMarker` и `EitherTMarker`.

### Changes Required:

#### 1. New: `IEitherLikeMarker<TSelf, TError>`
**File**: `StyliSH.Abstractions/Monads/IEitherLikeMarker.cs`

```csharp
namespace StyliSH.Abstractions.Monads;

public interface IEitherLikeMarker<TSelf, TError> : IMonadMarker<TSelf>
    where TSelf : IEitherLikeMarker<TSelf, TError>
{
    static abstract MonadWrapper<TSelf, TValue> FromError<TValue>(TError error);
    static abstract MonadWrapper<TSelf, TValue> FromValue<TValue>(TValue value);
}
```

#### 2. Retrofit `EitherMarker<TError>`
**File**: `StyliSH.Implementations/Monads/Either/EitherMarker.cs`

```csharp
public readonly record struct EitherMarker<TError> : IEitherLikeMarker<EitherMarker<TError>, TError>
{
    // Existing:
    public static IMonad<EitherMarker<TError>, TValue> Pure<TValue>(TValue value)
        => Either<TError, TValue>.Success(value);

    // New (from IEitherLikeMarker):
    public static MonadWrapper<EitherMarker<TError>, TValue> FromError<TValue>(TError error)
        => Either<TError, TValue>.FromError(error);

    public static MonadWrapper<EitherMarker<TError>, TValue> FromValue<TValue>(TValue value)
        => Either<TError, TValue>.FromValue(value);

    // Existing Match stays as-is:
    public static TResult Match<TValue, TResult>(...) => ...;
}
```

#### 3. Retrofit `EitherTMarker<TOuterMarker, TError>`
**File**: `StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs`

```csharp
public readonly record struct EitherTMarker<TOuterMarker, TError>
    : IEitherLikeMarker<EitherTMarker<TOuterMarker, TError>, TError>,
      ITransformerMarker<EitherTMarker<TOuterMarker, TError>, TOuterMarker>
    where TOuterMarker : IMonadMarker<TOuterMarker>
{
    // Existing Pure stays as-is

    // New (from IEitherLikeMarker):
    public static MonadWrapper<EitherTMarker<TOuterMarker, TError>, TValue> FromError<TValue>(TError error)
        => EitherT<TOuterMarker, TError, TValue>.FromError(error);

    public static MonadWrapper<EitherTMarker<TOuterMarker, TError>, TValue> FromValue<TValue>(TValue value)
        => EitherT<TOuterMarker, TError, TValue>.FromValue(value);
}
```

### Success Criteria:

#### Automated Verification:
- [x] `dotnet build StyliSH.sln` — проект собирается
- [x] `dotnet test StyliSH.Tests` — все существующие тесты проходят (ретрофит не ломает ничего)

---

## Phase 2: MonadAlias

### Overview
Создать `MonadAlias<TAliasMarker, TInnerMarker, TValue>` — универсальную обёртку для алиасов монад.

### Changes Required:

#### 1. New: `MonadAlias<TAliasMarker, TInnerMarker, TValue>`
**File**: `StyliSH.Abstractions/Monads/Aliases/MonadAlias.cs`

```csharp
using StyliSH.Abstractions.Monads;

namespace StyliSH.Abstractions.Monads.Aliases;

public readonly record struct MonadAlias<TAliasMarker, TInnerMarker, TValue>(
    IMonad<TInnerMarker, TValue> Inner)
    : IMonad<TAliasMarker, TValue>,
      IMonadUnwrapper<MonadAlias<TAliasMarker, TInnerMarker, TValue>, TAliasMarker, TValue>
    where TAliasMarker : IMonadMarker<TAliasMarker>
    where TInnerMarker : IMonadMarker<TInnerMarker>
{
    public IMonad<TAliasMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new MonadAlias<TAliasMarker, TInnerMarker, TNewValue>(Inner.RawMap(map));

    public IMonad<TAliasMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<TAliasMarker, TNewValue>> bind)
        => new MonadAlias<TAliasMarker, TInnerMarker, TNewValue>(
            Inner.RawBind(value =>
                ((MonadAlias<TAliasMarker, TInnerMarker, TNewValue>)bind(value)).Inner));

    public static implicit operator MonadAlias<TAliasMarker, TInnerMarker, TValue>(
        MonadWrapper<TAliasMarker, TValue> monad)
        => IMonadUnwrapper<MonadAlias<TAliasMarker, TInnerMarker, TValue>, TAliasMarker, TValue>
            .CastFrom(monad);
}
```

#### 2. Tests: MonadAlias
**File**: `StyliSH.Tests/Aliases/MonadAliasTests.cs`

Тестовый алиас: `DomainResult` как алиас для `Either<string, TValue>`.

```csharp
// Тестовый маркер
public readonly record struct DomainResultMarker
    : IEitherLikeMarker<DomainResultMarker, string>
{
    public static IMonad<DomainResultMarker, TValue> Pure<TValue>(TValue value)
        => new MonadAlias<DomainResultMarker, EitherMarker<string>, TValue>(
            EitherMarker<string>.Pure(value));

    public static MonadWrapper<DomainResultMarker, TValue> FromError<TValue>(string error)
        => new MonadAlias<DomainResultMarker, EitherMarker<string>, TValue>(
            EitherMarker<string>.FromError<TValue>(error).Monad).Wrap();

    public static MonadWrapper<DomainResultMarker, TValue> FromValue<TValue>(TValue value)
        => new MonadAlias<DomainResultMarker, EitherMarker<string>, TValue>(
            EitherMarker<string>.FromValue(value).Monad).Wrap();
}
```

Тесты:
- `Pure_WrapsValue` — `Pure(42)` создаёт алиас, через `Inner` можно получить `Either` со значением 42
- `FromError_CreatesError` — `FromError("fail")` создаёт алиас с ошибкой
- `FromValue_CreatesSuccess` — `FromValue(42)` создаёт алиас с успехом
- `Map_TransformsValue` — `FromValue(5).Map(x => x * 2)` → 10
- `Map_OnError_ShortCircuits` — `FromError("e").Map(...)` → ошибка сохраняется
- `Bind_ChainsComputation` — `FromValue(3).Bind(x => FromValue(x + 10))` → 13
- `Bind_OnError_ShortCircuits` — `FromError("e").Bind(...)` → ошибка сохраняется
- `MonadWrapper_CastsToAlias_NotToOriginal` — `MonadWrapper<DomainResultMarker, int>` приводится к `MonadAlias<DomainResultMarker, ...>`, но НЕ к `Either<string, int>` (не компилируется — разные маркеры)
- `LeftIdentity`, `RightIdentity`, `Associativity` — три закона монад

### Success Criteria:

#### Automated Verification:
- [x] `dotnet build StyliSH.sln`
- [x] `dotnet test StyliSH.Tests` — все тесты проходят, включая новые alias-тесты
- [x] Законы монад проходят для `DomainResultMarker`

---

## Phase 3: TransformerAlias

### Overview
Создать `TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>` — универсальную обёртку для алиасов трансформеров.

### Changes Required:

#### 1. New: `TransformerAlias`
**File**: `StyliSH.Abstractions/Monads/Aliases/TransformerAlias.cs`

```csharp
using StyliSH.Abstractions.Monads.Transformers;

namespace StyliSH.Abstractions.Monads.Aliases;

public readonly record struct TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>(
    IMonad<TInnerTransformerMarker, TValue> Inner)
    : ITransformer<TAliasMarker, TOuterMarker, TValue>,
      ITransformerRunner<TOuterMarker, TInnerMonad, TValue>,
      IMonadUnwrapper<TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>, TAliasMarker, TValue>
    where TAliasMarker : ITransformerMarker<TAliasMarker, TOuterMarker>
    where TInnerTransformerMarker : ITransformerMarker<TInnerTransformerMarker, TOuterMarker>
    where TOuterMarker : IMonadMarker<TOuterMarker>
{
    public IMonad<TAliasMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>(
            Inner.RawMap(map));

    public IMonad<TAliasMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<TAliasMarker, TNewValue>> bind)
        => new TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>(
            Inner.RawBind(value =>
                ((TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>)bind(value)).Inner));

    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMarker, TInnerMonad>,
                            IMonadUnwrapper<TOuterMonad, TOuterMarker, TInnerMonad>
        => ((ITransformerRunner<TOuterMarker, TInnerMonad, TValue>)Inner).Run<TOuterMonad>();

    public static implicit operator TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>(
        MonadWrapper<TAliasMarker, TValue> monad)
        => IMonadUnwrapper<TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>, TAliasMarker, TValue>
            .CastFrom(monad);
}
```

#### 2. Tests: TransformerAlias
**File**: `StyliSH.Tests/Aliases/TransformerAliasTests.cs`

Тестовый алиас: `DomainResultT` как алиас для `EitherT<IdMarker, string, TValue>`.

```csharp
// Тестовый маркер
public readonly record struct DomainResultTMarker
    : IEitherLikeMarker<DomainResultTMarker, string>,
      ITransformerMarker<DomainResultTMarker, IdMarker>
{
    public static IMonad<DomainResultTMarker, TValue> Pure<TValue>(TValue value)
        => new TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, TValue>, TValue>(
            EitherTMarker<IdMarker, string>.Pure<TValue>(value));

    public static MonadWrapper<DomainResultTMarker, TValue> FromError<TValue>(string error)
        => new TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, TValue>, TValue>(
            EitherTMarker<IdMarker, string>.FromError<TValue>(error).Monad).Wrap();

    public static MonadWrapper<DomainResultTMarker, TValue> FromValue<TValue>(TValue value)
        => new TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, TValue>, TValue>(
            EitherTMarker<IdMarker, string>.FromValue<TValue>(value).Monad).Wrap();
}
```

Тесты:
- `Pure_WrapsValue` — Pure + Run → IdMonad с Either.Success
- `FromError_CreatesError` — FromError + Run → IdMonad с Either.Left
- `FromValue_CreatesSuccess` — FromValue + Run → IdMonad с Either.Right
- `Map_TransformsValue` — Map через трансформер-алиас
- `Map_OnError_ShortCircuits`
- `Bind_ChainsComputation`
- `Bind_OnError_ShortCircuits`
- `Run_UnwrapsToOuterMonad` — Run возвращает корректный `IdMonad<Either<string, int>>`
- `MonadWrapper_CastsToAlias_NotToOriginal` — изоляция типов
- `LeftIdentity`, `RightIdentity`, `Associativity` — три закона монад

### Success Criteria:

#### Automated Verification:
- [x] `dotnet build StyliSH.sln`
- [x] `dotnet test StyliSH.Tests` — все тесты проходят
- [x] Законы монад проходят для `DomainResultTMarker`
- [x] `Run()` корректно разворачивает трансформер-алиас

---

## Testing Strategy

### Unit Tests (MonadAlias):
- Три закона монад через `MonadLawTests<DomainResultMarker, int>`
- `FromError`/`FromValue` через `IEitherLikeMarker`
- Изоляция типов: `MonadWrapper<DomainResultMarker, int>` → `MonadAlias` (ok), → `Either` (fail)

### Unit Tests (TransformerAlias):
- Три закона монад через `MonadLawTests<DomainResultTMarker, int>`
- `Run()` — корректное разворачивание
- `FromError`/`FromValue` через `IEitherLikeMarker`
- Изоляция типов

### Equality для тестов:
- `MonadAlias` equality: unwrap через `Inner` → delegate to inner monad equality
- `TransformerAlias` equality: `Run()` → compare outer monads

## Performance Considerations

- `MonadAlias.RawBind` содержит runtime cast `(MonadAlias<...>)bind(value)` — неизбежно из-за type erasure через `IMonad<TAliasMarker, T>`
- `TransformerAlias.Run()` содержит runtime cast к `ITransformerRunner` — неизбежно из-за хранения `IMonad<TInnerTransformerMarker, T>`
- Обе операции O(1), не влияют на производительность

## File Summary

### New Files:
1. `StyliSH.Abstractions/Monads/IEitherLikeMarker.cs` — интерфейс
2. `StyliSH.Abstractions/Monads/Aliases/MonadAlias.cs` — алиас монад
3. `StyliSH.Abstractions/Monads/Aliases/TransformerAlias.cs` — алиас трансформеров
4. `StyliSH.Tests/Aliases/MonadAliasTests.cs` — тесты алиасов монад
5. `StyliSH.Tests/Aliases/TransformerAliasTests.cs` — тесты алиасов трансформеров

### Modified Files:
1. `StyliSH.Implementations/Monads/Either/EitherMarker.cs` — `+ IEitherLikeMarker`
2. `StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs` — `+ IEitherLikeMarker`

## References

- Task: `tasks/monad-aliases.md`
- Research: `thoughts/shared/research/2026-03-23-monad-aliases.md`
- Existing tests: `StyliSH.Tests/Monads/`, `StyliSH.Tests/Transformers/`
- C# using alias limitation: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-directive
