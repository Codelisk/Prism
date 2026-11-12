namespace Prism.DryIoc.Maui.Tests.Mocks.Views;

/// <summary>Outer region guest that hosts an inner <see cref="ContentView"/> region.</summary>
public class MockOuterGuestView : ContentView
{
    public MockOuterGuestView()
    {
        InnerRegionHost = new ContentView();
        InnerRegionHost.SetValue(Prism.Navigation.Regions.Xaml.RegionManager.RegionNameProperty, "InnerRegion");
        Content = InnerRegionHost;
    }

    public ContentView InnerRegionHost { get; }
}
