# Список изменений

## StyliSH.Abstractions/Monads/IMonad.cs
- Добавлен `public static CastFrom` в `IMonadUnwrapper<TSelf, TMonadMarker, TValue>`
- Примечание: вызывается только через квалифицированное имя интерфейса:
  `IMonadUnwrapper<TSelf, TMarker, TValue>.CastFrom(monad)`

## StyliSH.Abstractions/Monads/Transformers/ITransformerRunner.cs
- Убран параметр `TInnerMonadMarker` (было 4 параметра, стало 3)
- Удалено ограничение `where TInnerMonad : IMonad<TInnerMonadMarker, TValue>`

## StyliSH.Implementations/Monads/Either/Either.cs
- Позиционный конструктор заменён на приватный `Either(bool, TError?, TValue?)`
- Добавлен `Match<TResult>(onError, onSuccess)`
- Добавлен `internal static Success(TValue)` — краткий конструктор успешного значения
- `RawMap` и `RawBind` переписаны через `Match` (убраны `!`-подавители)
- Implicit operator упрощён через `CastFrom`

## StyliSH.Implementations/Monads/Either/EitherMarker.cs
- `Pure` обновлён: использует `Either<TError, TValue>.Success(value)` вместо `new Either(...)`
- Добавлен `static Match<TValue, TResult>(IMonad<...>, onError, onSuccess)` для диспатча без даункаста

## StyliSH.Implementations/Monads/Transformers/Either/EitherT.cs
- `ITransformerRunner` обновлён до 3 параметров
- Добавлен приватный `ExtractOuter<TNewValue>` — именованный каст в `RawBind`
- `RawBind` переписан через `EitherMarker<TError>.Match` (убран явный даункаст `(Either<TError, TValue>)value`)
- Implicit operator упрощён через `CastFrom`

## StyliSH.Implementations/Monads/Transformers/Either/EitherTMarker.cs
- `Pure` обновлён: использует `Either<TError, TValue>.Success(value)` вместо `new Either(true, default, value)`

## StyliSH.Implementations/Monads/Id/IdMonad.cs
- Implicit operator упрощён через `CastFrom`

## StyliSH.Implementations/Monads/Tasks/TaskMonad.cs
- Implicit operator упрощён через `CastFrom`
