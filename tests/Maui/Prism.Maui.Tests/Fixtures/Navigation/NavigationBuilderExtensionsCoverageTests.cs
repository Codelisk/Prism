using Moq;
using Prism.Common;
using Prism.Maui.Tests.Mocks.Ioc;
using Prism.Maui.Tests.Mocks.ViewModels;
using Prism.Maui.Tests.Mocks.Views;
using Prism.Navigation;
using Prism.Navigation.Builder;

namespace Prism.Maui.Tests.Fixtures.Navigation;

public class NavigationBuilderExtensionsCoverageTests
{
    [Fact]
    public void AddNavigationPage_UsesRegisteredNavigationPage()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        container.RegisterForNavigation<NavigationPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        var uri = builder.AddNavigationPage().AddSegment("ViewA").Uri;

        Assert.StartsWith("NavigationPage", uri.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void AddNavigationPage_WithModal_UsesSegmentParameter()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        container.RegisterForNavigation<NavigationPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        var uri = builder.AddNavigationPage(useModalNavigation: true).Uri;

        Assert.Contains("UseModalNavigation", uri.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddSegment_WithBoolModal_AppendsParameter()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        var uri = builder.AddSegment("ViewA", useModalNavigation: false).Uri;

        Assert.Contains("ViewA", uri.ToString());
        Assert.Contains("UseModalNavigation", uri.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TabbedSegment_CreateTab_UsesViewModelKey_OnCreateTabBuilder()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        container.RegisterForNavigation<PageMock, PageMockViewModel>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var uri = navigationService.Object
            .CreateBuilder()
            .AddTabbedSegment(b => b.CreateTab(t => t.AddSegment<PageMockViewModel>()))
            .Uri;

        Assert.Contains("createTab=", uri.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TabbedSegment_CreateTab_AddNavigationPageNested()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        container.RegisterForNavigation<NavigationPage>();
        container.RegisterForNavigation<PageMock, PageMockViewModel>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var uri = navigationService.Object
            .CreateBuilder()
            .AddTabbedSegment(b => b.CreateTab(t => t.AddNavigationPage().AddSegment<PageMockViewModel>()))
            .Uri;

        Assert.Contains("NavigationPage", uri.ToString());
    }

    [Fact]
    public void TabbedSegment_SelectTab_StringOverload_JoinsSegments()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var tabbedBuilder = new Mock<ITabbedNavigationBuilder>();
        tabbedBuilder.Setup(b => b.SelectTab(It.IsAny<string>())).Returns(tabbedBuilder.Object);
        NavigationBuilderExtensions.SelectTab(tabbedBuilder.Object, "Nav", "Child");
        tabbedBuilder.Verify(b => b.SelectTab("Nav|Child"), Times.Once);
    }

    [Fact]
    public async Task GoBackToAsync_Generic_OnBuilder_ResolvesKey()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        container.RegisterForNavigation<PageMock, PageMockViewModel>();
        var navMock = new Mock<INavigationService>();
        navMock
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navMock.Object.CreateBuilder();

        navMock.Setup(n => n.GoBackToAsync(It.IsAny<string>(), It.IsAny<INavigationParameters>()))
            .ReturnsAsync(new NavigationResult());

        await builder.GoBackToAsync<PageMockViewModel>();

        navMock.Verify(n => n.GoBackToAsync(
            It.Is<string>(s => s.Length > 0),
            It.IsAny<INavigationParameters>()), Times.Once);
    }
}
