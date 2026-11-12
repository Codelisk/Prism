using Microsoft.Maui.Controls;
using Prism.Common;

namespace Prism.Maui.Tests.Mocks;

public sealed class MutablePageAccessor : IPageAccessor
{
    public Page Page { get; set; }
}
