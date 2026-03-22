namespace StyliSH.Tests;

public static class MonadLawTests<TMarker, TValue>
    where TMarker : IMonadMarker<TMarker>
{
    /// <summary>Left Identity: Pure(a).Bind(f) == f(a)</summary>
    public static void VerifyLeftIdentity(
        TValue value,
        Func<TValue, MonadWrapper<TMarker, TValue>> f,
        Func<MonadWrapper<TMarker, TValue>, MonadWrapper<TMarker, TValue>, bool> equals)
    {
        var lhs = TMarker.Pure(value).Wrap().Bind(f);
        var rhs = f(value);
        equals(lhs, rhs).Should().BeTrue("left identity law violated");
    }

    /// <summary>Right Identity: m.Bind(Pure) == m</summary>
    public static void VerifyRightIdentity(
        MonadWrapper<TMarker, TValue> m,
        Func<MonadWrapper<TMarker, TValue>, MonadWrapper<TMarker, TValue>, bool> equals)
    {
        var result = m.Bind(v => TMarker.Pure(v).Wrap());
        equals(result, m).Should().BeTrue("right identity law violated");
    }

    /// <summary>Associativity: m.Bind(f).Bind(g) == m.Bind(x => f(x).Bind(g))</summary>
    public static void VerifyAssociativity(
        MonadWrapper<TMarker, TValue> m,
        Func<TValue, MonadWrapper<TMarker, TValue>> f,
        Func<TValue, MonadWrapper<TMarker, TValue>> g,
        Func<MonadWrapper<TMarker, TValue>, MonadWrapper<TMarker, TValue>, bool> equals)
    {
        var lhs = m.Bind(f).Bind(g);
        var rhs = m.Bind(x => f(x).Bind(g));
        equals(lhs, rhs).Should().BeTrue("associativity law violated");
    }
}
