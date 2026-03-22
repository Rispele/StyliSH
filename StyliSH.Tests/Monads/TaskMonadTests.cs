using StyliSH.Implementations.Monads.Tasks;

namespace StyliSH.Tests.Monads;

[TestFixture]
public class TaskMonadTests
{
    private static async Task<bool> EqAsync(
        MonadWrapper<TaskMarker, int> a,
        MonadWrapper<TaskMarker, int> b)
    {
        var ta = ((TaskMonad<int>)a).Value;
        var tb = ((TaskMonad<int>)b).Value;
        return await ta == await tb;
    }

    private static bool Eq(MonadWrapper<TaskMarker, int> a, MonadWrapper<TaskMarker, int> b)
        => EqAsync(a, b).GetAwaiter().GetResult();

    [Test]
    public async Task Pure_CreatesCompletedTask()
    {
        TaskMonad<int> m = TaskMarker.Pure(42).Wrap();
        (await m.Value).Should().Be(42);
    }

    [Test]
    public async Task Map_TransformsAsyncResult()
    {
        TaskMonad<int> m = TaskMarker.Pure(5).Wrap().Map(x => x * 2);
        (await m.Value).Should().Be(10);
    }

    [Test]
    public async Task Bind_ChainsAsyncComputation()
    {
        TaskMonad<int> m = TaskMarker.Pure(3).Wrap()
            .Bind(x => TaskMarker.Pure(x + 1).Wrap());
        (await m.Value).Should().Be(4);
    }

    [Test]
    public async Task MonadWrapper_RoundTrip()
    {
        MonadWrapper<TaskMarker, int> wrapper = TaskMarker.Pure(7).Wrap();
        TaskMonad<int> concrete = wrapper;
        (await concrete.Value).Should().Be(7);
    }

    [Test]
    public void LeftIdentity()
    {
        MonadLawTests<TaskMarker, int>.VerifyLeftIdentity(
            value: 10,
            f: v => TaskMarker.Pure(v + 1).Wrap(),
            equals: Eq);
    }

    [Test]
    public void RightIdentity()
    {
        MonadLawTests<TaskMarker, int>.VerifyRightIdentity(
            m: TaskMarker.Pure(10).Wrap(),
            equals: Eq);
    }

    [Test]
    public void Associativity()
    {
        MonadLawTests<TaskMarker, int>.VerifyAssociativity(
            m: TaskMarker.Pure(10).Wrap(),
            f: v => TaskMarker.Pure(v + 1).Wrap(),
            g: v => TaskMarker.Pure(v * 2).Wrap(),
            equals: Eq);
    }
}
