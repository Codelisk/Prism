using Prism.Dialogs;
using Prism.DryIoc.Maui.Tests.Mocks.ViewModels;
using Prism.DryIoc.Maui.Tests.Mocks.Views;
using Prism.Ioc;
using Prism.Navigation.Xaml;

namespace Prism.DryIoc.Maui.Tests.Fixtures.Navigation;

/// <summary>Regression tests for dialog + navigation interactions (GitHub #3395).</summary>
public class DialogNavigationRegressionFixture : TestBase
{
    public DialogNavigationRegressionFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task Issue3395_GoBackAsync_After_ShowDialogAsync_Completes_Pops_Navigation_Page()
    {
        var mauiApp = CreateBuilder(prism => prism
                .RegisterTypes(c =>
                {
                    c.RegisterDialog<TestDialogView, TestDialogViewModel>("TestDialog");
                })
                .CreateWindow("NavigationPage/MockViewA/MockViewB"))
            .Build();
        var window = GetWindow(mauiApp);
        var navPage = Assert.IsAssignableFrom<Microsoft.Maui.Controls.NavigationPage>(window.Page);
        Assert.IsType<MockViewB>(navPage.CurrentPage);
        Assert.Equal(2, navPage.Navigation.NavigationStack.Count);

        var container = navPage.CurrentPage.GetContainerProvider();
        var dialogService = container.Resolve<IDialogService>();
        var navigationService = Prism.Navigation.Xaml.Navigation.GetNavigationService(navPage.CurrentPage);
        var hostPage = navPage.CurrentPage;

        Assert.Empty(hostPage.Navigation.ModalStack);
        var dialogResult = await dialogService.ShowDialogAsync("TestDialog");
        Assert.NotNull(dialogResult);
        Assert.Null(dialogResult.Exception);
        Assert.Equal(ButtonResult.OK, dialogResult.Result);
        Assert.Empty(hostPage.Navigation.ModalStack);
        Assert.Empty(IDialogContainer.DialogStack);

        var goBackResult = await navigationService.GoBackAsync();
        Assert.True(goBackResult.Success);
        Assert.Null(goBackResult.Exception);
        Assert.IsType<MockViewA>(navPage.CurrentPage);
        Assert.Single(navPage.Navigation.NavigationStack);
    }
}
