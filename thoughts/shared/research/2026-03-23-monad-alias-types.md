---
date: 2026-03-23T18:00:00+03:00
researcher: Claude (claude-opus-4-6)
git_commit: e40242ddf5d3bd5340ddf129c355c9541d761c3a
branch: monad-aliases
repository: StyliSH
topic: "Monad Alias Types — Named Types for Function Signatures"
tags: [research, codebase, monads, aliases, named-types, source-generators, C#-limitations]
status: complete
last_updated: 2026-03-23
last_updated_by: Claude (claude-opus-4-6)
---

# Research: Monad Alias Types — Named Types for Function Signatures

**Date**: 2026-03-23T18:00:00+03:00
**Researcher**: Claude (claude-opus-4-6)
**Git Commit**: e40242ddf5d3bd5340ddf129c355c9541d761c3a
**Branch**: monad-aliases
**Repository**: StyliSH

---

## Research Question

Как реализовать именованные типы алиасов (`DomainResult<int>` вместо `MonadAlias<DomainResultMarker, EitherMarker<string>, int>`), чтобы использовать их в сигнатурах функций.

Текущее состояние:
```csharp
// Сейчас:
MonadAlias<DomainResultMarker, EitherMarker<string>, int> Foo(Type1 arg1, Type2 arg2...)

// Хочу:
DomainResult<int> Foo(Type1 arg1, Type2 arg2...)
```

---

## Summary

**C# не поддерживает open generic using aliases** (`using DomainResult<T> = MonadAlias<..., T>` — невозможно). Это фундаментальное ограничение языка, не решённое ни в C# 14 / .NET 10.

Предлагаются три подхода:
1. **Именованные struct-ы вместо MonadAlias** — пользователь определяет `DomainResult<TValue>` как `readonly record struct` с той же логикой, что и MonadAlias (~15 строк). `MonadAlias<,,>` остаётся как generic-вариант для inline использования.
2. **Source Generator** — атрибут + кодогенерация для автоматизации подхода 1.
3. **IMonadAliasMarker + обновлённый MonadAlias** — маркер несёт информацию об inner marker через новый интерфейс, что позволяет MonadAlias делегировать создание экземпляров маркеру.

**Рекомендация**: подход 1 (именованные struct-ы) — наименее инвазивный, не требует новой инфраструктуры, даёт именно тот API, который нужен.

---

## Detailed Findings

### Finding 1: Open Generic Using Aliases — Не поддерживается

**C# 12** (shipped с .NET 8) расширил `using` для алиасов любых типов, включая tuples, pointers, closed generics:
```csharp
using Point = (int x, int y);              // ✓ — closed tuple
using IntList = System.Collections.Generic.List<int>;  // ✓ — closed generic
```

**Open generic aliases НЕ поддерживаются**:
```csharp
using DomainResult<T> = MonadAlias<DomainResultMarker, EitherMarker<string>, T>;  // ✗ — compile error
```

Это давний feature request: [dotnet/csharplang#1239](https://github.com/dotnet/csharplang/issues/1239), [dotnet/csharplang Discussion #90](https://github.com/dotnet/csharplang/discussions/90). По состоянию на C# 14 / .NET 10 (November 2025) — **не реализовано**.

### Finding 2: C# 14 Extension Types — Не помогают

C# 14 добавил extension members (extension properties, operators, static methods). Но extension types **расширяют существующие типы**, а не создают новые имена типов. Нельзя написать:
```csharp
extension DomainResult<T> for MonadAlias<DomainResultMarker, EitherMarker<string>, T> { }
// Это не создаёт новый тип — DomainResult<T> нельзя использовать в сигнатурах
```

### Finding 3: Record Struct Inheritance — Невозможно

`readonly record struct` не поддерживает наследование. `DomainResult<T>` не может наследовать от `MonadAlias<,,T>`.

### Finding 4: Default Interface Methods — Ограничены отсутствием HKT

Нельзя написать generic интерфейс с default implementation для `RawMap`/`RawBind`, который создаёт новый экземпляр типа-алиаса с другим `TValue`. Причина: C# не имеет Higher-Kinded Types — невозможно выразить "создай `TSelf` но с другим `TValue`".

---

## Подходы

### Подход A: Именованные Struct-ы (Manual Named Types)

Пользователь определяет `DomainResult<TValue>` как самостоятельный `readonly record struct`, воспроизводящий логику `MonadAlias`. `MonadAlias<,,>` НЕ используется — `DomainResult` сам реализует `IMonad`:

```csharp
// ─── Тип алиаса ─────────────────────────────────────────────────────
public readonly record struct DomainResult<TValue>(
    IMonad<EitherMarker<string>, TValue> Inner)
    : IMonad<DomainResultMarker, TValue>,
      IMonadUnwrapper<DomainResult<TValue>, DomainResultMarker, TValue>
{
    public IMonad<DomainResultMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new DomainResult<TNewValue>(Inner.RawMap(map));

    public IMonad<DomainResultMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<DomainResultMarker, TNewValue>> bind)
        => new DomainResult<TNewValue>(
            Inner.RawBind(value =>
                ((DomainResult<TNewValue>)bind(value)).Inner));

    public static implicit operator DomainResult<TValue>(
        MonadWrapper<DomainResultMarker, TValue> monad)
        => IMonadUnwrapper<DomainResult<TValue>, DomainResultMarker, TValue>
            .CastFrom(monad);
}

// ─── Маркер ─────────────────────────────────────────────────────────
public readonly record struct DomainResultMarker
    : IEitherLikeMarker<DomainResultMarker, string>
{
    public static IMonad<DomainResultMarker, TValue> Pure<TValue>(TValue value)
        => new DomainResult<TValue>(EitherMarker<string>.Pure(value));

    public static MonadWrapper<DomainResultMarker, TValue> FromError<TValue>(string error)
        => new DomainResult<TValue>(
            EitherMarker<string>.FromError<TValue>(error).Monad).Wrap();

    public static MonadWrapper<DomainResultMarker, TValue> FromValue<TValue>(TValue value)
        => new DomainResult<TValue>(
            EitherMarker<string>.FromValue(value).Monad).Wrap();
}
```

**Использование в сигнатурах**:
```csharp
DomainResult<int> Foo(Type1 arg1, Type2 arg2)
{
    return DomainResultMarker.FromValue(42);  // implicit cast MonadWrapper → DomainResult
}
```

**Плюсы**:
- Чистый API: `DomainResult<int>` в сигнатурах
- Тип полностью совместим с `MonadWrapper<DomainResultMarker, TValue>` через implicit cast
- Никакой новой инфраструктуры — использует существующие абстракции
- Паттерн идентичен `MonadAlias`, но с именем пользователя

**Минусы**:
- ~15 строк boilerplate на тип алиаса (+ ~10 на маркер)
- RawMap/RawBind дублируют логику MonadAlias
- Runtime cast в RawBind (как и в MonadAlias — неизбежно)

**Взаимоотношение с MonadAlias**:
- `MonadAlias<,,>` **остаётся** как generic-вариант для inline использования (когда именованный тип не нужен)
- Именованный struct (`DomainResult`) **заменяет** MonadAlias для данного конкретного алиаса
- Оба подхода могут сосуществовать: MonadAlias = quick & dirty, Named struct = production signatures

### Подход B: Source Generator

Автоматизация подхода A через атрибут и кодогенерацию:

```csharp
// Пользователь пишет только:
[MonadAlias<EitherMarker<string>>]
public partial readonly record struct DomainResult<TValue>;

// Source generator генерирует:
// - Inner property
// - IMonad<DomainResultMarker, TValue> implementation
// - IMonadUnwrapper implementation
// - RawMap, RawBind, implicit operator
```

**Плюсы**:
- Минимальный boilerplate (1 строка + атрибут)
- Compile-time, без runtime overhead
- Одинаковый паттерн для всех алиасов

**Минусы**:
- Требует создания отдельного проекта `StyliSH.Generators` (Microsoft.CodeAnalysis.CSharp)
- Значительная начальная инвестиция (~200-400 строк кода генератора)
- Дополнительная сложность отладки (generated code)
- Source generators не работают с `record struct` primary constructors просто — нужен workaround

**Оценка**: имеет смысл если планируется 5+ алиасов. Для 1-3 алиасов — overkill.

### Подход C: IMonadAliasMarker + Обновлённый MonadAlias

Новый интерфейс маркера, несущий информацию о `TInnerMarker`:

```csharp
public interface IMonadAliasMarker<TSelf, TInnerMarker> : IMonadMarker<TSelf>
    where TSelf : IMonadAliasMarker<TSelf, TInnerMarker>
    where TInnerMarker : IMonadMarker<TInnerMarker>
{
    static abstract IMonad<TSelf, TValue> WrapInner<TValue>(IMonad<TInnerMarker, TValue> inner);
    static abstract IMonad<TInnerMarker, TValue> UnwrapAlias<TValue>(IMonad<TSelf, TValue> alias);
}
```

`MonadAlias` использует маркер для создания экземпляров:
```csharp
public readonly record struct MonadAlias<TAliasMarker, TInnerMarker, TValue>(
    IMonad<TInnerMarker, TValue> Inner)
    where TAliasMarker : IMonadAliasMarker<TAliasMarker, TInnerMarker>
{
    public IMonad<TAliasMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => TAliasMarker.WrapInner(Inner.RawMap(map));  // Маркер решает, какой тип создать

    public IMonad<TAliasMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<TAliasMarker, TNewValue>> bind)
        => TAliasMarker.WrapInner(
            Inner.RawBind(value =>
                TAliasMarker.UnwrapAlias(bind(value))));  // Без runtime cast!
}
```

**Плюсы**:
- Маркер контролирует создание экземпляров → можно возвращать именованный тип из MonadAlias
- Убирает runtime cast в RawBind (через `UnwrapAlias`)

**Минусы**:
- Не решает проблему сигнатур: в сигнатуре всё ещё `MonadAlias<DomainResultMarker, EitherMarker<string>, int>` — 3 параметра
- Добавляет сложность (новый интерфейс, больше static abstract методов)
- Если маркер.WrapInner возвращает `DomainResult<T>`, а MonadAlias хранит `IMonad`, то inner type mismatch: `MonadAlias.Inner` — это `IMonad<TInnerMarker>`, но `DomainResult.Inner` тоже `IMonad<TInnerMarker>`, а сами они оба `IMonad<TAliasMarker>` — два разных runtime типа для одного маркера

**Вывод**: подход C усложняет архитектуру без решения основной проблемы (именованные типы в сигнатурах). Не рекомендуется.

---

## Подход для TransformerAlias

Аналогично подходу A, но для трансформеров:

```csharp
public readonly record struct DomainResultT<TValue>(
    IMonad<EitherTMarker<TaskMarker, string>, TValue> Inner)
    : ITransformer<DomainResultTMarker, TaskMarker, TValue>,
      ITransformerRunner<TaskMarker, Either<string, TValue>, TValue>,
      IMonadUnwrapper<DomainResultT<TValue>, DomainResultTMarker, TValue>
{
    public IMonad<DomainResultTMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new DomainResultT<TNewValue>(Inner.RawMap(map));

    public IMonad<DomainResultTMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<DomainResultTMarker, TNewValue>> bind)
        => new DomainResultT<TNewValue>(
            Inner.RawBind(value =>
                ((DomainResultT<TNewValue>)bind(value)).Inner));

    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TaskMarker, Either<string, TValue>>,
                            IMonadUnwrapper<TOuterMonad, TaskMarker, Either<string, TValue>>
        => ((ITransformerRunner<TaskMarker, Either<string, TValue>, TValue>)Inner)
            .Run<TOuterMonad>();

    public static implicit operator DomainResultT<TValue>(
        MonadWrapper<DomainResultTMarker, TValue> monad)
        => IMonadUnwrapper<DomainResultT<TValue>, DomainResultTMarker, TValue>
            .CastFrom(monad);
}
```

---

## Рекомендация

**Подход A (именованные struct-ы)** — для текущего этапа.

Причины:
1. Решает основную проблему (чистые сигнатуры)
2. Не требует новой инфраструктуры
3. Boilerplate приемлем (~25 строк на маркер + тип)
4. `MonadAlias`/`TransformerAlias` остаются для generic/inline использования
5. Паттерн может быть автоматизирован source generator'ом позже

**MonadAlias остаётся**: `MonadAlias<,,>` и `TransformerAlias<,,,,>` продолжают существовать как generic инфраструктурные типы. Именованные struct-ы — это дополнительный способ определения алиасов, а не замена.

---

## Code References

- `StyliSH.Abstractions/Monads/Aliases/MonadAlias.cs` — текущий generic MonadAlias (3 type params)
- `StyliSH.Abstractions/Monads/Aliases/TransformerAlias.cs` — текущий generic TransformerAlias (5 type params)
- `StyliSH.Abstractions/Monads/IEitherLikeMarker.cs` — интерфейс для Either-подобных маркеров
- `StyliSH.Abstractions/Monads/IMonad.cs:6-21` — IMonad + IMonadUnwrapper
- `StyliSH.Abstractions/Monads/MonadWrapper.cs` — type-erased wrapper
- `StyliSH.Tests/Aliases/MonadAliasTests.cs` — тесты MonadAlias (DomainResultMarker)
- `StyliSH.Tests/Aliases/TransformerAliasTests.cs` — тесты TransformerAlias (DomainResultTMarker)

## Historical Context (from thoughts/)

- `thoughts/shared/research/2026-03-23-monad-aliases.md` — предыдущее исследование, описывает дизайн MonadAlias/TransformerAlias
- `thoughts/shared/plans/2026-03-23-monad-aliases.md` — план реализации MonadAlias (phases 1-3, все завершены)

## Related Research

- `thoughts/shared/research/2026-03-23-monad-aliases.md` — дизайн инфраструктурных типов алиасов

## Open Questions

1. **Нужен ли source generator?** Зависит от количества планируемых алиасов. Для 1-3 — manual struct достаточно. Для 5+ — стоит рассмотреть.

2. **Naming convention**: `DomainResult<T>` (тип) + `DomainResultMarker` (маркер) или `DomainResult<T>` + `DomainResult.Marker` (вложенный тип)? Вложенный маркер позволяет `DomainResult.FromValue(42)` вместо `DomainResultMarker.FromValue(42)`.

3. **Match на именованных алиасах**: Пользователь может добавить extension method `Match` для `DomainResult<T>`, делегирующий к `Inner`. Стоит ли формализовать это в интерфейсе?

4. **Коллизия IMonadUnwrapper**: Если в одной сборке определены и `MonadAlias<DomainResultMarker, EitherMarker<string>, int>` и `DomainResult<int>` — оба реализуют `IMonadUnwrapper` для `DomainResultMarker`. Implicit cast из `MonadWrapper` будет работать только для того типа, чьи экземпляры фактически лежат внутри MonadWrapper. Нужно чётко разграничить: либо всегда MonadAlias, либо всегда DomainResult.

   **Решение**: при определении именованного типа `DomainResult<T>`, маркер (`DomainResultMarker`) создаёт экземпляры `DomainResult<T>` (не `MonadAlias<,,>`). Тогда `MonadAlias<DomainResultMarker,,>` не используется для этого маркера вообще.

## Web Sources

- [dotnet/csharplang#1239 — Support generics in aliases](https://github.com/dotnet/csharplang/issues/1239)
- [dotnet/csharplang Discussion #90 — Open generic aliases](https://github.com/dotnet/csharplang/discussions/90)
- [What's new in C# 14 — Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-14)
- [C# 14 extension members — .NET Blog](https://devblogs.microsoft.com/dotnet/csharp-exploring-extension-members/)
