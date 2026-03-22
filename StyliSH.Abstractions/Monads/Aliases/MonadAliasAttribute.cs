namespace StyliSH.Abstractions.Monads.Aliases;

[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class MonadAliasAttribute : Attribute
{
    public Type InnerMarkerType { get; }

    public MonadAliasAttribute(Type innerMarkerType)
    {
        InnerMarkerType = innerMarkerType;
    }
}
