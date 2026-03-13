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


        var bodyType = typeof(BaseBody);

        foreach (var modLoadedTypes in modsWithLoadedTypes)
        {
            foreach (var type in modLoadedTypes.LoadedTypes)
            {
                // 继承 BaseBody 时，永不进入 Service 特性分支

                if (type.IsSubclassOf(bodyType))
                {
                    if (type.IsDefined(typeof(RegisterUIAttribute)))
                    {
                        RegisterImplementation(services, ServiceLifetime.Transient, type);
                        RegisterInterfaceMappings(services, ServiceLifetime.Transient, type);
                    }
                    else if (type.IsDefined(typeof(RegisterGlobalUIAttribute)))
                    {
                        RegisterImplementation(services, ServiceLifetime.Singleton, type);
                        RegisterInterfaceMappings(services, ServiceLifetime.Singleton, type);
                    }

                    continue; // Body 跳过 ServiceAttribute
                }

                if (type.GetCustomAttribute<ServiceAttribute>() is { } serviceAttr)
                {
                    RegisterImplementation(services, serviceAttr.Lifetime, type);
                    RegisterInterfaceMappings(services, serviceAttr.Lifetime, type);
                }
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

    /// <summary>
    /// 仅注册实现类型本身。
    /// </summary>
    private static void RegisterImplementation(
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

    /// <summary>
    /// 将实现类型的所有接口映射到同一实例解析链。
    /// </summary>
    private static void RegisterInterfaceMappings(
        IServiceCollection services, ServiceLifetime lifetime, Type implementation)
    {
        foreach (var iface in implementation.GetInterfaces())
        {
            RegisterInterfaceMapping(services, lifetime, iface, implementation);
        }
    }

    /// <summary>
    /// 注册单个接口到实现类型的映射。
    /// </summary>
    private static void RegisterInterfaceMapping(
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
