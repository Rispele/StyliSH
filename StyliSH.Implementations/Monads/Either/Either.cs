using StyliSH.Abstractions;
using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Either;

public readonly record struct Either<TError, TValue>(bool IsSuccess, TError? Error, TValue? Value) :
    IMonad<EitherMarker<TError>, TValue>,
    IMonadUnwrapper<Either<TError, TValue>, EitherMarker<TError>, TValue>
{
    public static MonadWrapper<EitherMarker<TError>, TValue> FromError(TError error)
    {
        return new Either<TError, TValue>(IsSuccess: false, error, default).Wrap();
    }

    public static MonadWrapper<EitherMarker<TError>, TValue> FromValue(TValue value)
    {
        return new Either<TError, TValue>(IsSuccess: true, Error: default, value).Wrap();
    }

    public IMonad<EitherMarker<TError>, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
    {
        return IsSuccess
            ? Either<TError, TNewValue>.FromValue(map(Value!)).Monad
            : Either<TError, TNewValue>.FromError(Error!).Monad;
    }

    public IMonad<EitherMarker<TError>, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<EitherMarker<TError>, TNewValue>> bind)
    {
        return IsSuccess
            ? bind(Value!)
            : Either<TError, TNewValue>.FromError(Error!).Monad;
    }

    public static implicit operator Either<TError, TValue>(MonadWrapper<EitherMarker<TError>, TValue> monad)
    {
        return monad.Monad is Either<TError, TValue> either
            ? either
            : throw new InvalidCastException();
    }
}