namespace SilkyUIFramework;

internal static class ServiceProviderBuilder
{
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

    private static void RegisterAttributedServices(IServiceCollection services, Type[] types)
    {
        foreach (var (type, attr) in CollectServices(types))
        {
            Register(services, attr.Lifetime, type);
        }
    }

    private static IEnumerable<(Type, ServiceAttribute)> CollectServices(Type[] types)
        => types.Select(t => (t, t.GetCustomAttribute<ServiceAttribute>())).Where(p => p.Item2 != null);

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
