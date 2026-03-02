namespace SilkyUIFramework;

/// <summary>
/// 构建框架所需的依赖注入容器，并按约定注册带特性的服务与 UI Body。
/// </summary>
internal static class ServiceProviderBuilder
{
    /// <summary>
    /// 扫描传入类型集合并构建 <see cref="IServiceProvider"/>。
    /// 会注册：
    /// 1) 框架基础单例（Logger、SilkyUISystem）。
    /// 2) 带 <see cref="ServiceAttribute"/> 的服务类型。
    /// 3) 带 UI 注册特性的 <see cref="BaseBody"/>（普通 UI 为 Transient，全局 UI 为 Singleton）。
    /// </summary>
    /// <param name="allTypes">待扫描的类型集合分组。</param>
    /// <returns>构建完成的服务提供器。</returns>
    public static IServiceProvider BuildServiceProvider(IEnumerable<Type[]> allTypes)
    {
        var services = new ServiceCollection();
        RegisterCoreServices(services);

        foreach (var registration in ScanRegistrations(allTypes))
        {
            Register(services, registration);
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 注册框架构建服务提供器所需的核心单例。
    /// </summary>
    private static void RegisterCoreServices(IServiceCollection services)
    {
        services.AddSingleton(_ => SilkyUIFramework.Instance.Logger);
        services.AddSingleton(_ => SilkyUISystem.Instance);
    }

    /// <summary>
    /// 扫描并生成所有类型分组中的注册信息。
    /// </summary>
    private static IEnumerable<RegistrationInfo> ScanRegistrations(IEnumerable<Type[]> allTypes)
    {
        var bodyType = typeof(BaseBody);

        foreach (var types in allTypes)
        {
            foreach (var type in types)
            {
                // 继承 BaseBody 时，永不进入 Service 特性分支
                if (type.IsSubclassOf(bodyType))
                {
                    if (type.IsDefined(typeof(RegisterUIAttribute)))
                    {
                        yield return new RegistrationInfo(type, ServiceLifetime.Transient);
                    }
                    else if (type.IsDefined(typeof(RegisterGlobalUIAttribute)))
                    {
                        yield return new RegistrationInfo(type, ServiceLifetime.Singleton);
                    }

                    continue;
                }

                if (type.GetCustomAttribute<ServiceAttribute>() is { } serviceAttr)
                {
                    yield return new RegistrationInfo(type, serviceAttr.Lifetime);
                }
            }
        }
    }

    /// <summary>
    /// 按注册信息分发实现类型注册与接口映射注册。
    /// </summary>
    private static void Register(IServiceCollection services, RegistrationInfo registration)
    {
        RegisterImplementation(services, registration.Lifetime, registration.Implementation);
        RegisterInterfaceMappings(services, registration.Lifetime, registration.Implementation);
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
        }
    }

    private readonly record struct RegistrationInfo(Type Implementation, ServiceLifetime Lifetime);
}
