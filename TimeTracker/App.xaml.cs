using H.NotifyIcon;
using Microsoft.Maui.Controls;

namespace TimeTracker
{
    public partial class App : Application
    {
        IDispatcherTimer Timer;

        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            const int newWidth = 500;
            const int newHeight = 500;
            window.Width = newWidth;
            window.Height = newHeight;

            Timer = Current.Dispatcher.CreateTimer();
            Timer.Interval = TimeSpan.FromSeconds(5);
            Timer.IsRepeating = true;
            Timer.Tick += (a, e) =>
            {
                State.Get().Update(); 
            };
            Timer.Start();
            State.Get().Update();

            window.Destroying += (a, e) =>
            {
                Timer.Stop();
                var s = State.Get();
                s.EndAllSessions();
                s.Save();
            };

            return window;
        }
    }
}
