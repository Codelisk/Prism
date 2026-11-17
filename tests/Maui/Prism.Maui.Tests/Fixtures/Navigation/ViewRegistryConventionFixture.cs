using Prism.Maui.Tests.Fixtures.Navigation.ConventionViewModels;
using Prism.Maui.Tests.Fixtures.Navigation.ConventionViews;
using Prism.Maui.Tests.Mocks.Ioc;

namespace Prism.Maui.Tests.Fixtures.Navigation;

// Orderlyze-Fork: the view of a view model without its own registration is found by the naming convention among the
// registered views, without loading candidate types by name (trimming, NativeAOT).
public class ViewRegistryConventionFixture
{
    [Fact]
    public void ViewModelWithoutRegistration_FindsTheViewNamedByConvention()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<ConventionMockPage>("ConventionMock");

        var registry = container.Resolve<NavigationRegistry>();

        Assert.Equal("ConventionMock", registry.GetViewModelNavigationKey(typeof(ConventionMockPageViewModel)));
        Assert.Equal("ConventionMock", registry.GetViewModelNavigationKey(typeof(ConventionMockViewModel)));
    }

    [Fact]
    public void ViewModelWithoutMatchingView_Throws()
    {
        var container = new TestContainer();
        var registry = container.Resolve<NavigationRegistry>();

        Assert.Throws<KeyNotFoundException>(() => registry.GetViewModelNavigationKey(typeof(UnmatchedConventionViewModel)));
    }
}
