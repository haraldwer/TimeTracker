using H.NotifyIcon;
using CommunityToolkit.Mvvm.Input;
using System.Diagnostics;

namespace TimeTracker
{
    public partial class MainPage : ContentPage
    {
        bool IsWindowVisible = true;
        bool FirstLaunch = true;

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
            Loaded += MainPage_Loaded;
        }

        private void MainPage_Loaded(object? sender, EventArgs e)
        {
            if (FirstLaunch)
            {
                SetLoading(true, true); 
                FirstLaunch = false;

                Task.Run(async () =>
                {
                    var auth = await CloudUtility.LoadAuth();
                    if (!await CloudUtility.CreateClient(auth))
                        Trace.WriteLine("Failed to connect to cloud");
                    bool success = await State.Load();
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        SetLoading(false, success);
                        if (success)
                            CreateCategories();
                    });
                });
                return;
            }

            SetLoading(false, true);
            CreateCategories(); 
        }

        private void SetLoading(bool InLoading, bool InSuccess)
        {
            scrl_Main.IsVisible = !InLoading && InSuccess;
            ld_Loading.IsVisible = InLoading;
            lbl_Error.IsVisible = !InSuccess;
        }

        void CreateCategories()
        {
            var s = State.Get();
            if (s == null)
                return;
            lst_Categories.Clear();
            foreach (var c in s.Categories)
                lst_Categories.Add(new CategoryWidget(c));
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

        private void ImageButton_Clicked(object sender, EventArgs e)
        {
            ShowHideWindow(); 
        }
    }
}
