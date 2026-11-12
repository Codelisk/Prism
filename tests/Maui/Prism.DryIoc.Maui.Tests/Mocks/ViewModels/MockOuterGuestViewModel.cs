using Prism.DryIoc.Maui.Tests.Mocks.Views;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace Prism.DryIoc.Maui.Tests.Mocks.ViewModels;

/// <summary>Mirrors nested-region samples: inner region is navigated when this guest is navigated to.</summary>
public class MockOuterGuestViewModel : BindableBase, IRegionAware
{
    private readonly IRegionManager _regionManager;

    public MockOuterGuestViewModel(IRegionManager regionManager) =>
        _regionManager = regionManager;

    public bool IsNavigationTarget(NavigationContext navigationContext) =>
        navigationContext.NavigatedName() == nameof(MockOuterGuestView);

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    public void OnNavigatedTo(NavigationContext navigationContext) =>
        _regionManager.RequestNavigate("InnerRegion", nameof(MockInnerGuestView));
}
