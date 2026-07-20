using StyliSH.Abstractions.Monads;

namespace StyliSH.Abstractions;

public static class MonadExtensions
{
    extension<TMarker, TValue>(IMonad<TMarker, TValue> monad) where TMarker : IMonadMarker<TMarker>
    {
        public MonadWrapper<TMarker, TValue> Wrap()
        {
            return new MonadWrapper<TMarker, TValue>(monad);
        }

        public MonadWrapper<TMarker, TNewValue> Map<TNewValue>(Func<TValue, TNewValue> map,
            out IValueWrapper<TNewValue> newValue)
        {
            var mapped = monad.Map(map);
            newValue = mapped.Monad.Value;

            return mapped;
        }

        public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(Func<TValue, IMonad<TMarker, TNewValue>> bind,
            out IValueWrapper<TNewValue> newValue)
        {
            var mapped = monad.Bind(bind);
            newValue = mapped.Monad.Value;

            return mapped;
        }

        public MonadWrapper<TMarker, TNewValue> Bind<TNewValue>(Func<TValue, MonadWrapper<TMarker, TNewValue>> bind,
            out IValueWrapper<TNewValue> newValue)
        {
            var mapped = monad.Bind(bind);
            newValue = mapped.Monad.Value;

            return mapped;
        }
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