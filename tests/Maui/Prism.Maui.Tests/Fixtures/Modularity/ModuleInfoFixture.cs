using System;
using System.Linq;
using Prism.Ioc;
using Prism.Modularity;

namespace Prism.Maui.Tests.Fixtures.Modularity;

// Orderlyze-Fork: ModuleInfo keeps the module type itself instead of loading it again by its name (trimming, NativeAOT).
public class ModuleInfoFixture
{
    [Fact]
    public void AddModuleWithDependencies_KeepsTypeNameModeAndDependencies()
    {
        var catalog = new ModuleCatalog([]);

        catalog.AddModule<DependentModule>("Dependent", InitializationMode.OnDemand, "Explicit");

        var module = Assert.IsType<ModuleInfo>(Assert.Single(catalog.Modules));
        Assert.Equal(typeof(DependentModule), module.ModuleType);
        Assert.Equal("Dependent", module.ModuleName);
        Assert.Equal(InitializationMode.OnDemand, module.InitializationMode);
        Assert.Equal(new[] { "Attributed", "Explicit" }, module.DependsOn.ToArray());
    }

    [Fact]
    public void ModuleTypeName_LoadsTheSameType()
    {
        IModuleInfo module = new ModuleInfo(typeof(DependentModule), "Dependent");

        Assert.Equal(typeof(DependentModule).AssemblyQualifiedName, module.ModuleType);
        Assert.Equal(typeof(DependentModule), Type.GetType(module.ModuleType, true));
    }

    [Fact]
    public void NameBasedConstructor_StillLoadsTheType()
    {
        var module = new ModuleInfo("ByName", "Prism.Maui.Tests.Fixtures.Modularity.DependentModule, Prism.Maui.Tests", "Explicit");

        Assert.Equal(typeof(DependentModule), module.ModuleType);
        Assert.Equal("ByName", module.ModuleName);
        Assert.Equal(new[] { "Attributed", "Explicit" }, module.DependsOn.ToArray());
    }

    [Fact]
    public void TypeBasedConstructor_RejectsEmptyName()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => new ModuleInfo(typeof(DependentModule), " "));

        Assert.Equal("name", ex.ParamName);
    }
}

[ModuleDependency("Attributed")]
public class DependentModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry) { }

    public void OnInitialized(IContainerProvider containerProvider) { }
}
