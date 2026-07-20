namespace StyliSH.Abstractions.Monads;

/// <summary>
/// Useful for casting interfaces to monad implementation depending on monad marker 
/// </summary>
public readonly record struct MonadWrapper<TMarker, TValue>(IMonad<TMarker, TValue> Monad)
    where TMarker : IMonadMarker<TMarker>
{
    public MonadWrapper<TMarker, TNewValue> Map<TNewValue>(Func<TValue, TNewValue> map)
    {
        return Monad.Map(map);
    }

    public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(Func<TValue, IMonad<TMarker, TNewValue>> bind)
    {
        return Monad.Bind(bind);
    }

    public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(Func<TValue, MonadWrapper<TMarker, TNewValue>> bind)
    {
        return Monad.Bind(bind);
    }

    public MonadWrapper<TMarker, TNewValue> Map<TNewValue>(
        Func<TValue, TNewValue> map,
        out IValueWrapper<TNewValue> newValue)
    {
        return Monad.Map(map, out newValue);
    }

    public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(
        Func<TValue, IMonad<TMarker, TNewValue>> bind,
        out IValueWrapper<TNewValue> newValue)
    {
        return Monad.Bind(bind, out newValue);
    }

    public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(
        Func<TValue, MonadWrapper<TMarker, TNewValue>> bind,
        out IValueWrapper<TNewValue> newValue)
    {
        return Monad.Bind(bind, out newValue);
    }
}