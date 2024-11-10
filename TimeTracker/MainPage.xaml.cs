using H.NotifyIcon;
using CommunityToolkit.Mvvm.Input;

namespace TimeTracker
{
    public partial class MainPage : ContentPage
    {
        bool IsWindowVisible = false;
        bool FirstLaunch = true;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object? sender, EventArgs e)
        {
            var s = State.Get();
            lst_Categories.Clear();
            foreach (var c in s.Categories)
                lst_Categories.Add(new CategoryWidget(c));

            if (FirstLaunch)
                Application.Current?.MainPage?.Window?.Hide(true);
            FirstLaunch = false;
        }

        private void btn_New_Clicked(object sender, EventArgs e)
        {
            Navigation.PushAsync(new CategoryPage(null));
        }

        [RelayCommand]
        public void ShowHideWindow()
        {
            var window = Application.Current?.MainPage?.Window;
            if (window == null)
                return;

            if (IsWindowVisible)
                window.Hide(true);
            else
                window.Show();
            IsWindowVisible = !IsWindowVisible;
        }

        [RelayCommand]
        public void ExitApplication()
        {
            Application.Current?.Quit();
        }
    }
}
