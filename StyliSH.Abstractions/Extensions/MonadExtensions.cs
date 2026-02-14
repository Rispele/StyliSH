using StyliSH.Abstractions.Monads;

namespace StyliSH.Abstractions;

public static class MonadExtensions
{
    public static MonadWrapper<TMarker, TValue> Wrap<TMarker, TValue>(this IMonad<TMarker, TValue> monad)
        where TMarker : IMonadMarker<TMarker>
    {
        return new MonadWrapper<TMarker, TValue>(monad);
    }
    
    // public static IMonad<TMarker, TValue> Unwrap<TMarker, TValue>(this IMonad<TMarker, TValue> monad) where TMarker : IMonadMarker<TMarker>
    // {
    //     var current = monad;
    //     while (current is MonadWrapper<TMarker, TValue> wrapper)
    //     {
    //         current = wrapper.Monad;
    //     }
    //
    //     return current;
    // }
}