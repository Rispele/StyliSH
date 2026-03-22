# Рефакторинг: Упрощение добавления монад и трансформеров

## Контекст

Текущая библиотека StyliSH имеет правильную архитектуру (marker-type pattern, static abstract), но каждая новая монада/трансформер требует значительного бойлерплейта и хрупких каст-операций. Цель — снизить трение при добавлении новых типов, не меняя фундаментальный дизайн.

## Что НЕ меняем

- Паттерн маркер-тип + `static abstract`
- `MonadWrapper<TMarker, TValue>` как type-erased обёртка
- Разделение `RawMap`/`RawBind` (реализация) и `Map`/`Bind` (API с обёрткой)
- Дублирование Bind-перегрузок между `IMonad` и `MonadWrapper` — фиксированная стоимость в слое абстракций, не повторяется при добавлении монад

---

## Изменения

### 1. Хелпер `CastFrom` в `IMonadUnwrapper`

**Файл:** `StyliSH.Abstractions/Monads/IMonad.cs`

Добавить `protected static` хелпер — каждый неявный оператор сокращается до однострочного.

```csharp
public interface IMonadUnwrapper<out TSelf, TMonadMarker, TValue>
    where TMonadMarker : IMonadMarker<TMonadMarker>
    where TSelf : IMonadUnwrapper<TSelf, TMonadMarker, TValue>, IMonad<TMonadMarker, TValue>
{
    public static abstract implicit operator TSelf(MonadWrapper<TMonadMarker, TValue> monad);

    protected static TSelf CastFrom(MonadWrapper<TMonadMarker, TValue> monad)
        => monad.Monad is TSelf concrete
            ? concrete
            : throw new InvalidCastException($"Expected {typeof(TSelf).Name} but found {monad.Monad.GetType().Name}");
}
```

Все четыре реализации (`IdMonad`, `Either`, `TaskMonad`, `EitherT`) сокращают оператор:

```csharp
// До (3 строки):
public static implicit operator IdMonad<TValue>(MonadWrapper<IdMarker, TValue> monad)
{
    return monad.Monad is IdMonad<TValue> x ? x : throw new InvalidCastException();
}

// После (1 строка):
public static implicit operator IdMonad<TValue>(MonadWrapper<IdMarker, TValue> monad) => CastFrom(monad);
```

---

### 2. `Either` — приватный конструктор + `Match`

**Файл:** `StyliSH.Implementations/Monads/Either/Either.cs`

Убрать публичный позиционный конструктор. Добавить `Match` — устраняет `!`-подавители в `RawMap`/`RawBind`.

```csharp
public readonly record struct Either<TError, TValue>
    : IMonad<EitherMarker<TError>, TValue>,
      IMonadUnwrapper<Either<TError, TValue>, EitherMarker<TError>, TValue>
{
    private readonly bool _isSuccess;
    private readonly TError? _error;
    private readonly TValue? _value;

    private Either(bool isSuccess, TError? error, TValue? value)
    { _isSuccess = isSuccess; _error = error; _value = value; }

    public TResult Match<TResult>(Func<TError, TResult> onError, Func<TValue, TResult> onSuccess)
        => _isSuccess ? onSuccess(_value!) : onError(_error!);

    public static MonadWrapper<EitherMarker<TError>, TValue> FromError(TError error)
        => new Either<TError, TValue>(false, error, default).Wrap();

    public static MonadWrapper<EitherMarker<TError>, TValue> FromValue(TValue value)
        => new Either<TError, TValue>(true, default, value).Wrap();

    internal static Either<TError, TValue> Success(TValue value) => new(true, default, value);

    public IMonad<EitherMarker<TError>, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => Match(
            onError: e => Either<TError, TNewValue>.FromError(e).Monad,
            onSuccess: v => Either<TError, TNewValue>.FromValue(map(v)).Monad);

    public IMonad<EitherMarker<TError>, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<EitherMarker<TError>, TNewValue>> bind)
        => Match(
            onError: e => Either<TError, TNewValue>.FromError(e).Monad,
            onSuccess: v => bind(v));

    public static implicit operator Either<TError, TValue>(MonadWrapper<EitherMarker<TError>, TValue> monad)
        => CastFrom(monad);
}
```

---

### 3. Статический `Match` в `EitherMarker`

**Файл:** `StyliSH.Implementations/Monads/Either/EitherMarker.cs`

Добавить метод для диспатча по `IMonad<EitherMarker<TError>, TValue>` без каста к конкретному типу.
Используется в `EitherT.RawBind` — устраняет явный даункаст `(Either<TError, TValue>)value`.

