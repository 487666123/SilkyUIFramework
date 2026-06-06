using System.Collections;

namespace SilkyUIFramework;

/// <summary>
/// 构建框架所需的依赖注入容器
/// </summary>
internal static class ServiceProviderBuilder
{
    public static IServiceProvider BuildServiceProvider(ReadOnlySpan<ModLoadedTypes> modsWithLoadedTypes)
    {
        var services = new ServiceCollection();
        RegisterCoreServices(services);

        foreach (var modLoadedTypes in modsWithLoadedTypes)
        {
            foreach (var type in modLoadedTypes.LoadedTypes)
            {
                RegisterDiscoveredType(services, type);
            }
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 注册框架构建服务提供器所需的核心单例。
    /// </summary>
    static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton(_ => SilkyUIFramework.Instance);
        services.AddSingleton(_ => SilkyUIFramework.Instance.Logger);
        services.AddSingleton(_ => SilkyUISystem.Instance);
    }

    static void RegisterDiscoveredType(IServiceCollection services, Type type)
    {
        if (type.IsSubclassOf(typeof(UIView))) return;
        if (type.GetCustomAttribute<ServiceAttribute>() is not { } serviceAttr) return;

        RegisterImplementationAndInterfaces(services, serviceAttr.Lifetime, type);
    }

    static void RegisterImplementationAndInterfaces(
       IServiceCollection services,
       ServiceLifetime lifetime,
       Type implementation)
    {
        RegisterImplementation(services, lifetime, implementation);
        RegisterInterfaceMappings(services, lifetime, implementation);
    }

    /// <summary>
    /// 仅注册实现类型本身。
    /// </summary>
    static void RegisterImplementation(
       IServiceCollection services, ServiceLifetime lifetime, Type implementation)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
            {
                services.AddSingleton(implementation);
                break;
            }
            case ServiceLifetime.Transient:
            {
                services.AddTransient(implementation);
                break;
            }
            default:
                throw new Exception("不支持的生命周期。");
        }
    }

    static readonly HashSet<Type> ExcludedInterfaces =
   [
       typeof(IDisposable),
        typeof(IAsyncDisposable),
        typeof(IComparable),
        typeof(IComparable<>),
        typeof(IEquatable<>),
        typeof(IEnumerable),
        typeof(IEnumerator),
    ];

    /// <summary>
    /// 将实现类型的所有接口映射到同一实例解析链。
    /// </summary>
    static readonly HashSet<Type> _registeredInterfaces = []; // 去重表

    static void RegisterInterfaceMappings(
       IServiceCollection services, ServiceLifetime lifetime, Type implementation)
    {
        // 获取“直接实现”的接口，并过滤黑名单
        var directInterfaces = implementation
            .GetDirectlyImplementedInterfaces()  // 之前写好的扩展
            .Where(i => !ExcludedInterfaces.Contains(i.IsGenericType
                ? i.GetGenericTypeDefinition()
                : i));

        foreach (var iface in directInterfaces)
        {
            // 去重：一个接口只注册第一个遇到实现（或其他策略）
            if (_registeredInterfaces.Add(iface))
            {
                RegisterInterfaceMapping(services, lifetime, iface, implementation);
            }
        }
    }

    /// <summary>
    /// 获取类型直接实现的接口（不包括从基类继承的）
    /// </summary>
    static Type[] GetDirectlyImplementedInterfaces(this Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        var allInterfaces = type.GetInterfaces();
        var baseInterfaces = type.BaseType?.GetInterfaces() ?? Type.EmptyTypes;
        return [.. allInterfaces.Except(baseInterfaces)];
    }

    /// <summary>
    /// 注册单个接口到实现类型的映射。
    /// </summary>
    static void RegisterInterfaceMapping(
       IServiceCollection services, ServiceLifetime lifetime, Type interfaceType, Type implementation)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
            {
                services.AddSingleton(interfaceType, sp => sp.GetRequiredService(implementation));
                break;
            }
            case ServiceLifetime.Transient:
            {
                services.AddTransient(interfaceType, sp => sp.GetRequiredService(implementation));
                break;
            }
            default:
                throw new Exception("不支持的生命周期。");
        }
    }
}
