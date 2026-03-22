---
date: 2026-03-23T00:00:00+03:00
researcher: Claude (claude-opus-4-6)
git_commit: 4b91b290dbf434ec445459bd940f2a953de2bacf
branch: master
repository: StyliSH
topic: "Monad Aliases — Implementation Research"
tags: [research, codebase, monads, transformers, aliases, newtype, Either, EitherT, MonadWrapper]
status: complete
last_updated: 2026-03-23
last_updated_by: Claude (claude-opus-4-6)
---

# Research: Monad Aliases — Implementation Research

**Date**: 2026-03-23T00:00:00+03:00
**Researcher**: Claude (claude-opus-4-6)
**Git Commit**: 4b91b290dbf434ec445459bd940f2a953de2bacf
**Branch**: master
**Repository**: StyliSH

---

## Research Question

Как реализовать алиасы для монад и трансформеров в виде единого механизма, соответствующего требованиям в `tasks/monad-aliases.md`.

Ключевые требования:
- Алиасы работают как `newtype` из Haskell
- Единый механизм для монад и трансформеров
- Один маркер — одна монада; `MonadWrapper` от операций над алиасом по умолчанию приводится только к алиасу, но явное приведение к оригинальному типу допустимо по требованию
- Алиас прост в реализации

---

## Summary

Текущая архитектура хорошо подходит для реализации алиасов. Ключевой механизм — это отдельный **маркер-тип** для каждого алиаса и обёртывающий `readonly record struct` (`MonadAlias<...>` / `EitherTAlias<...>`), который делегирует всю логику внутренней монаде. C# 12 generic type aliases (`.NET 10`) позволяют дать этой обёртке пользовательское имя (`DomainResult<TValue>`).

**Ключевой инсайт**: «Один маркер — одна монада» уже обеспечивается тем, что тип `MonadWrapper<TMarker, TValue>` параметризован маркером. Если маркер алиаса (`DomainResultMarker`) отличен от маркера оригинала (`EitherMarker<DomainError>`), то `MonadWrapper<DomainResultMarker, TValue>` по умолчанию (implicit) приводится только к типу алиаса — не к `Either<DomainError, TValue>`. Доступ к оригинальному типу по требованию — через свойство `Inner` или явный explicit-оператор на самом алиасе.

---

## Detailed Findings

### Текущие абстракции (StyliSH.Abstractions)

#### IMonadMarker<TSelf>
`StyliSH.Abstractions/Monads/IMonadMarker.cs`

```csharp
public interface IMonadMarker<TSelf>
    where TSelf : IMonadMarker<TSelf>
{
    public static abstract IMonad<TSelf, TValue> Pure<TValue>(TValue value);
}
```

- CRTP-паттерн: `TSelf : IMonadMarker<TSelf>`
- Единственный статический абстрактный метод `Pure<TValue>()` — "поднятие" значения в монаду
- Реализуется маркер-типом каждой конкретной монады

#### IMonad<TMarker, TValue>
`StyliSH.Abstractions/Monads/IMonad.cs:6-21`

```csharp
public interface IMonad<TMonadMarker, out TValue>
    where TMonadMarker : IMonadMarker<TMonadMarker>
{
    public MonadWrapper<TMonadMarker, TNewValue> Map<TNewValue>(Func<TValue, TNewValue> map)
        => RawMap(map).Wrap();
    public MonadWrapper<TMonadMarker, TNewValue> Bind<TNewValue>(Func<TValue, IMonad<TMonadMarker, TNewValue>> bind)
        => RawBind(bind).Wrap();
    public MonadWrapper<TMonadMarker, TNewValue> Bind<TNewValue>(Func<TValue, MonadWrapper<TMonadMarker, TNewValue>> bind);
    public IMonad<TMonadMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map);
    public IMonad<TMonadMarker, TNewValue> RawBind<TNewValue>(Func<TValue, IMonad<TMonadMarker, TNewValue>> bind);
}
```

