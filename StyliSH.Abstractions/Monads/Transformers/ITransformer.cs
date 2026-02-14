namespace StyliSH.Abstractions.Monads.Transformers;

/// <summary>
/// Transformer interface
/// </summary>
public interface ITransformer<TTransformerMarker, TOuterMonadMarker, out TValue> : IMonad<TTransformerMarker, TValue>
    where TTransformerMarker : ITransformerMarker<TTransformerMarker, TOuterMonadMarker>
    where TOuterMonadMarker : IMonadMarker<TOuterMonadMarker>;