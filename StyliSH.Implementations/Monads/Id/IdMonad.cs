using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Id;

public readonly record struct IdMarker : IMonadMarker<IdMarker>
{
    public static IMonad<IdMarker, TValue> Pure<TValue>(TValue value)
    {
        return new IdMonad<TValue>(value);
    }
}

public readonly record struct IdMonad<TValue>(TValue ActualValue) :
    IMonad<IdMarker, TValue>,
    IMonadUnwrapper<IdMonad<TValue>, IdMarker, TValue>
{
    public IValueWrapper<TValue> Value => new ValueWrapper<TValue>(ActualValue, isInitialized: true);

    public IMonad<IdMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
    {
        return IdMarker.Pure(map(ActualValue));
    }

    public IMonad<IdMarker, TNewValue> RawBind<TNewValue>(Func<TValue, IMonad<IdMarker, TNewValue>> bind)
    {
        return bind(ActualValue);
    }

    public static implicit operator IdMonad<TValue>(MonadWrapper<IdMarker, TValue> monad)
        => IMonadUnwrapper<IdMonad<TValue>, IdMarker, TValue>.CastFrom(monad);
}