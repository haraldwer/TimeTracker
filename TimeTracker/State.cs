
using Microsoft.Maui.Platform;
using Newtonsoft.Json;
using System.Diagnostics;

namespace TimeTracker
{
    public class Session
    {
        public DateTime Start;
        public DateTime End;
    }

    public class Category
    {
        public string Name = "";
        public List<Session> Sessions = new();
        public List<string> ProcessNames = new();
        public Session? ActiveSession = null;
    }

    public class State
    {
        private static State? Instance = null;

        public static State Get()
        {
            Instance ??= Load();
            Instance ??= new State();
            return Instance;
        }

        static string GetPath()
        {
            return Path.Combine(FileSystem.CacheDirectory, "state.json");
        }

        public static State Load()
        {
            try
            {
                string? json = SecureStorage.Default.GetAsync("state").Result;
                if (json == null)
                    if (File.Exists(GetPath()))
                        json = File.ReadAllText(GetPath());
                if (json == null) return new();
                var obj = JsonConvert.DeserializeObject<State>(json);
                if (obj == null) return new();
                obj.EndAllSessions();
                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
                return new();
            }
        }

        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                SecureStorage.Default.SetAsync("state", json).Wait(1000);
                if (!Directory.Exists(GetPath()))
                    Directory.CreateDirectory(GetPath());
                File.WriteAllText(GetPath(), json);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.ToString());
            }
        }

        public List<Category> Categories = new();

        public void Update()
        {
            Task.Run(() =>
            {
                // Get processes
                Dictionary<string, Process> map = new();
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        if (string.IsNullOrEmpty(p.MainWindowTitle) ||
                            p.BasePriority < 8 ||
                            p.HasExited)
                            continue;
                        var t = p.StartTime; // Try access time
                    }
                    catch
                    {
                        continue;
                    }
                    if (map.ContainsKey(p.ProcessName))
                        continue;
                    map.Add(p.ProcessName, p);
                }
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var c in Categories)
                    {
                        bool isActive = false;
                        DateTime start = DateTime.MinValue;
                        foreach (var p in c.ProcessNames)
                        {
                            if (map.ContainsKey(p))
                            {
                                isActive = true;
                                start = DateTime.Now;
                                if (map[p].StartTime < start)
                                    start = map[p].StartTime;
                            }
                        }

                        if (isActive)
                        {
                            foreach (var s in c.Sessions)
                            {
                                if (start < s.End)
                                {
                                    // Use this session instead
                                    c.ActiveSession = s;
                                    c.Sessions.Remove(s);
                                    break;
                                }
                            }

                            c.ActiveSession ??= new Session() {
                                Start = start
                            };
                            c.ActiveSession.End = DateTime.Now;
                        }
                        else 
                        {
                            if (c.ActiveSession != null)
                            {
                                // End active session
                                c.Sessions.Add(c.ActiveSession);
                                c.ActiveSession = null;
                            }
                        }
                    }
                });
            });
        }

        public void EndAllSessions()
        {
            foreach (var c in Categories)
            {
                if (c.ActiveSession != null)
                {
                    c.ActiveSession.End = DateTime.Now;
                    c.Sessions.Add(c.ActiveSession);
                    c.ActiveSession = null;
                }
            }
        }

        public static string GetTimeString(TimeSpan InSpan)
        {
            if (InSpan.Hours > 0)
                return InSpan.ToFormattedString("hh:mm");
            if (InSpan.Minutes > 0)
                return "00" + InSpan.ToFormattedString(":mm");
            if (InSpan.Seconds > 0)
                return InSpan.Seconds + " sec";
            return "";
        }
    }
}
