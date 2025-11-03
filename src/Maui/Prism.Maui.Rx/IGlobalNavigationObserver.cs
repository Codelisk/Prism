using Prism.Navigation;

namespace Prism.Maui.Rx;


public interface IGlobalNavigationObserver
{
    IObservable<NavigationRequestContext> NavigationRequest { get; }
}
