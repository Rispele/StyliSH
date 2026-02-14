using StyliSH.Abstractions.Monads;
using StyliSH.Abstractions.Monads.Transformers;
using StyliSH.Implementations.Monads.Either;

namespace StyliSH.Implementations.Monads.Transformers.Either;

public readonly record struct EitherTMarker<TOuterMarker, TError>
    : ITransformerMarker<EitherTMarker<TOuterMarker, TError>, TOuterMarker>
    where TOuterMarker : IMonadMarker<TOuterMarker>
{
    public static IMonad<EitherTMarker<TOuterMarker, TError>, TValue> Pure<TValue>(TValue value)
    {
        IMonad<EitherMarker<TError>, TValue> innerMonad = new Either<TError, TValue>(true, default, value);

        return new EitherT<TOuterMarker, TError, TValue>(TOuterMarker.Pure(innerMonad));
    }
}
