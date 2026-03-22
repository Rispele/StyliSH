using StyliSH.Abstractions.Monads.Aliases;
using StyliSH.Implementations.Monads.Either;

namespace StyliSH.Tests.Aliases;

[MonadAlias(typeof(EitherMarker<string>))]
public readonly partial record struct GenDomainResult<TValue>;

[TestFixture]
public class GeneratedMonadAliasTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Either<string, int> Unwrap(MonadWrapper<GenDomainResultMarker, int> w)
    {
        GenDomainResult<int> alias = w;
        return new MonadWrapper<EitherMarker<string>, int>(alias.Inner);
    }

    private static bool GenDomainResultEq(
        MonadWrapper<GenDomainResultMarker, int> a,
        MonadWrapper<GenDomainResultMarker, int> b)
        => Unwrap(a) == Unwrap(b);

    private static string Inspect(MonadWrapper<GenDomainResultMarker, int> w)
        => Unwrap(w).Match(onError: e => $"err:{e}", onSuccess: v => $"ok:{v}");

    // ── Basic operations ───────────────────────────────────────────────────────

    [Test]
    public void Pure_WrapsValue()
    {
        var w = GenDomainResultMarker.Pure(42).Wrap();
        Inspect(w).Should().Be("ok:42");
    }

    [Test]
    public void Map_TransformsValue()
    {
        var w = GenDomainResultMarker.Pure(5).Wrap().Map(x => x * 2);
        Inspect(w).Should().Be("ok:10");
    }

    [Test]
    public void Bind_ChainsComputation()
    {
        var w = GenDomainResultMarker.Pure(3).Wrap()
            .Bind(x => GenDomainResultMarker.Pure(x + 10));
        Inspect(w).Should().Be("ok:13");
    }

    // ── Type isolation ─────────────────────────────────────────────────────────

    [Test]
    public void MonadWrapper_ContainsGenDomainResult_NotBareEither()
    {
        var w = GenDomainResultMarker.Pure(42).Wrap();

        w.Monad.Should().BeOfType<GenDomainResult<int>>();
        w.Monad.Should().NotBeOfType<Either<string, int>>();

        GenDomainResult<int> alias = w;
        alias.Inner.Should().NotBeNull();
    }

    // ── Monad laws ─────────────────────────────────────────────────────────────

    [Test]
    public void LeftIdentity()
    {
        MonadLawTests<GenDomainResultMarker, int>.VerifyLeftIdentity(
            value: 10,
            f: v => GenDomainResultMarker.Pure(v + 1).Wrap(),
            equals: GenDomainResultEq);
    }

    [Test]
    public void RightIdentity()
    {
        MonadLawTests<GenDomainResultMarker, int>.VerifyRightIdentity(
            m: GenDomainResultMarker.Pure(10).Wrap(),
            equals: GenDomainResultEq);
    }

    [Test]
    public void Associativity()
    {
        MonadLawTests<GenDomainResultMarker, int>.VerifyAssociativity(
            m: GenDomainResultMarker.Pure(10).Wrap(),
            f: v => GenDomainResultMarker.Pure(v + 1).Wrap(),
            g: v => GenDomainResultMarker.Pure(v * 2).Wrap(),
            equals: GenDomainResultEq);
    }
}