- `Map`/`Bind` — default-реализации через `RawMap`/`RawBind` + `.Wrap()`
- `RawMap`/`RawBind` — абстрактные, реализуются конкретными монадами

#### MonadWrapper<TMarker, TValue>
`StyliSH.Abstractions/Monads/MonadWrapper.cs`

```csharp
public readonly record struct MonadWrapper<TMarker, TValue>(IMonad<TMarker, TValue> Monad)
    where TMarker : IMonadMarker<TMarker>
```

- Тип-стёртая обёртка, параметризованная **маркером**
- Именно маркер определяет, к какому конкретному типу можно привести `MonadWrapper`

#### IMonadUnwrapper<TSelf, TMarker, TValue>
`StyliSH.Abstractions/Monads/IMonad.cs:26-36`

```csharp
public interface IMonadUnwrapper<out TSelf, TMonadMarker, TValue>
    where TMonadMarker : IMonadMarker<TMonadMarker>
    where TSelf : IMonadUnwrapper<TSelf, TMonadMarker, TValue>, IMonad<TMonadMarker, TValue>
{
    public static abstract implicit operator TSelf(MonadWrapper<TMonadMarker, TValue> monad);

    public static TSelf CastFrom(MonadWrapper<TMonadMarker, TValue> monad)
        => monad.Monad is TSelf concrete
            ? concrete
            : throw new InvalidCastException(...);
}
```

- `static abstract implicit operator` — синтаксис `TSelf value = monadWrapper;`
- `CastFrom` — runtime-проверка типа через `is`

#### Transformer abstractions
`StyliSH.Abstractions/Monads/Transformers/`

- `ITransformerMarker<TSelf, TOuterMarker>` — расширяет `IMonadMarker<TSelf>`, добавляет параметр внешней монады
- `ITransformer<TMarker, TOuterMarker, TValue>` — расширяет `IMonad<TMarker, TValue>`
- `ITransformerRunner<TOuterMarker, TInnerMonad, TValue>` — метод `Run<TOuterMonad>()` для разворачивания трансформера

### Конкретные реализации (StyliSH.Implementations)

#### Either<TError, TValue> / EitherMarker<TError>
`StyliSH.Implementations/Monads/Either/Either.cs`
`StyliSH.Implementations/Monads/Either/EitherMarker.cs`

- `Either` — `readonly record struct`, внутреннее состояние: `isSuccess`, `error`, `value`
- `EitherMarker<TError>.Pure<TValue>` → `Either<TError, TValue>.Success(value)`
- Статические фабричные методы: `Either.FromError(error)`, `Either.FromValue(value)` → возвращают `MonadWrapper`
- `EitherMarker<TError>.Match<TValue, TResult>` — статический pattern-matching на `IMonad`

#### EitherT<TOuterMarker, TError, TValue> / EitherTMarker
`StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs`
`StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs`

- Хранит `IMonad<TOuterMarker, IMonad<EitherMarker<TError>, TValue>> OuterMonad`
- Реализует `ITransformer`, `ITransformerRunner`, `IMonadUnwrapper`
- `RawBind` использует `EitherMarker<TError>.Match()` для pattern-matching по внутренней Either
- `Run<TOuterMonad>()` — приводит `IMonad<EitherMarker<TError>, TValue>` к `Either<TError, TValue>` и оборачивает во внешнюю монаду

#### Паттерн IMonadUnwrapper во всех реализациях

Все конкретные типы (`IdMonad`, `Either`, `TaskMonad`, `EitherT`) реализуют:
```csharp
public static implicit operator ConcreteType<TValue>(MonadWrapper<Marker, TValue> monad)
    => IMonadUnwrapper<ConcreteType<TValue>, Marker, TValue>.CastFrom(monad);
```

### Нет существующих алиасов

Поиск по всей кодовой базе: `alias`, `newtype`, `DomainResult`, `DomainError` — совпадений нет (кроме `tasks/monad-aliases.md`). Фича в фазе проектирования.

---

## Architecture Insights

### Почему текущая архитектура хорошо подходит для алиасов

