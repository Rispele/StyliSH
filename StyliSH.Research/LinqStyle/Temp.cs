using System.Collections;

namespace StyliSH.Research.LinqStyle;

public record Either<TError, TValue>(bool IsSuccess, TError Error, TValue Value) : IEnumerable<TValue>
{
    public IEnumerator<TValue> GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    public class EitherEnumerator(TError error, TValue value) : IEnumerator<TValue>
    {
        private object? current;
        private TValue current1;

        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        TValue IEnumerator<TValue>.Current => current1;

        object? IEnumerator.Current => current;

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}

public static class Temp
{
    public static IEnumerable<char> Chars => ['a', 'b', 'c'];
    public static IEnumerable<int> Numbers => [1, 2, 3];

    public static IEnumerable<int> T1(int a) => [a]; 
    
    public static void Main()
    {
        var t1 =
            from n in Numbers
            from c in Chars
            select (n, c);
        
        var t2 =
            from n in Numbers
            from n1 in T1(n)
            from c in Chars
            select (n, c);

        foreach (var c in t1)
        {
            Console.WriteLine(c);
        }
    }
}