#nullable enable
using Microsoft.Maui.Controls;
using Prism.Navigation;

namespace Prism.Maui.Tests.Mocks;

public sealed class TestWindowManager : IWindowManager
{
    public TestWindowManager(PrismWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);
        Window = window;
        Windows = new[] { window };
        Current = window;
    }

    public PrismWindow Window { get; }

    public IReadOnlyList<Window> Windows { get; }

    public Window? Current { get; }

    public void OpenWindow(Window window)
    {
    }

    public void CloseWindow(Window window)
    {
    }
}
