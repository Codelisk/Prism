using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Prism.Ioc;
using Prism.Ioc.Internals;

namespace Prism.DependencyInjection;

/// <summary>
/// Implements Prism's container on Microsoft.Extensions.DependencyInjection.
/// </summary>
/// <remarks>
/// <para>
/// Registrations made through <see cref="IContainerRegistry"/> and the services populated from the
/// <see cref="IServiceCollection"/> collect in one collection, in call order, until
/// <see cref="CreateServiceProvider"/> builds it. The last registration of a service wins. After the
/// build the container only resolves; a further registration throws.
/// </para>
/// <para>
/// The container creates only what is registered. It does not construct unregistered concrete types and
/// does not supply <see cref="Func{TResult}"/> or <see cref="Lazy{T}"/> without a registration. It compiles
/// no code at runtime, so it behaves the same under the JIT, the interpreter, trimming and Native AOT.
/// </para>
/// <para>
/// Arguments passed to <see cref="Resolve(Type, ValueTuple{Type, object}[])"/> reach the constructor of a
/// transient type registered by implementation type, if a public constructor takes them. Otherwise the
/// arguments are unused and the registration resolves as usual. Arguments do not reach the dependencies
/// of that type.
/// </para>
/// </remarks>
public sealed class MicrosoftContainerExtension : IContainerExtension<IServiceProvider>, IServiceCollectionAware
{
    private const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicConstructors;

    // Prism registriert diese Schnittstellen bei RegisterMany nie als Dienst.
    private static readonly Type[] IgnoredServiceTypes =
    [
        typeof(System.ComponentModel.INotifyPropertyChanged),
        typeof(System.ComponentModel.INotifyPropertyChanging),
        typeof(System.Windows.Input.ICommand),
        typeof(IDisposable),
        typeof(IAsyncDisposable),
    ];

    private readonly ServiceCollection _services = new();
    private readonly ServiceProviderOptions _options;
    private ServiceProvider? _provider;
    private Dictionary<(Type ServiceType, object? Key), ServiceDescriptor>? _registrations;
    private IScopedProvider? _currentScope;

    /// <summary>
    /// Creates a container with the default <see cref="ServiceProviderOptions"/>.
    /// </summary>
    public MicrosoftContainerExtension()
        : this(new ServiceProviderOptions())
    {
    }

    /// <summary>
    /// Creates a container that builds its service provider with <paramref name="options"/>.
    /// </summary>
    /// <param name="options">The options for the service provider, for example
    /// <see cref="ServiceProviderOptions.ValidateOnBuild"/> during development.</param>
    public MicrosoftContainerExtension(ServiceProviderOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;

        _services.AddSingleton<IContainerExtension>(this);
        _services.AddScoped<ContainerProviderLocator>();
        _services.AddTransient<IContainerProvider>(ProviderFor);
    }

    /// <summary>
    /// Gets the built service provider.
    /// </summary>
    /// <exception cref="InvalidOperationException">The service provider has not been built yet.</exception>
    public IServiceProvider Instance => _provider ?? throw NotBuilt();

    /// <inheritdoc />
    public IScopedProvider? CurrentScope => _currentScope;

    /// <inheritdoc />
    public void Populate(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ThrowIfBuilt();

        foreach (var descriptor in services)
            _services.Add(descriptor);
    }

    /// <inheritdoc />
    public IServiceProvider CreateServiceProvider()
    {
        if (_provider is not null)
            return _provider;

        _services.MakeReadOnly();
        var provider = _services.BuildServiceProvider(_options);

        var registrations = new Dictionary<(Type, object?), ServiceDescriptor>(_services.Count);
        foreach (var descriptor in _services)
            registrations[(descriptor.ServiceType, descriptor.ServiceKey)] = descriptor;

        _registrations = registrations;
        provider.GetRequiredService<ContainerProviderLocator>().Current = this;
        _provider = provider;
        return provider;
    }

    /// <inheritdoc />
    public IContainerRegistry RegisterInstance(Type type, object instance) =>
        Add(new ServiceDescriptor(type, instance));

    /// <inheritdoc />
    public IContainerRegistry RegisterInstance(Type type, object instance, string name) =>
        Add(new ServiceDescriptor(type, name, instance));

    /// <inheritdoc />
    public IContainerRegistry RegisterSingleton(Type from, Type to) =>
        AddType(from, null, to, ServiceLifetime.Singleton);

    /// <inheritdoc />
    public IContainerRegistry RegisterSingleton(Type from, Type to, string name) =>
        AddType(from, name, to, ServiceLifetime.Singleton);

