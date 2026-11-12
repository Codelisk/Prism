using Moq;
using Prism.Common;
using Prism.Maui.Tests.Mocks.Ioc;
using Prism.Navigation;
using Prism.Navigation.Builder;

namespace Prism.Maui.Tests.Fixtures.Navigation;

public class NavigationBuilderUriValidationTests
{
    private static INavigationBuilder CreateBuilderWithRegistry()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        return navigationService.Object.CreateBuilder();
    }

    [Fact]
    public void BuildUri_Throws_WhenUriContainsParentSegments_WithAbsoluteNavigation()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = CreateBuilderWithRegistry()
                .AddSegment("A")
                .RelativeBack()
                .AddSegment("B")
                .UseAbsoluteNavigation()
                .Uri;
        });
        Assert.Contains("relative back", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUri_Throws_WhenOnlyRelativeBackSegments()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = CreateBuilderWithRegistry()
                .RelativeBack()
                .RelativeBack()
                .Uri;
        });
        Assert.Contains("no other navigation segments", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildUri_Throws_WhenRelativeBack_AfterForwardSegment()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = CreateBuilderWithRegistry()
                .AddSegment("ViewA")
                .RelativeBack()
                .AddSegment("ViewB")
                .Uri;
        });
        Assert.Contains("relative back operator after", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
