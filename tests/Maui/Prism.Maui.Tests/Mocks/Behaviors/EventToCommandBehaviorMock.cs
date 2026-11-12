using Prism.Behaviors;

namespace Prism.Maui.Tests.Mocks.Behaviors;

public class EventToCommandBehaviorMock : EventToCommandBehavior
{
    /// <summary>
    /// Invokes <see cref="EventToCommandBehavior.OnEventRaised"/> without going through the
    /// strongly typed event delegate (e.g. <c>SelectionChangedEventArgs</c> for <see cref="Microsoft.Maui.Controls.CollectionView"/>).
    /// </summary>
    public void RaiseEvent(object sender, EventArgs eventArgs)
    {
        OnEventRaised(sender, eventArgs);
    }
}
