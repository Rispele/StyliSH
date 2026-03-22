namespace StyliSH.Abstractions.Monads.Transformers;

public interface ITransformerRunner<TOuterMonadMarker, TInnerMonad, TValue>
    where TOuterMonadMarker : IMonadMarker<TOuterMonadMarker>
{
    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMonadMarker, TInnerMonad>,
                            IMonadUnwrapper<TOuterMonad, TOuterMonadMarker, TInnerMonad>;
}
