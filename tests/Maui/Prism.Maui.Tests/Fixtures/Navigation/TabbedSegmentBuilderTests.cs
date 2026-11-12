using Moq;
using Prism.Common;
using Prism.Maui.Tests.Mocks.Ioc;
using Prism.Navigation;
using Prism.Navigation.Builder;

namespace Prism.Maui.Tests.Fixtures.Navigation;

public class TabbedSegmentBuilderTests
{
    [Fact]
    public void AddTabbedSegment_WithUnknownSegmentName_Throws()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        var ex = Assert.Throws<NavigationException>(() =>
            builder.AddTabbedSegment("NoSuchTabbedPage", _ => { }));

        Assert.Equal(NavigationException.NoPageIsRegistered, ex.Message);
    }

    [Fact]
    public void CreateTab_WithNullConfigure_ThrowsArgumentNullException()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        Assert.Throws<ArgumentNullException>(() =>
            builder.AddTabbedSegment(t => t.CreateTab((Action<ICreateTabBuilder>)null!)));
    }
}
