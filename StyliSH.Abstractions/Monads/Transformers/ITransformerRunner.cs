namespace StyliSH.Abstractions.Monads.Transformers;

public interface ITransformerRunner<TOuterMonadMarker, TInnerMonad, TInnerMonadMarker, TValue>
    where TOuterMonadMarker : IMonadMarker<TOuterMonadMarker>
    where TInnerMonad : IMonad<TInnerMonadMarker, TValue>
    where TInnerMonadMarker : IMonadMarker<TInnerMonadMarker>
{
    public TOuterMonad Run<TOuterMonad>()
        where TOuterMonad : IMonad<TOuterMonadMarker, TInnerMonad>, IMonadUnwrapper<TOuterMonad, TOuterMonadMarker, TInnerMonad>;
}