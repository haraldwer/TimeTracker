
namespace TimeTracker
{
    public partial class App : Application
    {
        IDispatcherTimer Timer;

        public App()
        {
            InitializeComponent();
            MainPage = new AppShell();
            if (Current != null)
                Current.UserAppTheme = AppTheme.Dark;
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
                var s = State.Get();
                if (s != null)
                    s.Update(); 
            };
            Timer.Start();

            window.Destroying += (a, e) =>
            {
                Timer.Stop();
                var s = State.Get();
                if (s == null)
                    return;
                s.EndAllSessions();
                bool finished = false;
                Task.Run(async () =>
                {
                    await s.Save();
                    finished = true;
                });
                while (!finished)
                    Thread.Sleep(200);
            };

            return window;
        }
    }
}
