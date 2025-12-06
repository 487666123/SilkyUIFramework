namespace SilkyUIFramework.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterGlobalUIAttribute(string name = "NONE", int priority = 0) : Attribute
{
    public string Name { get; } = name;
    public int Priority { get; } = priority;
}