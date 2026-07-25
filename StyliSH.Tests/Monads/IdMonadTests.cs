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
        m.ActualValue.Should().Be(42);
    }

    [Test]
    public void Map_TransformsValue()
    {
        IdMonad<int> m = IdMarker.Pure(5).Wrap().Map(x => x * 2);
        m.ActualValue.Should().Be(10);
    }

    [Test]
    public void Bind_ChainsComputation()
    {
        IdMonad<int> m = IdMarker.Pure(3).Wrap().Bind(x => IdMarker.Pure(x + 1).Wrap());
        m.ActualValue.Should().Be(4);
    }

    [Test]
    public void Value_ExposesContainedValue()
    {
        var monad = IdMarker.Pure(42);

        monad.Value.Value.Should().Be(42);
    }

    [Test]
    public void OutValueExtensions_OnIMonad_CaptureEveryOperationResult()
    {
        IMonad<IdMarker, int> monad = IdMarker.Pure(3);

        var mapped = monad.Map(x => x * 2, out var mappedValue);
        var boundToMonad = monad.Bind(x => IdMarker.Pure(x + 1), out var monadValue);
        var boundToWrapper = monad.Bind(x => IdMarker.Pure(x + 2).Wrap(), out var wrapperValue);

        ((IdMonad<int>)mapped).ActualValue.Should().Be(6);
        mappedValue.Value.Should().Be(6);
        ((IdMonad<int>)boundToMonad).ActualValue.Should().Be(4);
        monadValue.Value.Should().Be(4);
        ((IdMonad<int>)boundToWrapper).ActualValue.Should().Be(5);
        wrapperValue.Value.Should().Be(5);
    }

    [Test]
    public void OutValueMethods_OnMonadWrapper_CaptureEveryOperationResult()
    {
        var result = IdMarker.Pure(2).Wrap()
            .Map(x => x * 3, out var mappedValue)
            .Bind(x => IdMarker.Pure(x + mappedValue.Value), out var monadValue)
            .Bind(x => IdMarker.Pure(x + 1).Wrap(), out var wrapperValue);

        ((IdMonad<int>)result).ActualValue.Should().Be(13);
        mappedValue.Value.Should().Be(6);
        monadValue.Value.Should().Be(12);
        wrapperValue.Value.Should().Be(13);
    }

    [Test]
    public void MonadWrapper_RoundTrip()
    {
        var wrapper = IdMarker.Pure(7).Wrap();
        IdMonad<int> concrete = wrapper;
        concrete.ActualValue.Should().Be(7);
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
