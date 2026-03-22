using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Either;

public readonly record struct EitherMarker<TError> : IEitherLikeMarker<EitherMarker<TError>, TError>
{
    public static IMonad<EitherMarker<TError>, TValue> Pure<TValue>(TValue value)
        => Either<TError, TValue>.Success(value);

    public static MonadWrapper<EitherMarker<TError>, TValue> FromError<TValue>(TError error)
        => Either<TError, TValue>.FromError(error);

    public static MonadWrapper<EitherMarker<TError>, TValue> FromValue<TValue>(TValue value)
        => Either<TError, TValue>.FromValue(value);

    public static TResult Match<TValue, TResult>(
        IMonad<EitherMarker<TError>, TValue> monad,
        Func<TError, TResult> onError,
        Func<TValue, TResult> onSuccess)
        => monad is Either<TError, TValue> either
            ? either.Match(onError, onSuccess)
            : throw new InvalidOperationException($"Expected Either<{typeof(TError).Name},{typeof(TValue).Name}>");
}
