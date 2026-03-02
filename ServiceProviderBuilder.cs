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
        services.AddSingleton(_ => SilkyUIFramework.Instance.Logger);
        services.AddSingleton(_ => SilkyUISystem.Instance);

        foreach (var types in allTypes)
        {
            RegisterAttributedServices(services, types);

            foreach (var type in types.Where(type => type.IsSubclassOf(typeof(BaseBody))))
            {
                if (type.IsDefined(typeof(RegisterUIAttribute)))
                {
                    Register(services, ServiceLifetime.Transient, type);
                }

                if (type.IsDefined(typeof(RegisterGlobalUIAttribute)))
                {
                    Register(services, ServiceLifetime.Singleton, type);
                }
            }
        }

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// 注册所有带 <see cref="ServiceAttribute"/> 的类型。
    /// </summary>
    private static void RegisterAttributedServices(IServiceCollection services, Type[] types)
    {
        foreach (var (type, attr) in CollectServices(types))
        {
            Register(services, attr.Lifetime, type);
        }
    }

    /// <summary>
    /// 从类型数组中筛出带 <see cref="ServiceAttribute"/> 的类型及其特性实例。
    /// </summary>
    private static IEnumerable<(Type, ServiceAttribute)> CollectServices(Type[] types)
        => types.Select(t => (t, t.GetCustomAttribute<ServiceAttribute>())).Where(p => p.Item2 != null);

    /// <summary>
    /// 按生命周期注册实现类型，并将其已实现接口映射到同一实例解析链。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="lifetime">注册生命周期。</param>
    /// <param name="impl">实现类型。</param>
    private static void Register(IServiceCollection services, ServiceLifetime lifetime, Type impl)
    {
        var interfaces = impl.GetInterfaces();
        switch (lifetime)
        {
            case ServiceLifetime.Singleton:
            {
                services.AddSingleton(impl);
                foreach (var iface in interfaces)
                    services.AddSingleton(iface, sp => sp.GetRequiredService(impl));
                break;
            }
            case ServiceLifetime.Transient:
            {
                services.AddTransient(impl);
                foreach (var iface in interfaces)
                    services.AddTransient(iface, sp => sp.GetRequiredService(impl));
                break;
            }
        }
    }
}
