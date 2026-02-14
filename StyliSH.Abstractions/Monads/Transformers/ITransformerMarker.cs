namespace StyliSH.Abstractions.Monads.Transformers;

/// <summary>
/// Transformer marker interface 
/// </summary>
public interface ITransformerMarker<TSelf, TOuterMonadMarker> : IMonadMarker<TSelf>
    where TSelf : ITransformerMarker<TSelf, TOuterMonadMarker>
    where TOuterMonadMarker : IMonadMarker<TOuterMonadMarker>;