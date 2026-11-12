using Prism.DryIoc.Maui.Tests.Mocks.Views;
using Prism.Mvvm;
using Prism.Navigation.Regions;

namespace Prism.DryIoc.Maui.Tests.Mocks.ViewModels;

public class MockInnerGuestViewModel : BindableBase, IRegionAware
{
    public bool IsNavigationTarget(NavigationContext navigationContext) =>
        navigationContext.NavigatedName() == nameof(MockInnerGuestView);

    public void OnNavigatedFrom(NavigationContext navigationContext)
    {
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
    }
}
