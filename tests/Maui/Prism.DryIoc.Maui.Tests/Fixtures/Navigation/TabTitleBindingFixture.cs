using Microsoft.Maui.Dispatching;
using Prism.DryIoc.Maui.Tests.Mocks;
using Prism.Navigation.Xaml;

namespace Prism.DryIoc.Maui.Tests.Fixtures.Navigation;

// Orderlyze-Fork: die typisierten Bindungen, die PageNavigationService einer NavigationPage im Tab gibt (statt Pfad als Text).
public class TabTitleBindingFixture
{
    public TabTitleBindingFixture()
    {
        DispatcherProvider.SetCurrent(TestDispatcher.Provider);
    }

    [Fact]
    public void FollowsTheRootPage()
    {
        var root = new ContentPage { Title = "Root", IconImageSource = "root.png" };
        var navigationPage = new NavigationPage(root);

        PageNavigationService.BindTabTitle(navigationPage, TabBindingSource.RootPage);

        Assert.Equal("Root", navigationPage.Title);
        Assert.Same(root.IconImageSource, navigationPage.IconImageSource);

        root.Title = "Renamed";
        root.IconImageSource = "renamed.png";

        Assert.Equal("Renamed", navigationPage.Title);
        Assert.Same(root.IconImageSource, navigationPage.IconImageSource);
    }

    [Fact]
    public async Task StaysWithTheRootPageWhenAPageIsPushed()
    {
        var root = new ContentPage { Title = "Root" };
        var navigationPage = new NavigationPage(root);
        PageNavigationService.BindTabTitle(navigationPage, TabBindingSource.RootPage);

        await navigationPage.PushAsync(new ContentPage { Title = "Top" });

        Assert.Equal("Root", navigationPage.Title);
    }

    [Fact]
    public async Task FollowsTheCurrentPage()
    {
        var root = new ContentPage { Title = "Root", IconImageSource = "root.png" };
        var navigationPage = new NavigationPage(root);
        PageNavigationService.BindTabTitle(navigationPage, TabBindingSource.CurrentPage);

        Assert.Equal("Root", navigationPage.Title);

        var top = new ContentPage { Title = "Top" };
        await navigationPage.PushAsync(top);

        Assert.Equal("Top", navigationPage.Title);
        Assert.Null(navigationPage.IconImageSource);

        top.Title = "Renamed";
        Assert.Equal("Renamed", navigationPage.Title);

        await navigationPage.PopAsync();

        Assert.Equal("Root", navigationPage.Title);
        Assert.Same(root.IconImageSource, navigationPage.IconImageSource);
    }

    [Fact]
    public void KeepsATitleThatIsAlreadySet()
    {
        var root = new ContentPage { Title = "Root", IconImageSource = "root.png" };
        var navigationPage = new NavigationPage(root) { Title = "Own" };

        PageNavigationService.BindTabTitle(navigationPage, TabBindingSource.RootPage);
        root.Title = "Renamed";

        Assert.Equal("Own", navigationPage.Title);
        Assert.Same(root.IconImageSource, navigationPage.IconImageSource);
    }
}
