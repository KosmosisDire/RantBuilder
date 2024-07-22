


using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NodeBuilder.Internal.Interactions;

public class HoverClassname
{
    public static void AddHover(Control hoverControl, Control classControl, bool setHandled = true)
    {
        hoverControl.AddHandler(Control.PointerEnteredEvent, (sender, e) =>
        {
            classControl.Classes.Set("Hover", true);
            e.Handled = setHandled;
        }, RoutingStrategies.Bubble, true);

        hoverControl.AddHandler(Control.PointerExitedEvent, (sender, e) =>
        {
            classControl.Classes.Set("Hover", false);
            e.Handled = setHandled;
        }, RoutingStrategies.Bubble, true);
    }
}