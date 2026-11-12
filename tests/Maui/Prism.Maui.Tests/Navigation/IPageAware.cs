using Microsoft.Maui.Controls;

namespace Prism.Maui.Tests.Navigation;

/// <summary>
/// Legacy test shim (pre-<see cref="Prism.Common.IPageAccessor"/>) so fixtures can assign the logical navigation root page.
/// </summary>
public interface IPageAware
{
    Page Page { get; set; }
}
