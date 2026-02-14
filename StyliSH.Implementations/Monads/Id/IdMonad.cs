using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Id;

public readonly record struct IdMarker : IMonadMarker<IdMarker>
{
    public static IMonad<IdMarker, TValue> Pure<TValue>(TValue value)
    {
        return new IdMonad<TValue>(value);
    }
}

public readonly record struct IdMonad<TValue>(TValue Value) :
    IMonad<IdMarker, TValue>,
    IMonadUnwrapper<IdMonad<TValue>, IdMarker, TValue>
{
    public IMonad<IdMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
    {
        return IdMarker.Pure(map(Value));
    }

    public IMonad<IdMarker, TNewValue> RawBind<TNewValue>(Func<TValue, IMonad<IdMarker, TNewValue>> bind)
    {
        return bind(Value);
    }

    public static implicit operator IdMonad<TValue>(MonadWrapper<IdMarker, TValue> monad)
    {
        return monad.Monad is IdMonad<TValue> idMonad
            ? idMonad
            : throw new InvalidCastException();
    }
}