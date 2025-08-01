﻿namespace SilkyUIFramework;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceAttribute(ServiceLifetime lifetime) : Attribute
{
    public ServiceLifetime Lifetime { get; } = lifetime;
}