    /// <inheritdoc />
    public IContainerRegistry RegisterSingleton(Type type, Func<object> factoryMethod) =>
        AddFactory(type, factoryMethod, ServiceLifetime.Singleton);

    /// <inheritdoc />
    public IContainerRegistry RegisterSingleton(Type type, Func<IContainerProvider, object> factoryMethod) =>
        AddFactory(type, factoryMethod, ServiceLifetime.Singleton);

    /// <inheritdoc />
    /// <remarks>All service types resolve to the same instance.</remarks>
    public IContainerRegistry RegisterManySingleton(Type type, params Type[] serviceTypes)
    {
        ThrowIfBuilt();

        // Ein eigener Schlüssel hält die eine Instanz, ohne den Implementierungstyp selbst als Dienst zu registrieren.
        var key = new object();
        AddType(type, key, type, ServiceLifetime.Singleton);
        foreach (var serviceType in ServiceTypes(type, serviceTypes))
            _services.Add(new ServiceDescriptor(serviceType, sp => sp.GetRequiredKeyedService(type, key), ServiceLifetime.Singleton));

        return this;
    }

    /// <inheritdoc />
    public IContainerRegistry RegisterScoped(Type from, Type to) =>
        AddType(from, null, to, ServiceLifetime.Scoped);

    /// <inheritdoc />
    public IContainerRegistry RegisterScoped(Type type, Func<object> factoryMethod) =>
        AddFactory(type, factoryMethod, ServiceLifetime.Scoped);

    /// <inheritdoc />
    public IContainerRegistry RegisterScoped(Type type, Func<IContainerProvider, object> factoryMethod) =>
        AddFactory(type, factoryMethod, ServiceLifetime.Scoped);

    /// <inheritdoc />
    public IContainerRegistry Register(Type from, Type to) =>
        AddType(from, null, to, ServiceLifetime.Transient);

    /// <inheritdoc />
    public IContainerRegistry Register(Type from, Type to, string name) =>
        AddType(from, name, to, ServiceLifetime.Transient);

    /// <inheritdoc />
    public IContainerRegistry Register(Type type, Func<object> factoryMethod) =>
        AddFactory(type, factoryMethod, ServiceLifetime.Transient);

    /// <inheritdoc />
    public IContainerRegistry Register(Type type, Func<IContainerProvider, object> factoryMethod) =>
        AddFactory(type, factoryMethod, ServiceLifetime.Transient);

    /// <inheritdoc />
    public IContainerRegistry RegisterMany(Type type, params Type[] serviceTypes)
    {
        ThrowIfBuilt();

        foreach (var serviceType in ServiceTypes(type, serviceTypes))
            AddType(serviceType, null, type, ServiceLifetime.Transient);

        return this;
    }

    /// <inheritdoc />
    public bool IsRegistered(Type type) => FindRegistration(type, null) is not null;

    /// <inheritdoc />
    public bool IsRegistered(Type type, string name) => FindRegistration(type, name) is not null;

    /// <inheritdoc />
    public object Resolve(Type type) => Resolve(type, Array.Empty<(Type, object)>());

    /// <inheritdoc />
    public object Resolve(Type type, params (Type Type, object Instance)[] parameters) =>
        ResolveIn(Instance, type, null, parameters, this, null);

    /// <inheritdoc />
    public object Resolve(Type type, string name) => Resolve(type, name, Array.Empty<(Type, object)>());

