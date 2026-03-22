namespace StyliSH.Abstractions.Monads;

public interface IEitherLikeMarker<TSelf, TError> : IMonadMarker<TSelf>
    where TSelf : IEitherLikeMarker<TSelf, TError>
{
    static abstract MonadWrapper<TSelf, TValue> FromError<TValue>(TError error);
    static abstract MonadWrapper<TSelf, TValue> FromValue<TValue>(TValue value);
}
