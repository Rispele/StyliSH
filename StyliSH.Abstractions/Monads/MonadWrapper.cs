namespace StyliSH.Abstractions.Monads;

/// <summary>
/// Useful for casting interfaces to monad implementation depending on monad marker 
/// </summary>
public readonly record struct MonadWrapper<TMarker, TValue>(IMonad<TMarker, TValue> Monad)
    where TMarker : IMonadMarker<TMarker>
{
    public MonadWrapper<TMarker, TNewValue> Map<TNewValue>(Func<TValue, TNewValue> map)
    {
        return new MonadWrapper<TMarker, TNewValue>(Monad.RawMap(map));
    }

    public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(Func<TValue, IMonad<TMarker, TNewValue>> bind)
    {
        return new MonadWrapper<TMarker, TNewValue>(Monad.RawBind(bind));
    }
    
    public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(Func<TValue, MonadWrapper<TMarker, TNewValue>> bind)
    {
        return Bind(BindInner);

        IMonad<TMarker, TNewValue> BindInner(TValue value) => bind(value).Monad;
    }
}