    /// <inheritdoc />
    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters) =>
        ResolveIn(Instance, type, name, parameters, this, null);

    /// <inheritdoc />
    public IScopedProvider CreateScope()
    {
        var scope = CreateOwnedScope();
        _currentScope = scope;
        return scope;
    }

    internal MicrosoftScopedProvider CreateOwnedScope() => new(this, Instance.CreateScope());

    internal object ResolveIn(
        IServiceProvider services,
        Type type,
        string? name,
        (Type Type, object Instance)[] parameters,
        IContainerProvider owner,
        Action<IDisposable>? trackCreated)
    {
        try
        {
            if (parameters.Length > 0 && TryCreateWithArguments(services, type, name, parameters, out var created))
            {
                if (created is IDisposable disposable)
                    trackCreated?.Invoke(disposable);
                return created;
            }

            return name is null ? services.GetRequiredService(type) : services.GetRequiredKeyedService(type, name);
        }
        catch (ContainerResolutionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ContainerResolutionException(type, name, ex, owner);
        }
    }

    private IContainerProvider ProviderFor(IServiceProvider services)
    {
        // Prism setzt den Anbieter in jedem eigenen Scope, der Container den der Wurzel. Ein fremder Scope (etwa der
        // eines MAUI-Fensters) bekommt einen Anbieter, der in ihm auflöst und ihn nie entsorgt.
        var locator = services.GetRequiredService<ContainerProviderLocator>();
        return locator.Current ??= new MicrosoftScopedProvider(this, services);
    }

    private bool TryCreateWithArguments(
        IServiceProvider services,
        Type type,
        string? name,
        (Type Type, object Instance)[] parameters,
        [NotNullWhen(true)] out object? created)
    {
        created = null;
        if (FindRegistration(type, name) is not { Lifetime: ServiceLifetime.Transient } registration)
            return false;

        var arguments = registration.IsKeyedService
            ? ArgumentsTakenBy(registration.KeyedImplementationType, parameters)
            : ArgumentsTakenBy(registration.ImplementationType, parameters);
        if (arguments is null)
            return false;

        created = registration.IsKeyedService
            ? ActivatorUtilities.CreateInstance(services, registration.KeyedImplementationType!, arguments)
            : ActivatorUtilities.CreateInstance(services, registration.ImplementationType!, arguments);
        return true;
    }

    private static object[]? ArgumentsTakenBy(
        [DynamicallyAccessedMembers(Constructors)] Type? implementationType,
        (Type Type, object Instance)[] parameters)
    {
        if (implementationType is null)
            return null;

        var constructors = implementationType.GetConstructors();
        List<object>? taken = null;
        foreach (var (_, instance) in parameters)
        {
            // Wie bisher reicht der Container sich selbst als Anbieter; ein übergebener Anbieter zählt nicht.
            if (instance is null or IContainerProvider)
                continue;

            var argumentType = instance.GetType();
            if (constructors.Any(c => c.GetParameters().Any(p => p.ParameterType.IsAssignableFrom(argumentType))))
                (taken ??= []).Add(instance);
        }

        return taken?.ToArray();
    }

    private ServiceDescriptor? FindRegistration(Type type, object? key)
    {
        if (_registrations is { } registrations)
            return registrations.GetValueOrDefault((type, key));

        for (var i = _services.Count - 1; i >= 0; i--)
        {
            var descriptor = _services[i];
            if (descriptor.ServiceType == type && Equals(descriptor.ServiceKey, key))
                return descriptor;
        }

        return null;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2067",
        Justification = "IContainerRegistry takes implementations as unannotated types. Prism's generic registrations " +
                        "(Register<T>, RegisterSingleton<TFrom, TTo>, RegisterForNavigation<TView, TViewModel>, " +
                        "RegisterManySingleton<T>, …) annotate their type arguments with DynamicallyAccessedMembers " +
                        "(PublicConstructors), so the constructors survive trimming at the call site.")]
    private IContainerRegistry AddType(Type serviceType, object? key, Type implementationType, ServiceLifetime lifetime) =>
        Add(new ServiceDescriptor(serviceType, key, implementationType, lifetime));

    private IContainerRegistry AddFactory(Type type, Func<object> factoryMethod, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);
        return Add(new ServiceDescriptor(type, _ => factoryMethod(), lifetime));
    }

    private IContainerRegistry AddFactory(Type type, Func<IContainerProvider, object> factoryMethod, ServiceLifetime lifetime)
    {
        ArgumentNullException.ThrowIfNull(factoryMethod);
        return Add(new ServiceDescriptor(type, sp => factoryMethod(ProviderFor(sp)), lifetime));
    }

    private IContainerRegistry Add(ServiceDescriptor descriptor)
    {
        ThrowIfBuilt();
        _services.Add(descriptor);
        return this;
    }

    // Wie Prisms RegisterManyExtensions.GetServiceTypes: ohne ausdrückliche Dienste alle Schnittstellen des Typs.
    [UnconditionalSuppressMessage("Trimming", "IL2070",
        Justification = "Only interfaces something resolves are services. The trimmer keeps an implemented interface " +
                        "whenever the interface type itself is kept, so every interface that can be resolved is listed.")]
    private static Type[] ServiceTypes(Type implementationType, Type[] serviceTypes)
    {
        var explicitTypes = serviceTypes.Where(t => !IgnoredServiceTypes.Contains(t)).ToArray();
        return explicitTypes.Length > 0
            ? explicitTypes
            : implementationType.GetInterfaces().Where(t => !IgnoredServiceTypes.Contains(t)).ToArray();
    }

    private void ThrowIfBuilt()
    {
        if (_provider is not null)
            throw new InvalidOperationException(
                "The container has already been built. Register every service before MauiAppBuilder.Build().");
    }

    private static InvalidOperationException NotBuilt() =>
        new("The container has not been built yet. Services resolve after MauiAppBuilder.Build().");
}
