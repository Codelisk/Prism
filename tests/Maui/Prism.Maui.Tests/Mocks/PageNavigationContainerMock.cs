#nullable enable
using System.Collections.Generic;
using Prism.Behaviors;
using Prism.Common;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;

namespace Prism.Maui.Tests.Mocks;

public sealed class PageNavigationContainerMock : IContainerExtension, IDisposable
{
    private readonly List<ViewRegistration> _viewRegistrations = new();
    private readonly Dictionary<Type, Type> _typeMap = new();
    private INavigationRegistry? _navigationRegistry;

    /// <summary>
    /// When set, assigned to every <see cref="IPageNavigationEventRecordable"/> instance returned from <see cref="Resolve(Type)"/>
    /// so DI-created pages participate in the same <see cref="PageNavigationEventRecorder"/> as manually constructed test pages.
    /// </summary>
    public PageNavigationEventRecorder? SharedNavigationRecorder { get; set; }

    public object Instance => this;

    public IScopedProvider? CurrentScope { get; private set; }

    public IContainerRegistry Register(string key, Type type) =>
        throw new NotImplementedException();

    public IContainerRegistry Register(Type from, Type to)
    {
        _typeMap[from] = to;
        return this;
    }

    public IContainerRegistry Register(Type from, Type to, string name) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterInstance(Type type, object instance)
    {
        if (instance is ViewRegistration registration)
            _viewRegistrations.Add(registration);

        return this;
    }

    public IContainerRegistry RegisterSingleton(Type type) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterSingleton(Type from, Type to) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterType(Type type) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterType(Type type, string name) =>
        throw new NotImplementedException();

    public void Dispose()
    {
    }

    public bool IsRegistered(Type type) =>
        throw new NotImplementedException();

    public bool IsRegistered(Type type, string name) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterInstance(Type type, object instance, string name) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterSingleton(Type from, Type to, string name) =>
        throw new NotImplementedException();

    public object Resolve(Type type, params (Type Type, object Instance)[] parameters) =>
        Resolve(type);

    public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters) =>
        Resolve(type, name);

    public IScopedProvider CreateScope()
    {
        CurrentScope = new PageNavigationScope(this);
        return CurrentScope;
    }

    public IContainerRegistry RegisterSingleton(Type type, Func<object> factoryMethod) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterSingleton(Type type, Func<IContainerProvider, object> factoryMethod) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterManySingleton(Type type, params Type[] serviceTypes) =>
        throw new NotImplementedException();

    public IContainerRegistry Register(Type type, Func<object> factoryMethod) =>
        throw new NotImplementedException();

    public IContainerRegistry Register(Type type, Func<IContainerProvider, object> factoryMethod) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterMany(Type type, params Type[] serviceTypes) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterScoped(Type from, Type to) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterScoped(Type type, Func<object> factoryMethod) =>
        throw new NotImplementedException();

    public IContainerRegistry RegisterScoped(Type type, Func<IContainerProvider, object> factoryMethod) =>
        throw new NotImplementedException();

    public void FinalizeExtension()
    {
    }

    private object AfterResolve(object instance)
    {
        if (SharedNavigationRecorder is not null && instance is IPageNavigationEventRecordable recordable)
            recordable.PageNavigationEventRecorder = SharedNavigationRecorder;

        return instance;
    }

    public object Resolve(Type type)
    {
        if (type == typeof(INavigationRegistry))
        {
            _navigationRegistry ??= new NavigationRegistry(_viewRegistrations);
            return _navigationRegistry;
        }

        if (IsEnumerableOf(type, typeof(IPageBehaviorFactory)))
            return Array.Empty<IPageBehaviorFactory>();

        if (_typeMap.TryGetValue(type, out var implementation))
            return AfterResolve(Activator.CreateInstance(implementation)
                   ?? throw new InvalidOperationException($"Could not create instance of {implementation}."));

        return AfterResolve(Activator.CreateInstance(type)
               ?? throw new InvalidOperationException($"Could not create instance of {type}."));
    }

    public object Resolve(Type type, string name)
    {
        if (_typeMap.TryGetValue(type, out var implementation))
            return AfterResolve(Activator.CreateInstance(implementation)
                   ?? throw new InvalidOperationException($"Could not create instance of {implementation}."));

        return AfterResolve(Activator.CreateInstance(type)
               ?? throw new InvalidOperationException($"Could not create instance of {type}."));
    }

    internal static bool IsEnumerableOf(Type type, Type elementType)
    {
        if (!type.IsGenericType)
            return false;

        return type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
               type.GetGenericArguments()[0] == elementType;
    }

    private sealed class PageNavigationScope : IScopedProvider
    {
        private readonly PageNavigationContainerMock _root;
        private readonly MutablePageAccessor _accessor = new();
        private bool _disposed;

        public PageNavigationScope(PageNavigationContainerMock root) =>
            _root = root;

        public bool IsAttached { get; set; }

        public IScopedProvider? CurrentScope => this;

        public IScopedProvider CreateScope() =>
            new PageNavigationScope(_root);

        public IScopedProvider CreateChildScope() =>
            new PageNavigationScope(_root);

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _accessor.Page = null;
        }

        public object Resolve(Type type) =>
            Resolve(type, Array.Empty<(Type, object)>());

        public object Resolve(Type type, params (Type Type, object Instance)[] parameters)
        {
            if (type == typeof(IPageAccessor))
                return _accessor;

            if (IsEnumerableOf(type, typeof(IPageBehaviorFactory)))
                return Array.Empty<IPageBehaviorFactory>();

            return _root.Resolve(type);
        }

        public object Resolve(Type type, string name) =>
            _root.Resolve(type, name);

        public object Resolve(Type type, string name, params (Type Type, object Instance)[] parameters) =>
            _root.Resolve(type, name);
    }
}
