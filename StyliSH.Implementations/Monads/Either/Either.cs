using StyliSH.Abstractions;
using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Either;

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
        => IMonadUnwrapper<Either<TError, TValue>, EitherMarker<TError>, TValue>.CastFrom(monad);
}
