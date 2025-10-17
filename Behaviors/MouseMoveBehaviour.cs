using System.Windows;
using System.Windows.Input;

namespace Filmauswertung_ModernUI.Behaviors
{
    public static class MouseMoveBehavior
    {
        public static readonly DependencyProperty MouseMoveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseMoveCommand",
                typeof(ICommand),
                typeof(MouseMoveBehavior),
                new PropertyMetadata(null, OnMouseMoveCommandChanged));

        public static void SetMouseMoveCommand(UIElement element, ICommand value) =>
            element.SetValue(MouseMoveCommandProperty, value);

        public static ICommand GetMouseMoveCommand(UIElement element) =>
            (ICommand)element.GetValue(MouseMoveCommandProperty);

        private static void OnMouseMoveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ui)
            {
                ui.MouseMove -= Ui_MouseMove;
                if (e.NewValue != null)
                    ui.MouseMove += Ui_MouseMove;
            }
        }

        private static void Ui_MouseMove(object sender, MouseEventArgs e)
        {
            var ui = (UIElement)sender;
            var command = GetMouseMoveCommand(ui);
            if (command != null && command.CanExecute(null))
            {
                // Pass Point instead of MouseEventArgs
                Point pt = e.GetPosition(ui);
                command.Execute(pt);
            }
        }


        // Optional: MouseLeave to reset value
        public static readonly DependencyProperty MouseLeaveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseLeaveCommand",
                typeof(ICommand),
                typeof(MouseMoveBehavior),
                new PropertyMetadata(null, OnMouseLeaveCommandChanged));

        public static void SetMouseLeaveCommand(UIElement element, ICommand value) =>
            element.SetValue(MouseLeaveCommandProperty, value);

        public static ICommand GetMouseLeaveCommand(UIElement element) =>
            (ICommand)element.GetValue(MouseLeaveCommandProperty);

        private static void OnMouseLeaveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UIElement ui)
            {
                ui.MouseLeave -= Ui_MouseLeave;
                if (e.NewValue != null)
                    ui.MouseLeave += Ui_MouseLeave;
            }
        }

        private static void Ui_MouseLeave(object sender, MouseEventArgs e)
        {
            var ui = (UIElement)sender;
            var command = GetMouseLeaveCommand(ui);
            if (command != null && command.CanExecute(null))
                command.Execute(null);
        }
    }
}
