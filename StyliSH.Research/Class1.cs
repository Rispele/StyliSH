using StyliSH.Abstractions;
using StyliSH.Abstractions.Monads.Aliases;
using StyliSH.Implementations.Monads.Either;
using StyliSH.Implementations.Monads.Id;
using StyliSH.Implementations.Monads.Transformers.Either;

namespace StyliSH.Research;

[MonadAlias(typeof(EitherTMarker<IdMarker, string>))]
public readonly partial record struct GenDomainResult<TValue>;


public static class Class1
{
    public static void Main()
    {
        GenDomainResult<string> alias = GenDomainResultMarker.Pure(1)
            .Map(t => t * 10)
            .Map(t => "123")
            .Bind(t => GenDomainResultMarker.Pure("Треш"));
        EitherT<IdMarker, string, string> transformer = alias.Inner.Wrap();
        var monads = transformer.Run<IdMonad<Either<string, string>>>();
        var value = monads.Value.Match(
            s => "Error" + s,
            i => "Result" + i);
        Console.WriteLine(value);
    }
}