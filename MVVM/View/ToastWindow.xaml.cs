using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace Filmauswertung_ModernUI.MVVM.View
{
    public partial class ToastWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public ToastWindow()
        {
            InitializeComponent();
            DataContext = this;

            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
            _timer.Interval = ToastDuration;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();
            Close();
        }

        public void ShowToast()
        {
            Show();
            _timer.Start();
        }

        public string ToastMessage
        {
            get => (string)GetValue(ToastMessageProperty);
            set
            {
                SetValue(ToastMessageProperty, value);
            }
        }

        public static readonly DependencyProperty ToastMessageProperty =
            DependencyProperty.Register(
                nameof(ToastMessage),
                typeof(string),
                typeof(ToastWindow),
                new PropertyMetadata(string.Empty));

        public TimeSpan ToastDuration
        {
            get => (TimeSpan)GetValue(ToastDurationProperty);
            set
            {
                SetValue(ToastDurationProperty, value);
            }
        }

        public static readonly DependencyProperty ToastDurationProperty =
            DependencyProperty.Register(
                nameof(ToastDuration),
                typeof(TimeSpan),
                typeof(ToastWindow),
                new PropertyMetadata(TimeSpan.FromSeconds(1), OnToastDurationChanged));

        private static void OnToastDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToastWindow window && e.NewValue is TimeSpan newDuration)
            {
                window._timer.Interval = newDuration;
            }
        }
    }
}
