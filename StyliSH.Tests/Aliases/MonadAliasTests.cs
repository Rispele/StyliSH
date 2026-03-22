using StyliSH.Abstractions.Monads.Aliases;
using StyliSH.Implementations.Monads.Either;

namespace StyliSH.Tests.Aliases;

// Test alias: DomainResult is an alias for Either<string, TValue>
public readonly record struct DomainResultMarker
    : IEitherLikeMarker<DomainResultMarker, string>
{
    public static IMonad<DomainResultMarker, TValue> Pure<TValue>(TValue value)
        => new MonadAlias<DomainResultMarker, EitherMarker<string>, TValue>(
            EitherMarker<string>.Pure(value));

    public static MonadWrapper<DomainResultMarker, TValue> FromError<TValue>(string error)
        => new MonadAlias<DomainResultMarker, EitherMarker<string>, TValue>(
            EitherMarker<string>.FromError<TValue>(error).Monad).Wrap();

    public static MonadWrapper<DomainResultMarker, TValue> FromValue<TValue>(TValue value)
        => new MonadAlias<DomainResultMarker, EitherMarker<string>, TValue>(
            EitherMarker<string>.FromValue(value).Monad).Wrap();
}

[TestFixture]
public class MonadAliasTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Either<string, int> Unwrap(MonadWrapper<DomainResultMarker, int> w)
    {
        MonadAlias<DomainResultMarker, EitherMarker<string>, int> alias = w;
        return new MonadWrapper<EitherMarker<string>, int>(alias.Inner);
    }

    private static bool DomainResultEq(
        MonadWrapper<DomainResultMarker, int> a,
        MonadWrapper<DomainResultMarker, int> b)
        => Unwrap(a) == Unwrap(b);

    private static string Inspect(MonadWrapper<DomainResultMarker, int> w)
        => Unwrap(w).Match(onError: e => $"err:{e}", onSuccess: v => $"ok:{v}");

    // ── Basic operations ───────────────────────────────────────────────────────

    [Test]
    public void Pure_WrapsValue()
    {
        var w = DomainResultMarker.Pure(42).Wrap();
        Inspect(w).Should().Be("ok:42");
    }

    [Test]
    public void FromError_CreatesError()
    {
        var w = DomainResultMarker.FromError<int>("fail");
        Inspect(w).Should().Be("err:fail");
    }

    [Test]
    public void FromValue_CreatesSuccess()
    {
        var w = DomainResultMarker.FromValue(42);
        Inspect(w).Should().Be("ok:42");
    }

    [Test]
    public void Map_TransformsValue()
    {
        var w = DomainResultMarker.FromValue(5).Map(x => x * 2);
        Inspect(w).Should().Be("ok:10");
    }

    [Test]
    public void Map_OnError_ShortCircuits()
    {
        var w = DomainResultMarker.FromError<int>("e").Map(x => x * 2);
        Inspect(w).Should().Be("err:e");
    }

    [Test]
    public void Bind_ChainsComputation()
    {
        var w = DomainResultMarker.FromValue(3)
            .Bind(x => DomainResultMarker.FromValue(x + 10));
        Inspect(w).Should().Be("ok:13");
    }

    [Test]
    public void Bind_OnError_ShortCircuits()
    {
        var w = DomainResultMarker.FromError<int>("e")
            .Bind(x => DomainResultMarker.FromValue(x + 10));
        Inspect(w).Should().Be("err:e");
    }

    // ── Type isolation ─────────────────────────────────────────────────────────

    [Test]
    public void MonadWrapper_CastsToAlias_NotToOriginal()
    {
        var w = DomainResultMarker.FromValue(42);

        // The monad inside is a MonadAlias, not a bare Either
        w.Monad.Should().BeOfType<MonadAlias<DomainResultMarker, EitherMarker<string>, int>>();
        w.Monad.Should().NotBeOfType<Either<string, int>>();

        // Cast to alias succeeds
        MonadAlias<DomainResultMarker, EitherMarker<string>, int> alias = w;
        alias.Inner.Should().NotBeNull();
    }

    // ── Monad laws ─────────────────────────────────────────────────────────────

    [Test]
    public void LeftIdentity()
    {
        MonadLawTests<DomainResultMarker, int>.VerifyLeftIdentity(
            value: 10,
            f: v => DomainResultMarker.FromValue(v + 1),
            equals: DomainResultEq);
    }

    [Test]
    public void RightIdentity()
    {
        MonadLawTests<DomainResultMarker, int>.VerifyRightIdentity(
            m: DomainResultMarker.FromValue(10),
            equals: DomainResultEq);
    }

    [Test]
    public void Associativity()
    {
        MonadLawTests<DomainResultMarker, int>.VerifyAssociativity(
            m: DomainResultMarker.FromValue(10),
            f: v => DomainResultMarker.FromValue(v + 1),
            g: v => DomainResultMarker.FromValue(v * 2),
            equals: DomainResultEq);
    }
}
