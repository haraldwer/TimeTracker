using Microsoft.Maui.Platform;
using System.Diagnostics;
namespace TimeTracker;

public partial class ProcessWidget : ContentView
{
    Process? Process;
    IDispatcherTimer? Timer;
    Action<ProcessWidget, Process, string> OnClicked;

    public ProcessWidget(string InProcess, Action<ProcessWidget, Process, string> InOnClicked)
    {
        InitializeComponent();
        OnClicked += InOnClicked;
        lbl_Name.Text = InProcess;
    }

    public void SetProcess(Process InProcess)
    {
        Process = InProcess;

        lbl_Name.Text = Process.ProcessName;
        if (Timer != null && Timer.IsRunning)
            Timer.Stop();
        Timer = Application.Current.Dispatcher.CreateTimer();
        Timer.Interval = TimeSpan.FromSeconds(0.1);
        Timer.IsRepeating = true;
        Timer.Tick += Timer_Tick;
        Timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateTime();
    }

    void UpdateTime()
    {
        if (Process == null)
            return; 
        try
        {
            var upTime = DateTime.Now - Process.StartTime;
            lbl_Uptime.Text = State.GetTimeString(upTime);
        }
        catch (Exception ex)
        {

        }
    }

    public void SetSelected(bool InSelected)
    {
        brd_Base.BackgroundColor = InSelected ?
            Color.FromRgb(96, 85, 131) : Colors.Black;
    }

    private void Button_Clicked(object sender, EventArgs e)
    {
        OnClicked(this, Process, lbl_Name.Text);
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