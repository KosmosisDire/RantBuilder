using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace NodeBuilder.Internal.Interactions;

public class DragDropInteraction
{
    public DateTime lastMouseMovedTime;
    public Vector MouseDelta { get; private set; }
    public MovingAverageVector AverageMouseDelta { get; private set; }
    public Point MousePosition { get; private set; }
    public Point RelativeMousePosition { get; private set; }
    public bool IsPressed { get; private set; } = false;
    public bool IsDragging { get; private set; } = false;
    public Control DraggedControl { get; private set; }

    public Action<DragDropInteraction> DragStartedAction { set => DragStarted += value; }
    public Action<DragDropInteraction> DragEndedAction { set => DragEnded += value; }
    public Action<DragDropInteraction> DraggedAction { set => Dragged += value; }

    public float Velocity { get; set; }

    public event Action<DragDropInteraction> DragStarted;
    public event Action<DragDropInteraction> DragEnded;
    public event Action<DragDropInteraction> Dragged;


    public DragDropInteraction(Control control)
    {
        AverageMouseDelta = new MovingAverageVector(10);
        DraggedControl = control;

        control.PointerPressed += (sender, e) =>
        {
            IsPressed = true;
            MousePosition = e.GetPosition(null);
            RelativeMousePosition = e.GetPosition(control);
            e.Handled = true;
        };

        control.PointerReleased += (sender, e) =>
        {
            IsPressed = false;
            IsDragging = false;
            DraggedControl.Classes.Set("Dragging", false);
            MousePosition = e.GetPosition(null);
            RelativeMousePosition = e.GetPosition(control);
            DragEnded?.Invoke(this);
            _ = RunVelocityCoroutine();
            e.Handled = true;
        };

        control.PointerMoved += (sender, e) =>
        {
            var newMousePosition = e.GetPosition(null);
            MouseDelta = newMousePosition - MousePosition;
            MousePosition = newMousePosition;
            RelativeMousePosition = e.GetPosition(control);
            lastMouseMovedTime = DateTime.Now;

            if (IsPressed && !IsDragging)
            {
                AverageMouseDelta.Clear();
                AverageMouseDelta.AddSample(MouseDelta);
                IsDragging = true;
                DragStarted?.Invoke(this);
                DraggedControl.Classes.Set("Dragging", true);
            }
            if (IsDragging)
            {
                Dragged?.Invoke(this);
            }

            AverageMouseDelta.AddSample(MouseDelta);
            e.Handled = true;
        };
    }

    public async Task RunVelocityCoroutine()
    {
        if (lastMouseMovedTime < DateTime.Now.AddSeconds(-0.1f))
        {
            return;
        }

        var velocity = Velocity;
        var maxVelocity = Velocity;
        var startMouseDelta = AverageMouseDelta.Value * AverageMouseDelta.Value.Length * AverageMouseDelta.AverageDeltaTime.TotalSeconds * 35;

        while (velocity > 0)
        {
            velocity -= 0.01f;
            MouseDelta = startMouseDelta * velocity / maxVelocity;
            Dragged?.Invoke(this);
            await Task.Delay(10);
            DraggedControl.InvalidateVisual();

            if (IsDragging || IsPressed)
            {
                return;
            }
        }
    }


}
