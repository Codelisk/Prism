using Prism.Dialogs;
using Prism.Mvvm;

namespace Prism.DryIoc.Maui.Tests.Mocks.ViewModels;

/// <summary>Minimal <see cref="IDialogAware"/> for dialog regression tests; view closes itself on <see cref="VisualElement.Loaded"/>.</summary>
public class TestDialogViewModel : BindableBase, IDialogAware
{
    private DialogCloseListener _requestClose;

    public TestDialogViewModel()
    {
        _requestClose = new DialogCloseListener();
    }

    public DialogCloseListener RequestClose
    {
        get => _requestClose;
        set => SetProperty(ref _requestClose, value);
    }

    public bool CanCloseDialog() => true;

    public void OnDialogClosed()
    {
    }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }
}
