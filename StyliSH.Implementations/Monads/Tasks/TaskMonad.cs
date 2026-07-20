using StyliSH.Abstractions.Monads;

namespace StyliSH.Implementations.Monads.Tasks;

public readonly record struct TaskMarker : IMonadMarker<TaskMarker>
{
    public static IMonad<TaskMarker, TValue> Pure<TValue>(TValue value)
    {
        return new TaskMonad<TValue>(Task.FromResult(value));
    }
}

public readonly record struct TaskMonad<TValue>(Task<TValue> ActualValue) :
    IMonad<TaskMarker, TValue>,
    IMonadUnwrapper<TaskMonad<TValue>, TaskMarker, TValue>
{
    public IValueWrapper<TValue> Value => new ValueWrapper<TValue>(ActualValue.Result, isInitialized: true);

    public IMonad<TaskMarker, TNewValue> RawMap<TNewValue>(Func<TValue, TNewValue> map)
    {
        var newTask = ActualValue.ContinueWith(task => map(task.Result));
        return new TaskMonad<TNewValue>(newTask);
    }

    public IMonad<TaskMarker, TNewValue> RawBind<TNewValue>(Func<TValue, IMonad<TaskMarker, TNewValue>> bind)
    {
        return new TaskMonad<TNewValue>(BindInner(bind));
    }

    private async Task<TNewValue> BindInner<TNewValue>(Func<TValue, IMonad<TaskMarker, TNewValue>> bind)
    {
        return bind(await ActualValue) is TaskMonad<TNewValue> taskMonad
            ? await taskMonad.ActualValue
            : throw new InvalidOperationException();
    }

    public static implicit operator TaskMonad<TValue>(MonadWrapper<TaskMarker, TValue> monad)
        => IMonadUnwrapper<TaskMonad<TValue>, TaskMarker, TValue>.CastFrom(monad);
}