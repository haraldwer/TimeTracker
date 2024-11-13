using Microsoft.Maui.Platform;
using System.Globalization;

namespace TimeTracker;

public partial class CategoryWidget : ContentView
{
	Category Category;
	IDispatcherTimer? Timer; 

	public CategoryWidget(Category InCategory)
	{
		InitializeComponent();
		Category = InCategory;

        Loaded += CategoryWidget_Loaded;
        Unloaded += CategoryWidget_Unloaded;
	}

    private void CategoryWidget_Loaded(object? sender, EventArgs e)
    {
		lbl_Name.Text = Category.Name;
		CalculateTotal();
		CalculateWeeklyAvg();

        if (Timer != null && Timer.IsRunning)
            Timer.Stop();
        Timer = Application.Current.Dispatcher.CreateTimer();
        Timer.Interval = TimeSpan.FromSeconds(0.1);
        Timer.IsRepeating = true;
        Timer.Tick += Timer_Tick;
        Timer.Start();

		Tick(); 
    }

    private void CategoryWidget_Unloaded(object? sender, EventArgs e)
    {
        if (Timer != null && Timer.IsRunning)
            Timer.Stop();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
		Tick(); 
    }

    public void Tick()
    {
        if (Category.ActiveSession == null)
        {
            if (lbl_SessionTime.Text != " - ")
            {
                lbl_SessionTime.Text = " - ";
                CalculateTotal();
                CalculateWeeklyAvg();
            }
        }
        else
        {
            var upTime = DateTime.Now - Category.ActiveSession.Start;
            lbl_SessionTime.Text = State.GetTimeString(upTime);
        }
    }

    public void CalculateTotal()
	{
		TimeSpan total = new();
		foreach (var s in Category.Sessions)
			total += (s.End - s.Start);
        string str = State.GetTimeString(total);
        if (str != "")
            lbl_TotalTime.Text = "Total: " + str;
        else
            lbl_TotalTime.Text = "";
	}

    public void CalculateWeeklyAvg()
	{
        DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
        Calendar cal = dfi.Calendar;

        Dictionary<int, TimeSpan> weeks = new();
        foreach (var s in Category.Sessions)
		{
            int week = cal.GetWeekOfYear(s.Start, dfi.CalendarWeekRule, dfi.FirstDayOfWeek);
			int hash = s.Start.Year * 100 + week;
			if (!weeks.ContainsKey(hash))
				weeks.Add(hash, new());
			weeks[hash] += (s.End - s.Start);
		}

        if (weeks.Count > 0)
        {
		    TimeSpan total = new();
		    foreach (var w in weeks)
			    total += w.Value;
		    total /= weeks.Count; 
            lbl_WeeklyTime.Text = "Weekly AVG: " + State.GetTimeString(total);
        }
        else
        {
            lbl_WeeklyTime.Text = "";
        }
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new CategoryPage(Category));
    }

    private void PointerGestureRecognizer_PointerEntered(object sender, PointerEventArgs e)
    {
        this.ScaleTo(0.98, 100);
    }

    private void PointerGestureRecognizer_PointerExited(object sender, PointerEventArgs e)
    {
        this.ScaleTo(1.0, 100);
    }
}