1. **Маркер = граница типа**: `MonadWrapper<TMarker, TValue>` жёстко привязан к маркеру. Если для алиаса создать отдельный маркер — обеспечивается полная изоляция.
2. **Implicit cast через IMonadUnwrapper**: Механизм уже универсален. Нужно только реализовать его для нового типа.
3. **Pure через static abstract**: Позволяет создавать новые "точки входа" без наследования.

### Предлагаемый дизайн алиасов

#### Для монад: `MonadAlias<TAliasMarker, TInnerMarker, TValue>`

Единый инфраструктурный тип в `StyliSH.Abstractions`:

```csharp
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

    // Implicit: MonadWrapper<TAliasMarker, TValue> → alias (по умолчанию)
    public static implicit operator MonadAlias<TAliasMarker, TInnerMarker, TValue>(
        MonadWrapper<TAliasMarker, TValue> monad)
        => IMonadUnwrapper<MonadAlias<TAliasMarker, TInnerMarker, TValue>, TAliasMarker, TValue>
            .CastFrom(monad);
}
```

Доступ к оригинальному типу **по требованию** обеспечивается двумя способами:

1. **Через `Inner`** — свойство уже есть как параметр record struct. Пользователь явно пишет `domainResult.Inner` и затем кастует к нужному конкретному типу (`(Either<DomainError, TValue>)domainResult.Inner`).

2. **Через explicit-оператор на маркере/алиасе** — если нужен более явный и безопасный API:

```csharp
// Можно добавить в DomainResultMarker или как extension
public static Either<DomainError, TValue> ToEither<TValue>(
    this MonadAlias<DomainResultMarker, EitherMarker<DomainError>, TValue> alias)
    => (Either<DomainError, TValue>)alias.Inner;
```

Ключевое: `MonadWrapper<DomainResultMarker, TValue>` **не имеет** implicit cast к `Either<DomainError, TValue>` — типы маркеров разные. Получить `Either` можно только через сам алиас, не через `MonadWrapper` напрямую. Это и есть гарантия «MonadWrapper приводится только к алиасу».

**Использование пользователем** (для `DomainResult` как алиаса `Either<DomainError, TValue>`):

```csharp
// 1. Определить маркер (единственный шаг с реальной логикой)
public readonly record struct DomainResultMarker : IMonadMarker<DomainResultMarker>
{
    public static IMonad<DomainResultMarker, TValue> Pure<TValue>(TValue value)
        => new MonadAlias<DomainResultMarker, EitherMarker<DomainError>, TValue>(
            Either<DomainError, TValue>.Success(value));

    public static MonadWrapper<DomainResultMarker, TValue> FromError<TValue>(DomainError error)
        => new MonadAlias<DomainResultMarker, EitherMarker<DomainError>, TValue>(
            Either<DomainError, TValue>.FromError(error).Monad).Wrap();
}

// 2. Создать алиас типа (C# 12, .NET 10, global using)
global using DomainResult<TValue> =
    StyliSH.Abstractions.Aliases.MonadAlias<DomainResultMarker, EitherMarker<DomainError>, TValue>;
```

#### Для трансформеров: `EitherTAlias<TAliasMarker, TOuterMarker, TError, TValue>`

EitherT-специфичный инфраструктурный тип в `StyliSH.Implementations`:

