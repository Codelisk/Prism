using Microsoft.Extensions.DependencyInjection;
using Prism.DryIoc.Maui.Tests.Mocks;
using Prism.Ioc;

namespace Prism.DependencyInjection.Maui.Tests.Fixtures.IoC;

public class MicrosoftContainerExtensionTests
{
    [Fact]
    public void LaterRegistrationWinsAcrossRegistryAndServiceCollection()
    {
        var container = new MicrosoftContainerExtension();
        container.RegisterSingleton<IService, First>();
        var services = new ServiceCollection();
        services.AddSingleton<IService, Second>();
        container.Populate(services);
        container.Register<IService, Third>();
        container.CreateServiceProvider();

        Assert.IsType<Third>(container.Resolve<IService>());
        Assert.Equal(
            new[] { typeof(First), typeof(Second), typeof(Third) },
            container.Instance.GetServices<IService>().Select(s => s.GetType()));
    }

    [Fact]
    public void IsRegisteredSeesRegistrationsBeforeAndAfterBuild()
    {
        var container = new MicrosoftContainerExtension();
        container.Register<IService, First>();
        container.Register<IOther, Multi>("named");

        Assert.True(container.IsRegistered(typeof(IService)));
        Assert.False(container.IsRegistered(typeof(IOther)));
        Assert.True(container.IsRegistered(typeof(IOther), "named"));
        Assert.False(container.IsRegistered(typeof(IService), "named"));

        container.CreateServiceProvider();

        Assert.True(container.IsRegistered(typeof(IService)));
        Assert.False(container.IsRegistered(typeof(IOther)));
        Assert.True(container.IsRegistered(typeof(IOther), "named"));
        Assert.False(container.IsRegistered(typeof(Second)));
    }

    [Fact]
    public void RegistrationAfterBuildThrows()
    {
        var container = new MicrosoftContainerExtension();
        container.CreateServiceProvider();

        Assert.Throws<InvalidOperationException>(() => container.Register<IService, First>());
        Assert.Throws<InvalidOperationException>(() => container.Populate(new ServiceCollection()));
    }

    [Fact]
    public void ResolvingBeforeBuildThrows()
    {
        var container = new MicrosoftContainerExtension();
        container.Register<IService, First>();

        Assert.Throws<InvalidOperationException>(() => container.Resolve<IService>());
    }

    [Fact]
    public void UnregisteredConcreteTypeIsNotCreated()
    {
        var container = Built(_ => { });

        var ex = Assert.Throws<ContainerResolutionException>(() => container.Resolve<First>());

        Assert.Equal(typeof(First), ex.ServiceType);
    }

    [Fact]
    public void FuncAndLazyNeedARegistration()
    {
        var container = Built(c => c.Register<IService, First>());

        Assert.Throws<ContainerResolutionException>(() => container.Resolve<Func<IService>>());
        Assert.Throws<ContainerResolutionException>(() => container.Resolve<Lazy<IService>>());
    }

    [Fact]
    public void ScopedServicesLiveInTheirScope()
    {
        var container = Built(c => c.RegisterScoped<IService, First>());

        using var first = container.CreateScope();
        using var second = container.CreateScope();

        Assert.Same(first.Resolve<IService>(), first.Resolve<IService>());
        Assert.NotSame(first.Resolve<IService>(), second.Resolve<IService>());
        Assert.Same(second, container.CurrentScope);
        Assert.Same(first, first.CurrentScope);
    }

    [Fact]
    public void ContainerProviderIsTheProviderThatResolves()
    {
        var container = Built(c => c.RegisterScoped<NeedsProvider, NeedsProvider>());

        using var scope = container.CreateScope();

        Assert.Same(container, container.Resolve<IContainerProvider>());
        Assert.Same(container, container.Resolve<IContainerExtension>());
        Assert.Same(scope, scope.Resolve<IContainerProvider>());
        Assert.Same(scope, scope.Resolve<NeedsProvider>().Provider);
    }

    [Fact]
    public void FactoryReceivesTheProviderOfTheResolvingScope()
    {
        var container = Built(c => c.RegisterScoped<NeedsProvider>(provider => new NeedsProvider(provider)));

        using var scope = container.CreateScope();

        Assert.Same(scope, scope.Resolve<NeedsProvider>().Provider);
        Assert.Same(container, container.Resolve<NeedsProvider>().Provider);
    }

