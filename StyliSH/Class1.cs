using StyliSH.Implementations.Monads.Either;
using StyliSH.Implementations.Monads.Tasks;
using StyliSH.Implementations.Monads.Transformers.Either;

namespace StyliSH;

public readonly record struct DomainError(string Error);

public static class Class1
{
    public static void Main()
    {
        var either = Do();
        var monads = either.Run<TaskMonad<Either<DomainError, int>>>();
        
        Console.WriteLine(monads.Value.GetAwaiter().GetResult());
    }

    public static EitherT<TaskMarker, DomainError, int> Do()
    {
        return EitherT<TaskMarker, DomainError, int>.FromValue(1)
            .Map(t => t + 1)
            .Map(t => t * 2);
        // .Bind(_ => EitherT<TaskMarker, DomainError, int>.FromError(new DomainError("123")));
    }
}