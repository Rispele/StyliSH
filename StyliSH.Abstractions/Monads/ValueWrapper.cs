namespace StyliSH.Abstractions.Monads;

public interface IValueWrapper<out TValue>
{
    public TValue Value { get; }
}

public readonly struct ValueWrapper<TValue>(TValue value, bool isInitialized) : IValueWrapper<TValue>
{
    public TValue Value => isInitialized
        ? value
        : throw new InvalidOperationException("Value is not initialized.");
}

public readonly struct AsyncValueWrapper<TValue>(Task<TValue> value, bool isInitialized) : IValueWrapper<TValue>
{
    public TValue Value => isInitialized
        ? value.Result
        : throw new InvalidOperationException("Value is not initialized.");
}