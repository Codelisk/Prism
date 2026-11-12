using Prism.DryIoc.Maui.Tests.Fixtures;
using Prism.DryIoc.Maui.Tests.Mocks;
using Prism.DryIoc.Maui.Tests.Mocks.Converters;
using Prism.DryIoc.Maui.Tests.Mocks.Events;
using Prism.DryIoc.Maui.Tests.Mocks.ViewModels;
using Prism.DryIoc.Maui.Tests.Mocks.Views;
using Prism.Events;
using Prism.Ioc;
using Prism.IoC;

namespace Prism.DependencyInjection.Maui.Tests.Fixtures.IoC;

/// <summary>
/// Gegenstück zu Prism.DryIoc.Maui.Tests: <see cref="ContainerProvider{T}"/> löst nur Registriertes auf.
/// </summary>
public class ContainerProviderTests(ITestOutputHelper testOutputHelper) : TestBase(testOutputHelper)
{
    [Fact]
    public void CanResolveUnamedType()
    {
        var app = CreateBuilder(prism => prism.RegisterTypes(c => c.Register<ConcreteTypeMock>())).Build();

        ConcreteTypeMock? type = new ContainerProvider<ConcreteTypeMock>();

        Assert.IsType<ConcreteTypeMock>(type);
    }

    [Fact]
    public void CanResolvedNamedType()
    {
        var app = CreateBuilder(prism => prism.RegisterTypes(c =>
            c.Register<ConcreteTypeMock>(ConcreteTypeMock.Key))).Build();

        ConcreteTypeMock? vm = new ContainerProvider<ConcreteTypeMock> { Name = ConcreteTypeMock.Key };

        Assert.IsType<ConcreteTypeMock>(vm);
    }

    [Fact]
    public void UnregisteredTypeIsNotCreated()
    {
        var app = CreateBuilder(prism => { }).Build();

        Assert.Throws<ContainerResolutionException>(() =>
        {
            ConcreteTypeMock? unused = new ContainerProvider<ConcreteTypeMock>();
        });
    }

    [Fact]
    public void ApplicationDisposesItsContainerAndServices()
    {
        var app = CreateBuilder(prism => prism.RegisterTypes(c => c.RegisterSingleton<PrismDisposalProbe>())).Build();
        var container = Assert.IsType<MicrosoftContainerExtension>(ContainerLocator.Current);
        var probe = container.Resolve<PrismDisposalProbe>();

        Assert.Same(container.Instance, app.Services);
        app.Dispose();

        Assert.Equal(1, probe.DisposeCount);
    }

    [Fact]
    public async Task ProvidesValueFromResourceDictionary()
    {
        var builder = CreateBuilder(prism =>
        {
            prism.RegisterTypes(containerRegistry =>
            {
                containerRegistry.RegisterForNavigation<MockXamlView, MockXamlViewViewModel>()
                    .Register<MockValueConverter>();
            })
            .CreateWindow(n =>
                n.CreateBuilder()
                .AddSegment<MockViewAViewModel>()
                .NavigateAsync());
        });
        var app = builder.Build();

        var ea = app.Services.GetRequiredService<IEventAggregator>();
        var events = new List<string>();
        ea.GetEvent<TestActionEvent>().Subscribe((m) => events.Add(m));

        var navigation = app.Services.GetRequiredService<INavigationService>();
        await navigation.CreateBuilder()
            .AddSegment<MockXamlViewViewModel>()
            .NavigateAsync();

        Assert.Contains(events, e => e == "Convert");
        var window = GetWindow(app);
        var xamlView = Assert.IsType<MockXamlView>(window.Page);

        xamlView.TestEntry.Text = "Foo Bar";
        Assert.Contains(events, e => e == "ConvertBack");
    }
}
