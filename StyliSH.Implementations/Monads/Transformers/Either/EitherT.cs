using StyliSH.Abstractions;
using StyliSH.Abstractions.Monads;
using StyliSH.Abstractions.Monads.Transformers;
using StyliSH.Implementations.Monads.Either;

namespace StyliSH.Implementations.Monads.Transformers.Either;

public readonly record struct EitherT<TOuterMarker, TError, TValue>(IMonad<TOuterMarker, IMonad<EitherMarker<TError>, TValue>> OuterMonad) :
    ITransformer<EitherTMarker<TOuterMarker, TError>, TOuterMarker, TValue>,
    ITransformerRunner<TOuterMarker, Either<TError, TValue>, EitherMarker<TError>, TValue>,
    IMonadUnwrapper<EitherT<TOuterMarker, TError, TValue>, EitherTMarker<TOuterMarker, TError>, TValue>
    where TOuterMarker : IMonadMarker<TOuterMarker>
{
    public static MonadWrapper<EitherTMarker<TOuterMarker, TError>, TValue> FromError(TError error)
    {
        var monad = TOuterMarker.Pure(Either<TError, TValue>.FromError(error).Monad);
        return new EitherT<TOuterMarker, TError, TValue>(monad).Wrap();
    }

    public static MonadWrapper<EitherTMarker<TOuterMarker, TError>, TValue> FromValue(TValue value)
    {
        var monad = TOuterMarker.Pure(Either<TError, TValue>.FromValue(value).Monad);
        return new EitherT<TOuterMarker, TError, TValue>(monad).Wrap();
    }

    public IMonad<EitherTMarker<TOuterMarker, TError>, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
    {
        return new EitherT<TOuterMarker, TError, TNewValue>(OuterMonad.RawMap(value => value.RawMap(map)));
    }

    public IMonad<EitherTMarker<TOuterMarker, TError>, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<EitherTMarker<TOuterMarker, TError>, TNewValue>> bind)
    {
        var bound = OuterMonad.RawBind(value =>
        {
            var either = (Either<TError, TValue>)value;
            if (either.IsSuccess)
            {
                return bind(either.Value!) is EitherT<TOuterMarker, TError, TNewValue> eitherT
                    ? eitherT.OuterMonad
                    : throw new InvalidOperationException();
            }

            return TOuterMarker.Pure(Either<TError, TNewValue>.FromError(either.Error!).Monad);
        });

        return new EitherT<TOuterMarker, TError, TNewValue>(bound);
    }

    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMarker, Either<TError, TValue>>, IMonadUnwrapper<TOuterMonad, TOuterMarker, Either<TError, TValue>>
    {
        return (TOuterMonad)OuterMonad.Map(value => (Either<TError, TValue>)value).Monad;
    }

    public static implicit operator EitherT<TOuterMarker, TError, TValue>(MonadWrapper<EitherTMarker<TOuterMarker, TError>, TValue> monad)
    {
        return monad.Monad is EitherT<TOuterMarker, TError, TValue> casted
            ? casted
            : throw new InvalidOperationException();
    }
}