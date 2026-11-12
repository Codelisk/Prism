using System;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Tests.Ioc.ViewModels;
using Prism.Tests.Ioc.Views;
using Xunit;

namespace Prism.Tests.Ioc
{
    // Orderlyze-Fork: Tests für die eigene ContainerAot-Fassung (upstream liegt sie in Prism.Container 10).
    public class ContainerAotFixture
    {
        [Fact]
        public void PreservedTypeIsFoundByTypeAndByFullName()
        {
            ContainerAot.Preserve<PreservedByType>();

            Assert.True(ContainerAot.TryGetImplementationType(typeof(PreservedByType), out var byType));
            Assert.Equal(typeof(PreservedByType), byType);
            Assert.True(ContainerAot.TryGetImplementationType(typeof(PreservedByType).FullName!, typeof(PreservedByType).Assembly, out var byName));
            Assert.Equal(typeof(PreservedByType), byName);
        }

        [Fact]
        public void ServiceIsMappedToItsImplementation()
        {
            ContainerAot.Preserve<IPreservedService, PreservedService>();

            Assert.True(ContainerAot.TryGetImplementationType(typeof(IPreservedService), out var forService));
            Assert.Equal(typeof(PreservedService), forService);
            Assert.True(ContainerAot.TryGetImplementationType(typeof(PreservedService), out var forImplementation));
            Assert.Equal(typeof(PreservedService), forImplementation);
            Assert.True(ContainerAot.TryGetImplementationType(typeof(PreservedService).FullName!, typeof(PreservedService).Assembly, out var byName));
            Assert.Equal(typeof(PreservedService), byName);
        }

        [Fact]
        public void TypesThatWereNotPreservedAreNotFound()
        {
            Assert.False(ContainerAot.TryGetImplementationType(typeof(NeverPreserved), out var byType));
            Assert.Null(byType);
            Assert.False(ContainerAot.TryGetImplementationType(typeof(NeverPreserved).FullName!, typeof(NeverPreserved).Assembly, out var byName));
            Assert.Null(byName);
        }

        [Fact]
        public void TheFullNameOnlyCountsInTheAssemblyOfTheType()
        {
            ContainerAot.Preserve<PreservedByType>();

            Assert.False(ContainerAot.TryGetImplementationType(typeof(PreservedByType).FullName!, typeof(object).Assembly, out _));
        }

        private sealed class PreservedByType { }

        private interface IPreservedService { }

        private sealed class PreservedService : IPreservedService { }

        private sealed class NeverPreserved { }
    }

    [CollectionDefinition("ViewModel location without reflection", DisableParallelization = true)]
    public sealed class ViewModelLocationWithoutReflectionCollection { }

    /// <summary>
    /// So läuft die ViewModel-Suche unter Native AOT: ohne Reflection findet sie nur vorgemerkte Typen.
    /// </summary>
    [Collection("ViewModel location without reflection")]
    public sealed class ViewModelLocationWithoutReflectionFixture : IDisposable
    {
        private const string ReflectionSwitch = "Prism.Mvvm.ReflectionBasedViewModelLocationEnabled";

        public ViewModelLocationWithoutReflectionFixture()
        {
            ViewModelLocationProvider.Reset();
            AppContext.SetSwitch(ReflectionSwitch, false);
        }

        public void Dispose()
        {
            AppContext.SetSwitch(ReflectionSwitch, true);
            ViewModelLocationProvider.Reset();
        }

        [Fact]
        public void PreservedViewModelIsFoundByConvention()
        {
            ContainerAot.Preserve<AotViewModel>();
            object? actual = null;

            ViewModelLocationProvider.AutoWireViewModelChanged(new AotView(), (_, vm) => actual = vm);

            Assert.IsType<AotViewModel>(actual);
        }

        [Fact]
        public void ViewModelThatWasNotPreservedIsNotFound()
        {
            var called = false;

            ViewModelLocationProvider.AutoWireViewModelChanged(new NotPreservedView(), (_, _) => called = true);

            Assert.False(called);
        }
    }
}

namespace Prism.Tests.Ioc.Views
{
    public sealed class AotView { }

    public sealed class NotPreservedView { }
}

namespace Prism.Tests.Ioc.ViewModels
{
    public sealed class AotViewModel { }

    public sealed class NotPreservedViewModel { }
}
