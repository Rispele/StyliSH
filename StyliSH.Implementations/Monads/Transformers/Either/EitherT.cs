using StyliSH.Abstractions;
using StyliSH.Abstractions.Monads;
using StyliSH.Abstractions.Monads.Transformers;
using StyliSH.Implementations.Monads.Either;

namespace StyliSH.Implementations.Monads.Transformers.Either;

public readonly record struct EitherT<TOuterMarker, TError, TValue>(
    IMonad<TOuterMarker, IMonad<EitherMarker<TError>, TValue>> OuterMonad) :
    ITransformer<EitherTMarker<TOuterMarker, TError>, TOuterMarker, TValue>,
    ITransformerRunner<TOuterMarker, Either<TError, TValue>, TValue>,
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

    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMarker, Either<TError, TValue>>,
        IMonadUnwrapper<TOuterMonad, TOuterMarker, Either<TError, TValue>>
    {
        return (TOuterMonad)OuterMonad.Map(value => (Either<TError, TValue>)value).Monad;
    }

    public static implicit operator EitherT<TOuterMarker, TError, TValue>(
        MonadWrapper<EitherTMarker<TOuterMarker, TError>, TValue> monad)
        => IMonadUnwrapper<EitherT<TOuterMarker, TError, TValue>, EitherTMarker<TOuterMarker, TError>, TValue>
            .CastFrom(monad);
}