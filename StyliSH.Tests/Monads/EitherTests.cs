using StyliSH.Implementations.Monads.Either;

namespace StyliSH.Tests.Monads;

[TestFixture]
public class EitherTests
{
    private static bool Eq(MonadWrapper<EitherMarker<string>, int> a, MonadWrapper<EitherMarker<string>, int> b)
        => a == b;

    [Test]
    public void FromValue_CreatesRight()
    {
        Either<string, int> m = Either<string, int>.FromValue(42);
        var result = m.Match(onError: _ => "error", onSuccess: v => $"ok:{v}");
        result.Should().Be("ok:42");
    }

    [Test]
    public void FromError_CreatesLeft()
    {
        Either<string, int> m = Either<string, int>.FromError("fail");
        var result = m.Match(onError: e => $"err:{e}", onSuccess: _ => "ok");
        result.Should().Be("err:fail");
    }

    [Test]
    public void Pure_CreatesRight()
    {
        Either<string, int> m = EitherMarker<string>.Pure(99).Wrap();
        var result = m.Match(onError: _ => "error", onSuccess: v => $"ok:{v}");
        result.Should().Be("ok:99");
    }

    [Test]
    public void Map_OnRight_TransformsValue()
    {
        Either<string, int> m = Either<string, int>.FromValue(5).Map(x => x * 2);
        m.Match(onError: _ => 0, onSuccess: v => v).Should().Be(10);
    }

    [Test]
    public void Map_OnLeft_ShortCircuits()
    {
        Either<string, int> m = Either<string, int>.FromError("oops").Map(x => x * 2);
        m.Match(onError: e => e, onSuccess: _ => "ok").Should().Be("oops");
    }

    [Test]
    public void Bind_OnRight_ChainsComputation()
    {
        Either<string, int> m = Either<string, int>.FromValue(3)
            .Bind(x => Either<string, int>.FromValue(x + 10));
        m.Match(onError: _ => 0, onSuccess: v => v).Should().Be(13);
    }

    [Test]
    public void Bind_OnLeft_ShortCircuits()
    {
        Either<string, int> m = Either<string, int>.FromError("bad")
            .Bind(x => Either<string, int>.FromValue(x + 10));
        m.Match(onError: e => e, onSuccess: _ => "ok").Should().Be("bad");
    }

    [Test]
    public void EitherMarker_Match_DispatchesCorrectly()
    {
        var right = EitherMarker<string>.Pure(5);
        var result = EitherMarker<string>.Match(right, onError: _ => -1, onSuccess: v => v);
        result.Should().Be(5);
    }

    [Test]
    public void MonadWrapper_RoundTrip()
    {
        var wrapper = Either<string, int>.FromValue(7);
        Either<string, int> concrete = wrapper;
        concrete.Match(onError: _ => 0, onSuccess: v => v).Should().Be(7);
    }

    [Test]
    public void LeftIdentity()
    {
        MonadLawTests<EitherMarker<string>, int>.VerifyLeftIdentity(
            value: 10,
            f: v => Either<string, int>.FromValue(v + 1),
            equals: Eq);
    }

    [Test]
    public void RightIdentity()
    {
        MonadLawTests<EitherMarker<string>, int>.VerifyRightIdentity(
            m: Either<string, int>.FromValue(10),
            equals: Eq);
    }

    [Test]
    public void Associativity()
    {
        MonadLawTests<EitherMarker<string>, int>.VerifyAssociativity(
            m: Either<string, int>.FromValue(10),
            f: v => Either<string, int>.FromValue(v + 1),
            g: v => Either<string, int>.FromValue(v * 2),
            equals: Eq);
    }
}
