using StyliSH.Implementations.Monads.Id;

namespace StyliSH.Tests.Monads;

[TestFixture]
public class IdMonadTests
{
    private static bool Eq(MonadWrapper<IdMarker, int> a, MonadWrapper<IdMarker, int> b) => a == b;

    [Test]
    public void Pure_WrapsValue()
    {
        IdMonad<int> m = IdMarker.Pure(42).Wrap();
        m.Value.Should().Be(42);
    }

    [Test]
    public void Map_TransformsValue()
    {
        IdMonad<int> m = IdMarker.Pure(5).Wrap().Map(x => x * 2);
        m.Value.Should().Be(10);
    }

    [Test]
    public void Bind_ChainsComputation()
    {
        IdMonad<int> m = IdMarker.Pure(3).Wrap().Bind(x => IdMarker.Pure(x + 1).Wrap());
        m.Value.Should().Be(4);
    }

    [Test]
    public void MonadWrapper_RoundTrip()
    {
        var wrapper = IdMarker.Pure(7).Wrap();
        IdMonad<int> concrete = wrapper;
        concrete.Value.Should().Be(7);
    }

    [Test]
    public void LeftIdentity()
    {
        MonadLawTests<IdMarker, int>.VerifyLeftIdentity(
            value: 10,
            f: v => IdMarker.Pure(v + 1).Wrap(),
            equals: Eq);
    }

    [Test]
    public void RightIdentity()
    {
        MonadLawTests<IdMarker, int>.VerifyRightIdentity(
            m: IdMarker.Pure(10).Wrap(),
            equals: Eq);
    }

    [Test]
    public void Associativity()
    {
        MonadLawTests<IdMarker, int>.VerifyAssociativity(
            m: IdMarker.Pure(10).Wrap(),
            f: v => IdMarker.Pure(v + 1).Wrap(),
            g: v => IdMarker.Pure(v * 2).Wrap(),
            equals: Eq);
    }
}