    [Fact]
    public void ForeignScopeResolvesWithinItselfAndStaysWithItsOwner()
    {
        var container = Built(c => c.RegisterScoped<IService, First>());
        using var scope = container.Instance.CreateScope();

        var provider = scope.ServiceProvider.GetRequiredService<IContainerProvider>();

        Assert.NotSame(container, provider);
        Assert.Same(provider, scope.ServiceProvider.GetRequiredService<IContainerProvider>());
        Assert.Same(scope.ServiceProvider.GetRequiredService<IService>(), provider.Resolve<IService>());

        ((IDisposable)provider).Dispose();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IService>());
    }

    [Fact]
    public void ArgumentReachesTheConstructorThatTakesIt()
    {
        var container = Built(c => c.Register<WithArgument>());
        var argument = new Argument();

        var created = container.Resolve<WithArgument>((typeof(Argument), argument));

        Assert.Same(argument, created.Argument);
    }

    [Fact]
    public void ArgumentNoConstructorTakesIsIgnored()
    {
        var container = Built(c => c.Register<IService, First>().RegisterSingleton<IOther, Multi>());

        Assert.IsType<First>(container.Resolve<IService>((typeof(Argument), new Argument())));
        Assert.Same(
            container.Resolve<IOther>((typeof(Argument), new Argument())),
            container.Resolve<IOther>((typeof(Argument), new Argument())));
    }

    [Fact]
    public void ArgumentDoesNotReplaceASingleton()
    {
        var container = Built(c => c.RegisterSingleton<WithArgument>(() => new WithArgument(new Argument())));

        var first = container.Resolve<WithArgument>((typeof(Argument), new Argument()));

        Assert.Same(first, container.Resolve<WithArgument>((typeof(Argument), new Argument())));
    }

    [Fact]
    public void DisposingAScopeEndsItsChildrenAndArgumentInstances()
    {
        var container = Built(c => c.Register<WithArgument>().RegisterScoped<PrismDisposalProbe, PrismDisposalProbe>());
        var scope = container.CreateScope();
        var created = scope.Resolve<WithArgument>((typeof(Argument), new Argument()));
        var child = scope.CreateChildScope();
        var probe = child.Resolve<PrismDisposalProbe>();

        scope.Dispose();
        scope.Dispose();

        Assert.Equal(1, created.DisposeCount);
        Assert.Equal(1, probe.DisposeCount);
        Assert.Throws<ObjectDisposedException>(() => scope.Resolve<PrismDisposalProbe>());
    }

    [Fact]
    public void NamedRegistrationsResolveByName()
    {
        var instance = new Second();
        var container = Built(c => c.Register<IService, First>("first").RegisterInstance<IService>(instance, "second"));

        Assert.IsType<First>(container.Resolve<IService>("first"));
        Assert.Same(instance, container.Resolve<IService>("second"));
        Assert.Throws<ContainerResolutionException>(() => container.Resolve<IService>());
    }

    [Fact]
    public void RegisterManySingletonSharesOneInstance()
    {
        var container = Built(c => c.RegisterManySingleton<Multi>());

        Assert.Same(container.Resolve<IService>(), container.Resolve<IOther>());
        Assert.False(container.IsRegistered(typeof(Multi)));
        Assert.False(container.IsRegistered(typeof(IDisposable)));
    }

    [Fact]
    public void RegisterManyCreatesAnInstancePerResolve()
    {
        var container = Built(c => c.RegisterMany<Multi>(typeof(IService), typeof(IOther)));

        Assert.NotSame(container.Resolve<IService>(), container.Resolve<IService>());
        Assert.IsType<Multi>(container.Resolve<IOther>());
    }

    [Fact]
    public void ValidateOnBuildReportsAMissingDependency()
    {
        var container = new MicrosoftContainerExtension(new ServiceProviderOptions { ValidateOnBuild = true });
        container.Register<WithArgument>();

        var ex = Assert.Throws<AggregateException>(() => container.CreateServiceProvider());

        Assert.Contains(nameof(Argument), ex.Message);
    }

    private static MicrosoftContainerExtension Built(Action<IContainerRegistry> register)
    {
        var container = new MicrosoftContainerExtension();
        register(container);
        container.CreateServiceProvider();
        return container;
    }

    public interface IService;

    public interface IOther;

    public sealed class First : IService;

    public sealed class Second : IService;

    public sealed class Third : IService;

    public sealed class Multi : IService, IOther, IDisposable
    {
        public void Dispose()
        {
        }
    }

    public sealed class Argument;

    public sealed class WithArgument(Argument argument) : IDisposable
    {
        public Argument Argument { get; } = argument;

        public int DisposeCount { get; private set; }

        public void Dispose() => DisposeCount++;
    }

    public sealed class NeedsProvider(IContainerProvider provider)
    {
        public IContainerProvider Provider { get; } = provider;
    }
}
