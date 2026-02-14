namespace StyliSH.Research.Interfaces;

public interface IBase;

public class A : IBase;

public class B : IBase;

public static class Temp
{
    public static void Main()
    {
        var t = (ITemp<IBase>)new TempAImpl();
    }
}

public interface ITemp<TEvent> where TEvent : IBase
{
    protected void Convert(TEvent @event);
}

public class TempAImpl : ITemp<A>
{
    public void Convert(A @event)
    {
    }
}

public class TempBImpl : ITemp<B>
{
    public void Convert(B @event)
    {
    }
}