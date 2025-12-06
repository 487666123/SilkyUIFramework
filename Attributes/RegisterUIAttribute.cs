namespace SilkyUIFramework.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class RegisterUIAttribute : Attribute
{
    /// <summary> 图层节点 </summary>
    public string LayerNode { get; }

    /// <summary> 界面名 </summary>
    public string Name { get; }

    /// <summary> 优先级 </summary>
    public int Priority { get; }

    public InterfaceScaleType InterfaceScaleType { get; }

    public RegisterUIAttribute(string layerNode, string name, int priority = 0, InterfaceScaleType interfaceScaleType = InterfaceScaleType.UI)
    {
        LayerNode = layerNode;
        Name = name;
        Priority = priority;
        InterfaceScaleType = interfaceScaleType;
    }

    public RegisterUIAttribute(string name = "NONE", int priority = 0, InterfaceScaleType interfaceScaleType = InterfaceScaleType.UI)
    {
        LayerNode = "Vanilla: Radial Hotbars";
        Name = name;
        Priority = priority;
        InterfaceScaleType = interfaceScaleType;
    }
}