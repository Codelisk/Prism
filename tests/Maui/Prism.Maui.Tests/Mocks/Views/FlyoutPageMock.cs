using Microsoft.Maui.Controls;

namespace Prism.Maui.Tests.Mocks.Views;

public class FlyoutPageMock : FlyoutPage, IFlyoutPageOptions, IDestructible, IPageNavigationEventRecordable
{
    public PageNavigationEventRecorder PageNavigationEventRecorder { get; set; }

    public FlyoutPageMock() : this(null)
    {
    }

    public FlyoutPageMock(PageNavigationEventRecorder recorder)
    {
        Flyout = new ContentPageMock(recorder) { Title = "Master" };
        Detail = new ContentPageMock(recorder);

        //ViewModelLocator.SetAutowireViewModel(this, true);

        PageNavigationEventRecorder = recorder;
        if (BindingContext is IPageNavigationEventRecordable recordable)
            recordable.PageNavigationEventRecorder = recorder;
    }

    public FlyoutPageMock(PageNavigationEventRecorder recorder, Page masterPage, Page detailPage)
    {
        Flyout = masterPage;
        Detail = detailPage;

        //ViewModelLocator.SetAutowireViewModel(this, true);

        PageNavigationEventRecorder = recorder;
        if (BindingContext is IPageNavigationEventRecordable recordable)
            recordable.PageNavigationEventRecorder = recorder;
    }

    public bool IsPresentedAfterNavigation { get; set; }
    public void Destroy()
    {
        PageNavigationEventRecorder.Record(this, PageNavigationEvent.Destroy);
    }
}