```csharp
public readonly record struct EitherTAlias<TAliasMarker, TOuterMarker, TError, TValue>(
    EitherT<TOuterMarker, TError, TValue> Inner)
    : ITransformer<TAliasMarker, TOuterMarker, TValue>,
      ITransformerRunner<TOuterMarker, Either<TError, TValue>, TValue>,
      IMonadUnwrapper<EitherTAlias<TAliasMarker, TOuterMarker, TError, TValue>, TAliasMarker, TValue>
    where TAliasMarker : ITransformerMarker<TAliasMarker, TOuterMarker>
    where TOuterMarker : IMonadMarker<TOuterMarker>
{
    public IMonad<TAliasMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new EitherTAlias<TAliasMarker, TOuterMarker, TError, TNewValue>(
            (EitherT<TOuterMarker, TError, TNewValue>)Inner.RawMap(map).Wrap());

    public IMonad<TAliasMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<TAliasMarker, TNewValue>> bind)
        => new EitherTAlias<TAliasMarker, TOuterMarker, TError, TNewValue>(
            (EitherT<TOuterMarker, TError, TNewValue>)Inner.RawBind(value =>
            {
                var aliasResult = (EitherTAlias<TAliasMarker, TOuterMarker, TError, TNewValue>)bind(value);
                return aliasResult.Inner;
            }).Wrap());

    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMarker, Either<TError, TValue>>,
                            IMonadUnwrapper<TOuterMonad, TOuterMarker, Either<TError, TValue>>
        => Inner.Run<TOuterMonad>();

    public static implicit operator EitherTAlias<TAliasMarker, TOuterMarker, TError, TValue>(
        MonadWrapper<TAliasMarker, TValue> monad)
        => IMonadUnwrapper<EitherTAlias<TAliasMarker, TOuterMarker, TError, TValue>, TAliasMarker, TValue>
            .CastFrom(monad);
}
```

**Использование:**
```csharp
// Маркер трансформерного алиаса
public readonly record struct DomainResultTMarker
    : ITransformerMarker<DomainResultTMarker, TaskMarker>
{
    public static IMonad<DomainResultTMarker, TValue> Pure<TValue>(TValue value)
        => new EitherTAlias<DomainResultTMarker, TaskMarker, DomainError, TValue>(
            (EitherT<TaskMarker, DomainError, TValue>)
            EitherTMarker<TaskMarker, DomainError>.Pure(value));

    public static MonadWrapper<DomainResultTMarker, TValue> FromError<TValue>(DomainError error)
        => new EitherTAlias<DomainResultTMarker, TaskMarker, DomainError, TValue>(
            EitherT<TaskMarker, DomainError, TValue>.FromError(error)).Wrap();
}

global using DomainResultT<TValue> =
    StyliSH.Implementations.Aliases.EitherTAlias<DomainResultTMarker, TaskMarker, DomainError, TValue>;
```

---

## Code References

- `StyliSH.Abstractions/Monads/IMonadMarker.cs` — маркер-интерфейс с `static abstract Pure`
- `StyliSH.Abstractions/Monads/IMonad.cs:6-21` — интерфейс монады (Map/Bind/RawMap/RawBind)
- `StyliSH.Abstractions/Monads/IMonad.cs:26-36` — `IMonadUnwrapper` + `CastFrom`
- `StyliSH.Abstractions/Monads/MonadWrapper.cs` — тип-стёртая обёртка
- `StyliSH.Abstractions/Monads/Transformers/ITransformerMarker.cs` — маркер трансформера
- `StyliSH.Abstractions/Monads/Transformers/ITransformer.cs` — интерфейс трансформера
- `StyliSH.Abstractions/Monads/Transformers/ITransformerRunner.cs` — `Run<TOuterMonad>()`
- `StyliSH.Abstractions/Extensions/MonadExtensions.cs` — `.Wrap()` extension method
- `StyliSH.Implementations/Monads/Either/Either.cs` — `Either<TError, TValue>` реализация
- `StyliSH.Implementations/Monads/Either/EitherMarker.cs` — `EitherMarker<TError>` + `Match`
- `StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs` — `EitherT` реализация
- `StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs` — `EitherTMarker` реализация

---

## Historical Context (from thoughts/)

- `thoughts/shared/plans/2026-03-22-monad-test-implementation.md` — план тестирования монад (завершён), даёт контекст о тестовой инфраструктуре
- `thoughts/shared/research/2026-03-22-monad-test-implementation.md` — исследование тестирования монад

---

## Open Questions

1. **Расположение `MonadAlias`**: В `StyliSH.Abstractions` (универсальная) или `StyliSH.Implementations` (т.к. использует конкретные типы в маркере)? Скорее всего, `MonadAlias<,,>` в абстракциях, `EitherTAlias<,,,>` в реализациях.

