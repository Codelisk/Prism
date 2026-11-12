using Microsoft.Maui.Controls;
using Prism.Navigation;

namespace Prism.Maui.Tests.Mocks;

/// <summary>
/// Minimal stand-in for <see cref="Application"/> used by legacy navigation fixtures:
/// exposes <see cref="MainPage"/> backed by a <see cref="PrismWindow"/> for <see cref="IWindowManager"/> integration.
/// </summary>
public sealed class ApplicationMock
{
    private readonly PrismWindow _window = new();

    public ApplicationMock()
    {
    }

    public ApplicationMock(Page mainPage)
    {
        MainPage = mainPage;
    }

    public Page MainPage
    {
        get => _window.Page;
        set => _window.Page = value;
    }

    internal PrismWindow Window => _window;
}
