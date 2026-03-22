using StyliSH.Abstractions.Monads.Aliases;
using StyliSH.Abstractions.Monads.Transformers;
using StyliSH.Implementations.Monads.Either;
using StyliSH.Implementations.Monads.Id;
using StyliSH.Implementations.Monads.Transformers.Either;

namespace StyliSH.Tests.Aliases;

// Test alias: DomainResultT is an alias for EitherT<IdMarker, string, TValue>
public readonly record struct DomainResultTMarker
    : IEitherLikeMarker<DomainResultTMarker, string>,
      ITransformerMarker<DomainResultTMarker, IdMarker>
{
    public static IMonad<DomainResultTMarker, TValue> Pure<TValue>(TValue value)
        => new TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, TValue>, TValue>(
            EitherTMarker<IdMarker, string>.Pure(value));

    public static MonadWrapper<DomainResultTMarker, TValue> FromError<TValue>(string error)
        => new TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, TValue>, TValue>(
            EitherTMarker<IdMarker, string>.FromError<TValue>(error).Monad).Wrap();

    public static MonadWrapper<DomainResultTMarker, TValue> FromValue<TValue>(TValue value)
        => new TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, TValue>, TValue>(
            EitherTMarker<IdMarker, string>.FromValue(value).Monad).Wrap();
}

[TestFixture]
public class TransformerAliasTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static IdMonad<Either<string, int>> Run(MonadWrapper<DomainResultTMarker, int> w)
    {
        TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, int>, int> alias = w;
        return alias.Run<IdMonad<Either<string, int>>>();
    }

    private static string Inspect(MonadWrapper<DomainResultTMarker, int> w)
        => Run(w).Value.Match(onError: e => $"err:{e}", onSuccess: v => $"ok:{v}");

    private static bool DomainResultTEq(
        MonadWrapper<DomainResultTMarker, int> a,
        MonadWrapper<DomainResultTMarker, int> b)
        => Run(a) == Run(b);

    // ── Basic operations ───────────────────────────────────────────────────────

    [Test]
    public void Pure_WrapsValue()
    {
        var w = DomainResultTMarker.Pure(42).Wrap();
        Inspect(w).Should().Be("ok:42");
    }

    [Test]
    public void FromError_CreatesError()
    {
        var w = DomainResultTMarker.FromError<int>("fail");
        Inspect(w).Should().Be("err:fail");
    }

    [Test]
    public void FromValue_CreatesSuccess()
    {
        var w = DomainResultTMarker.FromValue(42);
        Inspect(w).Should().Be("ok:42");
    }

    [Test]
    public void Map_TransformsValue()
    {
        var w = DomainResultTMarker.FromValue(5).Map(x => x * 2);
        Inspect(w).Should().Be("ok:10");
    }

    [Test]
    public void Map_OnError_ShortCircuits()
    {
        var w = DomainResultTMarker.FromError<int>("e").Map(x => x * 2);
        Inspect(w).Should().Be("err:e");
    }

    [Test]
    public void Bind_ChainsComputation()
    {
        var w = DomainResultTMarker.FromValue(3)
            .Bind(x => DomainResultTMarker.FromValue(x + 10));
        Inspect(w).Should().Be("ok:13");
    }

    [Test]
    public void Bind_OnError_ShortCircuits()
    {
        var w = DomainResultTMarker.FromError<int>("bad")
            .Bind(x => DomainResultTMarker.FromValue(x + 10));
        Inspect(w).Should().Be("err:bad");
    }

    // ── Run ────────────────────────────────────────────────────────────────────

    [Test]
    public void Run_UnwrapsToOuterMonad()
    {
        var w = DomainResultTMarker.FromValue(42);
        var result = Run(w);
        result.Should().BeOfType<IdMonad<Either<string, int>>>();
        result.Value.Match(onError: _ => false, onSuccess: v => v == 42).Should().BeTrue();
    }

    // ── Type isolation ─────────────────────────────────────────────────────────

    [Test]
    public void MonadWrapper_CastsToAlias_NotToOriginal()
    {
        var w = DomainResultTMarker.FromValue(42);

        // The monad inside is a TransformerAlias, not a bare EitherT
        w.Monad.Should().BeOfType<TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, int>, int>>();
        w.Monad.Should().NotBeOfType<EitherT<IdMarker, string, int>>();

        // Cast to alias succeeds
        TransformerAlias<DomainResultTMarker, EitherTMarker<IdMarker, string>, IdMarker, Either<string, int>, int> alias = w;
        alias.Inner.Should().NotBeNull();
    }

    // ── Monad laws ─────────────────────────────────────────────────────────────

    [Test]
    public void LeftIdentity()
    {
        MonadLawTests<DomainResultTMarker, int>.VerifyLeftIdentity(
            value: 10,
            f: v => DomainResultTMarker.FromValue(v + 1),
            equals: DomainResultTEq);
    }

    [Test]
    public void RightIdentity()
    {
        MonadLawTests<DomainResultTMarker, int>.VerifyRightIdentity(
            m: DomainResultTMarker.FromValue(10),
            equals: DomainResultTEq);
    }

    [Test]
    public void Associativity()
    {
        MonadLawTests<DomainResultTMarker, int>.VerifyAssociativity(
            m: DomainResultTMarker.FromValue(10),
            f: v => DomainResultTMarker.FromValue(v + 1),
            g: v => DomainResultTMarker.FromValue(v * 2),
            equals: DomainResultTEq);
    }
}
