using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Tasks;

public readonly record struct TaskMarker : IMonadMarker<TaskMarker>
{
    public static IMonad<TaskMarker, TValue> Pure<TValue>(TValue value)
    {
        return new TaskMonad<TValue>(Task.FromResult(value));
    }
}

public readonly record struct TaskMonad<TValue>(Task<TValue> Value) :
    IMonad<TaskMarker, TValue>,
    IMonadUnwrapper<TaskMonad<TValue>, TaskMarker, TValue>
{
    public IMonad<TaskMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
    {
        var newTask = Value.ContinueWith(task => map(task.Result));
        return new TaskMonad<TNewValue>(newTask);
    }

    public IMonad<TaskMarker, TNewValue> RawBind<TNewValue>(Func<TValue, IMonad<TaskMarker, TNewValue>> bind)
    {
        return new TaskMonad<TNewValue>(BindInner(bind));
    }

    private async Task<TNewValue> BindInner<TNewValue>(Func<TValue, IMonad<TaskMarker, TNewValue>> bind)
    {
        return bind(await Value) is TaskMonad<TNewValue> taskMonad
            ? await taskMonad.Value
            : throw new InvalidOperationException();
    }

    public static implicit operator TaskMonad<TValue>(MonadWrapper<TaskMarker, TValue> monad)
    {
        return monad.Monad is TaskMonad<TValue> taskMonad
            ? taskMonad
            : throw new InvalidCastException();
    }
}