```csharp
public readonly record struct EitherMarker<TError> : IMonadMarker<EitherMarker<TError>>
{
    public static IMonad<EitherMarker<TError>, TValue> Pure<TValue>(TValue value)
        => Either<TError, TValue>.Success(value);

    public static TResult Match<TValue, TResult>(
        IMonad<EitherMarker<TError>, TValue> monad,
        Func<TError, TResult> onError,
        Func<TValue, TResult> onSuccess)
        => monad is Either<TError, TValue> either
            ? either.Match(onError, onSuccess)
            : throw new InvalidOperationException($"Expected Either<{typeof(TError).Name},{typeof(TValue).Name}>");
}
```

---

### 4. Упрощение `ITransformerRunner` — убрать `TInnerMonadMarker`

**Файл:** `StyliSH.Abstractions/Monads/Transformers/ITransformerRunner.cs`

`TInnerMonadMarker` существует только для ограничения `where TInnerMonad : IMonad<TInnerMonadMarker, TValue>`,
которое нигде не используется внутри интерфейса. Убрать — 4 параметра → 3.

```csharp
public interface ITransformerRunner<TOuterMonadMarker, TInnerMonad, TValue>
    where TOuterMonadMarker : IMonadMarker<TOuterMonadMarker>
{
    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMonadMarker, TInnerMonad>,
                            IMonadUnwrapper<TOuterMonad, TOuterMonadMarker, TInnerMonad>;
}
```

---

### 5. Рефакторинг `EitherT`

**Файл:** `StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs`

- Обновить `ITransformerRunner` → 3 параметра
- Добавить приватный `ExtractOuter` — именованный каст в `RawBind`
- Переписать `RawBind` через `EitherMarker<TError>.Match`

```csharp
public readonly record struct EitherT<TOuterMarker, TError, TValue>(
    IMonad<TOuterMarker, IMonad<EitherMarker<TError>, TValue>> OuterMonad) :
    ITransformer<EitherTMarker<TOuterMarker, TError>, TOuterMarker, TValue>,
    ITransformerRunner<TOuterMarker, Either<TError, TValue>, TValue>,  // 3 параметра
    IMonadUnwrapper<EitherT<TOuterMarker, TError, TValue>, EitherTMarker<TOuterMarker, TError>, TValue>
    where TOuterMarker : IMonadMarker<TOuterMarker>
{
    // ... FromError, FromValue, RawMap без изменений ...

    private static IMonad<TOuterMarker, IMonad<EitherMarker<TError>, TNewValue>> ExtractOuter<TNewValue>(
        IMonad<EitherTMarker<TOuterMarker, TError>, TNewValue> monad)
        => monad is EitherT<TOuterMarker, TError, TNewValue> t
            ? t.OuterMonad
            : throw new InvalidOperationException($"Expected EitherT, got {monad.GetType().Name}");

    public IMonad<EitherTMarker<TOuterMarker, TError>, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<EitherTMarker<TOuterMarker, TError>, TNewValue>> bind)
    {
        var bound = OuterMonad.RawBind(value =>
            EitherMarker<TError>.Match(value,
                onError: e => TOuterMarker.Pure(Either<TError, TNewValue>.FromError(e).Monad),
                onSuccess: v => ExtractOuter(bind(v))));
        return new EitherT<TOuterMarker, TError, TNewValue>(bound);
    }

    public static implicit operator EitherT<TOuterMarker, TError, TValue>(
        MonadWrapper<EitherTMarker<TOuterMarker, TError>, TValue> monad) => CastFrom(monad);
}
```

**Файл:** `StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs`

Заменить `new Either<TError, TValue>(true, default, value)` на `Either<TError, TValue>.Success(value)`.

---

## Порядок применения

1. `StyliSH.Abstractions/Monads/IMonad.cs` — добавить `CastFrom`
2. `StyliSH.Implementations/Monads/Either/Either.cs` — приватный конструктор + `Match` + `Success`
3. `StyliSH.Implementations/Monads/Either/EitherMarker.cs` — обновить `Pure`, добавить `Match`
4. `StyliSH.Abstractions/Monads/Transformers/ITransformerRunner.cs` — убрать `TInnerMonadMarker`
5. `StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs` — обновить объявления, добавить `ExtractOuter`, переписать `RawBind`
6. `StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs` — обновить `Pure`
7. `StyliSH.Implementations/Monads/Id/IdMonad.cs` — implicit operator → `CastFrom`
8. `StyliSH.Implementations/Monads/Tasks/TaskMonad.cs` — implicit operator → `CastFrom`

## Верификация

```bash
dotnet build StyliSH.sln
dotnet run --project StyliSH/StyliSH.csproj
```
