using Moq;
using Prism.Common;
using Prism.Maui.Tests.Mocks.Ioc;
using Prism.Navigation;
using Prism.Navigation.Builder;

namespace Prism.Maui.Tests.Fixtures.Navigation;

public class NavigationBuilderNavigateAsyncTests
{
    private static (INavigationBuilder Builder, Mock<INavigationService> NavMock) CreateBuilderPair()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navMock = new Mock<INavigationService>();
        navMock
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        return (navMock.Object.CreateBuilder(), navMock);
    }

    [Fact]
    public async Task NavigateAsync_Delegates_ToNavigationService()
    {
        var (builder, navMock) = CreateBuilderPair();
        var expected = new NavigationResult();
        navMock
            .Setup(n => n.NavigateAsync(It.IsAny<Uri>(), It.IsAny<INavigationParameters>()))
            .ReturnsAsync(expected);

        var result = await builder.AddSegment("ViewA").NavigateAsync();

        Assert.Same(expected, result);
        navMock.Verify(n => n.NavigateAsync(
            It.Is<Uri>(u => u.ToString() == "ViewA"),
            It.IsAny<INavigationParameters>()), Times.Once);
    }

    [Fact]
    public async Task NavigateAsync_WithOnError_InvokesOnError_WhenException()
    {
        var (builder, navMock) = CreateBuilderPair();
        var boom = new NavigationException("test");
        navMock
            .Setup(n => n.NavigateAsync(It.IsAny<Uri>(), It.IsAny<INavigationParameters>()))
            .ReturnsAsync(new NavigationResult(boom));

        Exception caught = null;
        await builder.NavigateAsync(ex => caught = ex);

        Assert.Same(boom, caught);
    }

    [Fact]
    public async Task NavigateAsync_WithCallbacks_InvokesOnSuccess_WhenSuccessful()
    {
        var (builder, navMock) = CreateBuilderPair();
        navMock
            .Setup(n => n.NavigateAsync(It.IsAny<Uri>(), It.IsAny<INavigationParameters>()))
            .ReturnsAsync(new NavigationResult());

        var success = false;
        Exception error = null;
        await builder.NavigateAsync(() => success = true, ex => error = ex);

        Assert.True(success);
        Assert.Null(error);
    }

    [Fact]
    public async Task NavigateAsync_WithCallbacks_InvokesOnError_WhenFailed()
    {
        var (builder, navMock) = CreateBuilderPair();
        var boom = new NavigationException("fail");
        navMock
            .Setup(n => n.NavigateAsync(It.IsAny<Uri>(), It.IsAny<INavigationParameters>()))
            .ReturnsAsync(new NavigationResult(boom));

        var success = false;
        Exception caught = null;
        await builder.NavigateAsync(() => success = true, ex => caught = ex);

        Assert.False(success);
        Assert.Same(boom, caught);
    }

    [Fact]
    public async Task GoBackToAsync_Delegates_ToNavigationService()
    {
        var (builder, navMock) = CreateBuilderPair();
        var expected = new NavigationResult();
        navMock
            .Setup(n => n.GoBackToAsync("ViewZ", It.IsAny<INavigationParameters>()))
            .ReturnsAsync(expected);

        var result = await builder.GoBackToAsync("ViewZ");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task WithParameters_MergesIntoBuilder()
    {
        var (builder, navMock) = CreateBuilderPair();
        navMock
            .Setup(n => n.NavigateAsync(It.IsAny<Uri>(), It.IsAny<INavigationParameters>()))
            .ReturnsAsync(new NavigationResult())
            .Verifiable();

        var extra = new NavigationParameters { { "k", 42 } };
        await builder.WithParameters(extra).AddSegment("ViewA").NavigateAsync();

        navMock.Verify(n => n.NavigateAsync(
            It.IsAny<Uri>(),
            It.Is<INavigationParameters>(p => p.GetValue<int>("k") == 42)), Times.Once);
    }

    [Fact]
    public void UseRelativeNavigation_FlipsAbsoluteFlag()
    {
        var (builder, _) = CreateBuilderPair();
        var uri = builder
            .AddSegment("A")
            .UseAbsoluteNavigation()
            .UseRelativeNavigation()
            .Uri;

        Assert.Equal("A", uri.ToString());
    }
}
