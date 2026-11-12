using Prism.Common;
using Prism.DryIoc.Maui.Tests.Mocks.Navigation;
using Prism.DryIoc.Maui.Tests.Mocks.ViewModels;
using Prism.DryIoc.Maui.Tests.Mocks.Views;
using Prism.Navigation;
using Prism.Navigation.Xaml;
using TabbedPage = Microsoft.Maui.Controls.TabbedPage;

namespace Prism.DryIoc.Maui.Tests.Fixtures.Navigation;

/// <summary>
/// Targets branches in <see cref="Prism.Navigation.PageNavigationService"/> not exercised by other navigation fixtures.
/// </summary>
public class PageNavigationServiceCoverageTests : TestBase
{
    public PageNavigationServiceCoverageTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task SelectTabAsync_InvalidPipeCount_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism
                .CreateWindow(nav => nav.CreateBuilder()
                    .AddTabbedSegment(s => s.CreateTab("MockViewA").CreateTab("MockViewB"))
                    .NavigateAsync()))
            .Build();
        var window = GetWindow(mauiApp);
        var tabbed = (TabbedPage)window.Page;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(tabbed.CurrentPage);

        var result = await nav.SelectTabAsync("A|B|C", new NavigationParameters());

        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task SelectTabAsync_AbsoluteUri_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism
                .CreateWindow(nav => nav.CreateBuilder()
                    .AddTabbedSegment(s => s.CreateTab("MockViewA").CreateTab("MockViewB"))
                    .NavigateAsync()))
            .Build();
        var window = GetWindow(mauiApp);
        var tabbed = (TabbedPage)window.Page;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(tabbed.CurrentPage);

        var result = await nav.SelectTabAsync("MockViewB", new Uri("http://localhost/View", UriKind.Absolute));

        Assert.False(result.Success);
        Assert.IsType<NavigationException>(result.Exception);
    }

    [Fact]
    public async Task SelectTabAsync_UnknownTab_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism
                .CreateWindow(nav => nav.CreateBuilder()
                    .AddTabbedSegment(s => s.CreateTab("MockViewA").CreateTab("MockViewB"))
                    .NavigateAsync()))
            .Build();
        var window = GetWindow(mauiApp);
        var tabbed = (TabbedPage)window.Page;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(tabbed.CurrentPage);

        var result = await nav.SelectTabAsync("NoSuchTab");

        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task SelectTabAsync_WhenCanNavigateFalse_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism
                .CreateWindow(nav => nav.CreateBuilder()
                    .AddTabbedSegment(s => s.CreateTab("MockViewA").CreateTab("MockViewB"))
                    .NavigateAsync()))
            .Build();
        var window = GetWindow(mauiApp);
        var tabbed = (TabbedPage)window.Page;
        var mockA = (MockViewA)tabbed.CurrentPage;
        ((MockViewAViewModel)mockA.BindingContext).StopNavigation = true;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(mockA);

        var result = await nav.SelectTabAsync("MockViewB");

        Assert.False(result.Success);
        Assert.IsType<NavigationException>(result.Exception);
    }

    [Fact]
    public async Task GoBackToAsync_WhenTargetNotInStack_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism.CreateWindow("NavigationPage/MockViewA/MockViewB"))
            .Build();
        var window = GetWindow(mauiApp);
        var navPage = (NavigationPage)window.Page;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(navPage.CurrentPage);

        var result = await nav.GoBackToAsync("MissingView");

        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task NavigateAsync_UnregisteredSegment_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism.CreateWindow("MockViewA"))
            .Build();
        var window = GetWindow(mauiApp);
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(window.CurrentPage);

        var result = await nav.NavigateAsync("NotARegisteredPageName");

        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task GoBackToRootAsync_WhenCanNavigateFalse_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism.CreateWindow("NavigationPage/MockViewA/MockViewB"))
            .Build();
        var window = GetWindow(mauiApp);
        var navPage = (NavigationPage)window.Page;
        ((MockViewBViewModel)navPage.CurrentPage.BindingContext).StopNavigation = true;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(navPage.CurrentPage);

        var result = await nav.GoBackToRootAsync();

        Assert.False(result.Success);
        Assert.IsType<NavigationException>(result.Exception);
    }

    [Fact]
    public async Task GoBackAsync_WhenCanNavigateFalse_ReturnsFailure()
    {
        var mauiApp = CreateBuilder(prism => prism.CreateWindow("NavigationPage/MockViewA/MockViewB"))
            .Build();
        var window = GetWindow(mauiApp);
        var navPage = (NavigationPage)window.Page;
        ((MockViewBViewModel)navPage.CurrentPage.BindingContext).StopNavigation = true;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(navPage.CurrentPage);

        var result = await nav.GoBackAsync();

        Assert.False(result.Success);
        Assert.IsType<NavigationException>(result.Exception);
    }

    [Fact]
    public async Task GoBackAsync_WhenDoPopReturnsNull_ReturnsFailureWithInnerException()
    {
        var mauiApp = CreateBuilder(prism => prism.CreateWindow("NavigationPage/MockViewA/MockViewB"))
            .Build();
        var window = GetWindow(mauiApp);
        var navPage = (NavigationPage)window.Page;
        var nav = Prism.Navigation.Xaml.Navigation.GetNavigationService(navPage.CurrentPage);

        TestPageNavigationService.ForceNextDoPopToReturnNull = true;
        var result = await nav.GoBackAsync();

        Assert.False(result.Success);
        Assert.NotNull(result.Exception);
    }

}
