namespace StyliSH.Abstractions.Monads;

/// <summary>
/// Used for defining monad type
/// </summary>
public interface IMonadMarker<TSelf> 
    where TSelf : IMonadMarker<TSelf>
{
    public static abstract IMonad<TSelf, TValue> Pure<TValue>(TValue value);
}