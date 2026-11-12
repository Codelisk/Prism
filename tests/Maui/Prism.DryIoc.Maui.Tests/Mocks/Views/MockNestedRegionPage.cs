namespace Prism.DryIoc.Maui.Tests.Mocks.Views;

/// <summary>Shell page with a single outer <see cref="ContentView"/> region (GitHub #3332 repro shape).</summary>
public class MockNestedRegionPage : ContentPage
{
    public MockNestedRegionPage()
    {
        OuterRegion = new ContentView();
        OuterRegion.SetValue(Prism.Navigation.Regions.Xaml.RegionManager.RegionNameProperty, "OuterRegion");
        Content = OuterRegion;
    }

    public ContentView OuterRegion { get; }
}
