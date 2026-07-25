namespace StyliSH.Tests.Monads;

[TestFixture]
public class ValueWrapperTests
{
    [Test]
    public void ValueWrapper_WhenInitialized_ExposesValue()
    {
        var wrapper = new ValueWrapper<int>(42, isInitialized: true);

        wrapper.Value.Should().Be(42);
    }

    [Test]
    public void ValueWrapper_WhenNotInitialized_Throws()
    {
        var wrapper = new ValueWrapper<int>(42, isInitialized: false);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = wrapper.Value);

        exception!.Message.Should().Be("Value is not initialized.");
    }

    [Test]
    public async Task AsyncValueWrapper_WhenInitialized_WaitsForAndExposesValue()
    {
        var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var wrapper = new AsyncValueWrapper<int>(source.Task, isInitialized: true);

        var readStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var read = Task.Run(() =>
        {
            readStarted.SetResult();
            return wrapper.Value;
        });

        await readStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        read.IsCompleted.Should().BeFalse();

        source.SetResult(42);

        (await read.WaitAsync(TimeSpan.FromSeconds(5))).Should().Be(42);
    }

    [Test]
    public void AsyncValueWrapper_WhenNotInitialized_Throws()
    {
        var wrapper = new AsyncValueWrapper<int>(Task.FromResult(42), isInitialized: false);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = wrapper.Value);

        exception!.Message.Should().Be("Value is not initialized.");
    }
}
