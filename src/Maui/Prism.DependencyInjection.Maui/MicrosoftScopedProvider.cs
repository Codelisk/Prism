using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using Prism.Ioc.Internals;

namespace Prism.DependencyInjection;

/// <summary>
/// Resolves Prism services within one <see cref="IServiceScope"/>.
/// </summary>
/// <remarks>
/// A scope created by Prism belongs to this provider and ends with <see cref="Dispose"/>, together with its child
/// scopes and the disposable instances created with arguments. A scope created elsewhere, for example by a MAUI
/// window, is only used for resolving; its owner ends it.
/// </remarks>
internal sealed class MicrosoftScopedProvider : IScopedProvider
{
    private readonly MicrosoftContainerExtension _container;
    private readonly IServiceScope? _scope;
    private readonly List<IScopedProvider> _children = [];
    private readonly List<IDisposable> _created = [];
    private readonly Lock _gate = new();
    private IServiceProvider? _services;

    internal MicrosoftScopedProvider(MicrosoftContainerExtension container, IServiceScope scope)
        : this(container, scope.ServiceProvider)
    {
        _scope = scope;
    }

    internal MicrosoftScopedProvider(MicrosoftContainerExtension container, IServiceProvider services)
    {
        _container = container;
        _services = services;
        services.GetRequiredService<ContainerProviderLocator>().Current = this;
    }

    /// <inheritdoc />
    public bool IsAttached { get; set; }

    /// <inheritdoc />
    public IScopedProvider? CurrentScope => this;

    /// <inheritdoc />
    public IScopedProvider CreateChildScope()
    {
        var child = _container.CreateOwnedScope();
        lock (_gate)
            _children.Add(child);
        return child;
    }

    /// <inheritdoc />
    public IScopedProvider CreateScope() => ContainerLocator.Container.CreateScope();

    /// <inheritdoc />
    public object Resolve(Type type) => Resolve(type, Array.Empty<(Type, object)>());

    /// <inheritdoc />
    public object Resolve(Type type, params (Type Type, object Instance)[] parameters) =>
        _container.ResolveIn(Services, type, null, parameters, this, Track);

    /// <inheritdoc />
    public object Resolve(Type type, string name) => Resolve(type, name, Array.Empty<(Type, object)>());

    /// <inheritdoc />
    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters) =>
        _container.ResolveIn(Services, type, name, parameters, this, Track);

    /// <inheritdoc />
    public void Dispose()
    {
        IScopedProvider[] children;
        IDisposable[] created;
        lock (_gate)
        {
            if (_services is null)
                return;

            _services = null;
            children = [.. _children];
            created = [.. _created];
            _children.Clear();
            _created.Clear();
        }

        foreach (var child in children)
            child.Dispose();

        for (var i = created.Length - 1; i >= 0; i--)
            created[i].Dispose();

        _scope?.Dispose();
    }

    private IServiceProvider Services => _services ?? throw new ObjectDisposedException(nameof(MicrosoftScopedProvider));

    private void Track(IDisposable instance)
    {
        lock (_gate)
            _created.Add(instance);
    }
}
