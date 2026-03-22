using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Either;

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
