using StyliSH.Implementations.Monads.Either;
using StyliSH.Implementations.Monads.Id;
using StyliSH.Implementations.Monads.Tasks;
using StyliSH.Implementations.Monads.Transformers.Either;

namespace StyliSH.Tests.Transformers;

[TestFixture]
public class EitherTTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static bool IdEq(
        MonadWrapper<EitherTMarker<IdMarker, string>, int> a,
        MonadWrapper<EitherTMarker<IdMarker, string>, int> b)
    {
        EitherT<IdMarker, string, int> ta = a;
        EitherT<IdMarker, string, int> tb = b;
        var ra = ta.Run<IdMonad<Either<string, int>>>();
        var rb = tb.Run<IdMonad<Either<string, int>>>();
        return ra == rb;
    }

    private static string RunId(MonadWrapper<EitherTMarker<IdMarker, string>, int> w)
    {
        EitherT<IdMarker, string, int> t = w;
        var r = t.Run<IdMonad<Either<string, int>>>();
        return r.ActualValue.Match(onError: e => $"err:{e}", onSuccess: v => $"ok:{v}");
    }

    private static async Task<string> RunTask(MonadWrapper<EitherTMarker<TaskMarker, string>, int> w)
    {
        EitherT<TaskMarker, string, int> t = w;
        var r = t.Run<TaskMonad<Either<string, int>>>();
        var either = await r.ActualValue;
        return either.Match(onError: e => $"err:{e}", onSuccess: v => $"ok:{v}");
    }

    // ── IdMarker outer ─────────────────────────────────────────────────────────

    [Test]
    public void FromValue_WithId_CreatesRightInId()
    {
        var w = EitherT<IdMarker, string, int>.FromValue(42);
        RunId(w).Should().Be("ok:42");
    }

    [Test]
    public void FromError_WithId_CreatesLeftInId()
    {
        var w = EitherT<IdMarker, string, int>.FromError("fail");
        RunId(w).Should().Be("err:fail");
    }

    [Test]
    public void Map_OnSuccess_TransformsValue()
    {
        var w = EitherT<IdMarker, string, int>.FromValue(5).Map(x => x * 2);
        RunId(w).Should().Be("ok:10");
    }

    [Test]
    public void Map_OnError_ShortCircuits()
    {
        var w = EitherT<IdMarker, string, int>.FromError("oops").Map(x => x * 2);
        RunId(w).Should().Be("err:oops");
    }

    [Test]
    public void Bind_OnSuccess_ChainsComputation()
    {
        var w = EitherT<IdMarker, string, int>.FromValue(3)
            .Bind(x => EitherT<IdMarker, string, int>.FromValue(x + 10));
        RunId(w).Should().Be("ok:13");
    }

    [Test]
    public void Bind_OnError_ShortCircuits()
    {
        var w = EitherT<IdMarker, string, int>.FromError("bad")
            .Bind(x => EitherT<IdMarker, string, int>.FromValue(x + 10));
        RunId(w).Should().Be("err:bad");
    }

    [Test]
    public void Value_WithId_OnSuccess_ExposesValue()
    {
        EitherT<IdMarker, string, int> transformer = EitherT<IdMarker, string, int>.FromValue(42);

        transformer.Value.Value.Should().Be(42);
    }

    [Test]
    public void Value_WithId_OnError_Throws()
    {
        EitherT<IdMarker, string, int> transformer = EitherT<IdMarker, string, int>.FromError("fail");

        var exception = Assert.Throws<InvalidOperationException>(() => _ = transformer.Value.Value);

        exception!.Message.Should().Be("Value is not initialized.");
    }

    [Test]
    public void LeftIdentity_WithId()
    {
        MonadLawTests<EitherTMarker<IdMarker, string>, int>.VerifyLeftIdentity(
            value: 10,
            f: v => EitherT<IdMarker, string, int>.FromValue(v + 1),
            equals: IdEq);
    }

    [Test]
    public void RightIdentity_WithId()
    {
        MonadLawTests<EitherTMarker<IdMarker, string>, int>.VerifyRightIdentity(
            m: EitherT<IdMarker, string, int>.FromValue(10),
            equals: IdEq);
    }

    [Test]
    public void Associativity_WithId()
    {
        MonadLawTests<EitherTMarker<IdMarker, string>, int>.VerifyAssociativity(
            m: EitherT<IdMarker, string, int>.FromValue(10),
            f: v => EitherT<IdMarker, string, int>.FromValue(v + 1),
            g: v => EitherT<IdMarker, string, int>.FromValue(v * 2),
            equals: IdEq);
    }

    // ── TaskMarker outer ───────────────────────────────────────────────────────

    [Test]
    public async Task FromValue_WithTask_CreatesRightInTask()
    {
        var w = EitherT<TaskMarker, string, int>.FromValue(42);
        (await RunTask(w)).Should().Be("ok:42");
    }

    [Test]
    public async Task FromError_WithTask_CreatesLeftInTask()
    {
        var w = EitherT<TaskMarker, string, int>.FromError("fail");
        (await RunTask(w)).Should().Be("err:fail");
    }

    [Test]
    public async Task Map_WithTask_TransformsAsyncResult()
    {
        var w = EitherT<TaskMarker, string, int>.FromValue(5).Map(x => x * 2);
        (await RunTask(w)).Should().Be("ok:10");
    }

    [Test]
    public async Task Bind_WithTask_ChainsAsyncComputation()
    {
        var w = EitherT<TaskMarker, string, int>.FromValue(3)
            .Bind(x => EitherT<TaskMarker, string, int>.FromValue(x + 10));
        (await RunTask(w)).Should().Be("ok:13");
    }

    [Test]
    public async Task Map_WithOutValue_WithTask_DoesNotWaitForOuterTask()
    {
        var source = new TaskCompletionSource<IMonad<EitherMarker<string>, int>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        IMonad<EitherTMarker<TaskMarker, string>, int> transformer = new EitherT<TaskMarker, string, int>(
            new TaskMonad<IMonad<EitherMarker<string>, int>>(source.Task));
        var mapStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var mapCall = Task.Run(() =>
        {
            mapStarted.SetResult();
            var mapped = transformer.Map(x => x * 2, out var value);
            return (Mapped: mapped, Value: value);
        });

        bool completedBeforeSource;
        try
        {
            await mapStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            completedBeforeSource =
                await Task.WhenAny(mapCall, Task.Delay(TimeSpan.FromSeconds(2))) == mapCall;
        }
        finally
        {
            source.TrySetResult(Either<string, int>.FromValue(21).Monad);
        }

        var result = await mapCall.WaitAsync(TimeSpan.FromSeconds(5));

        completedBeforeSource.Should().BeTrue();
        (await RunTask(result.Mapped)).Should().Be("ok:42");
        result.Value.Value.Should().Be(42);
    }
}