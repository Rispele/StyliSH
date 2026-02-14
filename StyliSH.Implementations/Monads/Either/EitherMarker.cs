using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Either;

public readonly record struct EitherMarker<TError> : IMonadMarker<EitherMarker<TError>>
{
    public static IMonad<EitherMarker<TError>, TValue> Pure<TValue>(TValue value)
    {
        return new Either<TError, TValue>(IsSuccess: true, Error: default, Value: value);
    }
}