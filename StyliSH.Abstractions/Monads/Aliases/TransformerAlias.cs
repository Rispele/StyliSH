using StyliSH.Abstractions.Monads.Transformers;

namespace StyliSH.Abstractions.Monads.Aliases;

public readonly record struct TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>(
    IMonad<TInnerTransformerMarker, TValue> Inner)
    : ITransformer<TAliasMarker, TOuterMarker, TValue>,
      ITransformerRunner<TOuterMarker, TInnerMonad, TValue>,
      IMonadUnwrapper<TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>, TAliasMarker, TValue>
    where TAliasMarker : ITransformerMarker<TAliasMarker, TOuterMarker>
    where TInnerTransformerMarker : ITransformerMarker<TInnerTransformerMarker, TOuterMarker>
    where TOuterMarker : IMonadMarker<TOuterMarker>
{
    public IMonad<TAliasMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
        => new TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>(
            Inner.RawMap(map));

    public IMonad<TAliasMarker, TNewValue> RawBind<TNewValue>(
        Func<TValue, IMonad<TAliasMarker, TNewValue>> bind)
        => new TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>(
            Inner.RawBind(value =>
                ((TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TNewValue>)bind(value)).Inner));

    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMarker, TInnerMonad>,
                            IMonadUnwrapper<TOuterMonad, TOuterMarker, TInnerMonad>
        => ((ITransformerRunner<TOuterMarker, TInnerMonad, TValue>)Inner).Run<TOuterMonad>();

    public static implicit operator TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>(
        MonadWrapper<TAliasMarker, TValue> monad)
        => IMonadUnwrapper<TransformerAlias<TAliasMarker, TInnerTransformerMarker, TOuterMarker, TInnerMonad, TValue>, TAliasMarker, TValue>
            .CastFrom(monad);
}
