namespace Prism.DryIoc.Maui.Tests.Mocks.Views;

/// <summary>Guest view injected into the nested inner region.</summary>
public class MockInnerGuestView : ContentView
{
    public MockInnerGuestView()
    {
        Content = new Label { Text = nameof(MockInnerGuestView) };
    }
}
