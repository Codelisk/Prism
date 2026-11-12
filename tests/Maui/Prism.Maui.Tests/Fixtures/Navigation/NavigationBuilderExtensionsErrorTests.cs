using Microsoft.Maui.Controls;
using Moq;
using Prism.Common;
using Prism.Maui.Tests.Mocks.Ioc;
using Prism.Navigation;
using Prism.Navigation.Builder;

namespace Prism.Maui.Tests.Fixtures.Navigation;

public class NavigationBuilderExtensionsErrorTests
{
    [Fact]
    public void AddSegment_TViewModel_Throws_WhenViewModelIsVisualElement()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        var ex = Assert.Throws<NavigationException>(() => builder.AddSegment<VisualElement>());
        Assert.Equal(NavigationException.MvvmPatternBreak, ex.Message);
    }

    [Fact]
    public void AddNavigationPage_Throws_WhenNoNavigationPageRegistered()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        var ex = Assert.Throws<NavigationException>(() => builder.AddNavigationPage());
        Assert.Equal(NavigationException.NoPageIsRegistered, ex.Message);
    }

    private sealed class NonRegistryBuilder : INavigationBuilder
    {
        public Uri Uri => throw new NotImplementedException();

        public INavigationBuilder AddSegment(string segmentName, Action<ISegmentBuilder> configureSegment) =>
            throw new NotImplementedException();

        public INavigationBuilder AddTabbedSegment(Action<ITabbedSegmentBuilder> configuration) =>
            throw new NotImplementedException();

        public INavigationBuilder AddTabbedSegment(string segmentName, Action<ITabbedSegmentBuilder> configureSegment) =>
            throw new NotImplementedException();

        public INavigationBuilder AddParameter(string key, object value) => throw new NotImplementedException();

        public Task<INavigationResult> GoBackToAsync(string name) => throw new NotImplementedException();

        public Task<INavigationResult> NavigateAsync() => throw new NotImplementedException();

        public Task NavigateAsync(Action<Exception> onError) => throw new NotImplementedException();

        public Task NavigateAsync(Action onSuccess, Action<Exception> onError) => throw new NotImplementedException();

        public INavigationBuilder UseAbsoluteNavigation(bool absolute) => throw new NotImplementedException();

        public INavigationBuilder UseRelativeNavigation() => throw new NotImplementedException();

        public INavigationBuilder WithParameters(INavigationParameters parameters) => throw new NotImplementedException();
    }

    [Fact]
    public void AddNavigationPage_Throws_WhenBuilderNotRegistryAware()
    {
        var builder = new NonRegistryBuilder();
        var ex = Assert.Throws<Exception>(() => NavigationBuilderExtensions.AddNavigationPage(builder));
        Assert.Contains("IRegistryAware", ex.Message);
    }

    [Fact]
    public void AddSegment_NullConfigure_DoesNotThrow()
    {
        var container = new TestContainer();
        container.RegisterForNavigation<TabbedPage>();
        var navigationService = new Mock<INavigationService>();
        navigationService
            .As<IRegistryAware>()
            .Setup(x => x.Registry)
            .Returns(container.Resolve<NavigationRegistry>());
        var builder = navigationService.Object.CreateBuilder();

        var uri = builder.AddSegment("X", null!).Uri;
        Assert.Equal("X", uri.ToString());
    }
}