2. **`FromError`/`FromResult` на маркере vs отдельный интерфейс**: Текущее решение — методы на маркере. Можно ввести интерфейс `IEitherAliasMarker<TSelf, TError>` с `static abstract FromError<TValue>(TError)` для унификации.

3. **Глубина общего механизма**: `MonadAlias` универсален для любых монад; `EitherTAlias` специфичен для `EitherT`.

   **Можно ли сделать generic `TransformerAlias`, аналогичный `MonadAlias`?**

   Да, и `RawBind` здесь не проблема. Точно так же как `MonadAlias` хранит `IMonad<TInnerMarker, TValue>` и делегирует `Inner.RawBind(...)`, generic `TransformerAlias` может хранить `IMonad<TInnerTransformerMarker, TValue>` и делегировать тот же `Inner.RawBind(...)`. Никакого `EitherMarker.Match` не нужно — это нужно только если бы мы реализовывали RawBind трансформера с нуля.

   **Настоящая проблема — `ITransformerRunner`.**

   `ITransformerRunner<TOuterMarker, TInnerMonad, TValue>` параметризован `TInnerMonad` — конкретным типом того, что хранится во внешней монаде после `Run()`. Для `EitherT` это `Either<TError, TValue>`. Generic `TransformerAlias`, хранящий `IMonad<TInnerTransformerMarker, TValue>`, не знает этот тип — информация о нём потеряна при стирании к интерфейсу.

   Решение: добавить `TInnerMonad` как явный type parameter:
   ```csharp
   public readonly record struct TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>(
       IMonad<TInnerTransformerMarker, TValue> Inner)
       : ITransformer<TAliasMarker, TOuterMarker, TValue>,
         ITransformerRunner<TOuterMarker, TInnerMonad, TValue>,
         IMonadUnwrapper<TransformerAlias<...>, TAliasMarker, TValue>
       where TAliasMarker : ITransformerMarker<TAliasMarker, TOuterMarker>
       where TInnerTransformerMarker : ITransformerMarker<TInnerTransformerMarker, TOuterMarker>
       where TOuterMarker : IMonadMarker<TOuterMarker>
   {
       // RawMap/RawBind делегируют Inner — так же как MonadAlias
       public IMonad<TAliasMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
           => new TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>(Inner.RawMap(map));

       public IMonad<TAliasMarker, TNewValue> RawBind<TNewValue>(Func<TValue, IMonad<TAliasMarker, TNewValue>> bind)
           => new TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>(
               Inner.RawBind(value =>
                   ((TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>)bind(value)).Inner));

       // Run: кастуем Inner к ITransformerRunner
       public TOuterMonad Run<TOuterMonad>()
           where TOuterMonad : IMonad<TOuterMarker, TInnerMonad>, IMonadUnwrapper<TOuterMonad, TOuterMarker, TInnerMonad>
           => ((ITransformerRunner<TOuterMarker, TInnerMonad, TValue>)Inner).Run<TOuterMonad>();
   }
   ```

   Это работает. Недостаток — 5 type parameters. Сравним:
   - `EitherTAlias<TAliasMarker, TOuterMarker, TError, TValue>` — 4 параметра, хранит конкретный `EitherT`, нет runtime cast в `Run()`
   - `TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>` — 5 параметров, хранит интерфейс, есть runtime cast в `Run()`, зато работает с любым трансформером

   **Вывод**: оба подхода жизнеспособны. Если трансформеров планируется несколько — generic `TransformerAlias` предпочтительнее. Если только `EitherT` — `EitherTAlias` проще.

4. **Именование**: `MonadAlias` vs `NewtypeMonad` vs `AliasMonad`. Первый вариант более говорящий.

5. **C# generic type aliases (C# 12)**: `global using T<V> = Foo<Bar, Baz, V>` работает, но IDE-поддержка в Rider может быть неполной. Стоит проверить.
