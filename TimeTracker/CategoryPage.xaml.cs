using CommunityToolkit.Maui.Alerts;
using System.Diagnostics;
using System.Linq;

namespace TimeTracker;

public partial class CategoryPage : ContentPage
{
    Category Category;
    Dictionary<string, ProcessWidget> WidgetMap = new();

	public CategoryPage(Category? InCategory)
	{
		InitializeComponent();
        Category = InCategory;
        Category ??= new();
        Loaded += CategoryPage_Loaded;
	}

    private void CategoryPage_Loaded(object? sender, EventArgs e)
    {
        lbl_CategoryName.Text = Category.Name;
        tbx_CategoryName.Text = Category.Name;

        if (Category.Name == "")
        {
            lbl_CategoryName.IsVisible = false;
            tbx_CategoryName.IsVisible = true;
        }
        else
        {
            lbl_CategoryName.IsVisible = true;
            tbx_CategoryName.IsVisible = false;
        }

        lst_Processes.Clear();
        foreach (var n in Category.ProcessNames)
        {
            var w = new ProcessWidget(n, ProcessClicked);
            lst_Processes.Add(w);
            w.SetSelected(true);
            WidgetMap.Add(n, w);
        }

        Task.Run(LoadProcesses);
    }

    void LoadProcesses()
    {
        List<Process> queue = new();
        List<Process> totalQueue = new();
        Func<Task> a = async () =>
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                foreach (var p in queue)
                {
                    if (WidgetMap.ContainsKey(p.ProcessName))
                    {
                        WidgetMap[p.ProcessName].SetProcess(p);
                        continue;
                    }
                    var w = new ProcessWidget(p.ProcessName, ProcessClicked);
                    w.SetProcess(p);
                    w.SetSelected(false);
                    lst_Processes.Add(w);
                }
            });
            queue.Clear(); 
        };

        List<Process> processes = Process.GetProcesses().ToList();
        processes.Sort((p1, p2) => p1.ProcessName.CompareTo(p2.ProcessName));
        foreach (var p in processes)
        {
            try
            {
                if (p.BasePriority < 8 ||
                    p.HasExited ||
                    string.IsNullOrEmpty(p.MainWindowTitle))
                    continue;
                var t = p.StartTime; // Try access time
            }
            catch
            {
                continue;
            }
            if (totalQueue.Any((q) => p.ProcessName.Contains(q.ProcessName) || q.ProcessName.Contains(p.ProcessName)))
                continue;
            totalQueue.Add(p);
            queue.Add(p);
            if (queue.Count > 3)
                a().Wait();
        }
        a().Wait();
    }


    void ProcessClicked(ProcessWidget InWidget, Process InProcess, string InName)
    {
        if (Category.ProcessNames.Contains(InName))
        {
            Category.ProcessNames.Remove(InName);
            InWidget.SetSelected(false);
        }
        else
        {
            Category.ProcessNames.Add(InName);
            InWidget.SetSelected(true);
        }
    }

    private async void btn_Save_Clicked(object sender, EventArgs e)
    {
        if (Category.Name == "")
            Category.Name = tbx_CategoryName.Text;
        if (Category.Name == "")
        {
            await Toast.Make("Give your category a name!").Show();
            return;
        }

        var s = State.Get();
        s.Categories.RemoveAll((c) => c.Name == Category.Name);
        s.Categories.Add(Category);
        s.Save();
        await Navigation.PopAsync();
    }

    private async void btn_Discard_Clicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}