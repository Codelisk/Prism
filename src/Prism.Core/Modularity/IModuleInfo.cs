using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace Prism.Modularity
{
    /// <summary>
    /// Set of properties for each Module
    /// </summary>
    public interface IModuleInfo : IModuleCatalogItem
    {
        /// <summary>
        /// The module names this instance depends on.
        /// </summary>
        Collection<string> DependsOn { get; set; }

        /// <summary>
        /// Gets or Sets the <see cref="InitializationMode" />
        /// </summary>
        InitializationMode InitializationMode { get; set; }

        /// <summary>
        /// The name of the module
        /// </summary>
        string ModuleName { get; set; }

        /// <summary>
        /// The module's type
        /// </summary>
        /// <remarks>
        /// The assembly-qualified name of a module type whose constructors are kept; the module initializer loads it by
        /// this name and lets the container create it.
        /// </remarks>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
        string ModuleType { get; set; }

        /// <summary>
        /// A string ref is a location reference to load the module as it may not be already loaded in the Appdomain in some cases may need to be downloaded.
        /// </summary>
        /// <remarks>Support depends on the module loader and module information implementation. Implementations that use registered types may not support assembly locations.</remarks>
        string Ref { get; set; }

        /// <summary>
        /// Gets or Sets the current <see cref="ModuleState" />
        /// </summary>
        ModuleState State { get; set; }
    }
}
