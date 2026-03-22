namespace StyliSH.Abstractions.Monads.Aliases;

public readonly record struct MonadAlias<TAliasMarker, TInnerMarker, TValue>(
    IMonad<TInnerMarker, TValue> Inner)
    : IMonad<TAliasMarker, TValue>,
      IMonadUnwrapper<MonadAlias<TAliasMarker, TInnerMarker, TValue>, TAliasMarker, TValue>
    where TAliasMarker : IMonadMarker<TAliasMarker>
    where TInnerMarker : IMonadMarker<TInnerMarker>
{
    public IMonad<TAliasMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new MonadAlias<TAliasMarker, TInnerMarker, TNewValue>(Inner.RawMap(map));

    public IMonad<TAliasMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<TAliasMarker, TNewValue>> bind)
        => new MonadAlias<TAliasMarker, TInnerMarker, TNewValue>(
            Inner.RawBind(value =>
                ((MonadAlias<TAliasMarker, TInnerMarker, TNewValue>)bind(value)).Inner));

    public static implicit operator MonadAlias<TAliasMarker, TInnerMarker, TValue>(
        MonadWrapper<TAliasMarker, TValue> monad)
        => IMonadUnwrapper<MonadAlias<TAliasMarker, TInnerMarker, TValue>, TAliasMarker, TValue>
            .CastFrom(monad);
}
