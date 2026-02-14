namespace StyliSH.Abstractions.Monads;

/// <summary>
/// Monad interface
/// </summary>
public interface IMonad<TMonadMarker, out TValue>
    where TMonadMarker : IMonadMarker<TMonadMarker>
{
    public MonadWrapper<TMonadMarker, TNewValue> Map<TNewValue>(Func<TValue, TNewValue> map) => RawMap(map).Wrap();
    public MonadWrapper<TMonadMarker, TNewValue> Bind<TNewValue>(Func<TValue, IMonad<TMonadMarker, TNewValue>> bind) => RawBind(bind).Wrap();

    public MonadWrapper<TMonadMarker, TNewValue> Bind<TNewValue>(Func<TValue, MonadWrapper<TMonadMarker, TNewValue>> bind)
    {
        return Bind(BindInner);

        IMonad<TMonadMarker, TNewValue> BindInner(TValue value) => bind(value).Monad;
    }

    public IMonad<TMonadMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map);
    public IMonad<TMonadMarker, TNewValue> RawBind<TNewValue>(Func<TValue, IMonad<TMonadMarker, TNewValue>> bind);
}

/// <summary>
/// Used for cast from MonadWrapper to concrete monad
/// </summary>
public interface IMonadUnwrapper<out TSelf, TMonadMarker, TValue>
    where TMonadMarker : IMonadMarker<TMonadMarker>
    where TSelf : IMonadUnwrapper<TSelf, TMonadMarker, TValue>, IMonad<TMonadMarker, TValue>
{
    public static abstract implicit operator TSelf(MonadWrapper<TMonadMarker, TValue> monad);
}