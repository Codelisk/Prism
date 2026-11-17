using System.Diagnostics.CodeAnalysis;

namespace Prism.Dialogs;

/// <summary>
/// Provides a way for objects involved in Dialogs to be notified of Dialog activities.
/// </summary>
/// <remarks>
/// The dialog service sets <see cref="RequestClose"/> by reflection (<see cref="DialogUtilities"/>): the annotation keeps
/// that property and the fields it may be stored in on every dialog view model, also in a trimmed app.
/// </remarks>
[DynamicallyAccessedMembers(DialogUtilities.ListenerMembers)]
public interface IDialogAware
{
    /// <summary>
    /// Evaluates whether the Dialog is in a state that would allow the Dialog to Close
    /// </summary>
    /// <returns><c>true</c> if the Dialog can close</returns>
    bool CanCloseDialog();

    /// <summary>
    /// Provides a callback to clean up resources or finalize tasks when the Dialog has been closed
    /// </summary>
    void OnDialogClosed();

    /// <summary>
    /// Initializes the state of the Dialog with provided DialogParameters
    /// </summary>
    /// <param name="parameters"></param>
    void OnDialogOpened(IDialogParameters parameters);

    /// <summary>
    /// The <see cref="DialogCloseListener"/> will be set by the <see cref="IDialogService"/> and can be called to
    /// invoke the close of the Dialog.
    /// </summary>
    DialogCloseListener RequestClose { get; }
}
