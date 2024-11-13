
using CommunityToolkit.Maui.Alerts;
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
        public List<Category> Categories = new();

        private static State? Instance = null;

        public static State? Get()
        {
            return Instance;
        }

        static string GetFilePath() => Path.Combine(FileSystem.CacheDirectory, "TrackedTime.json");
        static string GetCloudPath() => "/Harald/Dev/TrackedTime.json";

        public static async Task<bool> Load()
        {
            State? file = JsonUtility.ReadJson<State>(GetFilePath());
            State? cloud = await JsonUtility.DownloadJson<State>(GetCloudPath());
            if (cloud != null)
                Toast.Make("Cloud data downloaded successfully!");
            file ??= new();
            cloud ??= new();
            file.EndAllSessions();
            cloud.EndAllSessions();
            Instance = Merge(file, cloud);
            await Instance.Save();
            return true;
        }

        public async Task Save()
        {
            if (!JsonUtility.WriteJson(this, GetFilePath()))
                Trace.WriteLine("File save failed");
            if (!await JsonUtility.UploadJson(this, GetCloudPath()))
                Trace.WriteLine("Cloud save failed");
        }

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
                    // Should be thread safe!
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
                    c.Sessions = MergeSessions(c.Sessions);
                }
            }
        }

        public static string GetTimeString(TimeSpan InSpan)
        {
            if (InSpan.TotalHours >= 10)
                return Convert.ToInt32(InSpan.TotalHours) + "h";
            if (InSpan.TotalMinutes > 0)
            {
                string min = Convert.ToString(InSpan.Minutes);
                if (InSpan.Minutes < 10)
                    min = "0" + min;
                return Convert.ToInt32(InSpan.TotalHours) + ":" + min;
            }
            if (InSpan.Seconds > 0)
                return Convert.ToInt32(InSpan.TotalSeconds) + " s";
            return "";
        }

        public static State Merge(State InA, State InB)
        {
            foreach (var cB in InB.Categories)
            {
                var matchesA = InA.Categories.Where((cA) => cA.Name == cB.Name);
                foreach (var cA in matchesA)
                {
                    cA.ProcessNames = cA.ProcessNames.Union(cB.ProcessNames).ToList();
                    cA.Sessions.AddRange(cB.Sessions);
                }

                if (matchesA.Count() == 0)
                    InA.Categories.Add(cB);
            }

            foreach (var cA in InA.Categories)
                cA.Sessions = MergeSessions(cA.Sessions);

            return InA;
        }

        private static List<Session> MergeSessions(List<Session> InSessions)
        {
            InSessions.Sort((first, second) => first.Start.CompareTo(second.Start));

            bool cond = true;
            while (cond)
            {
                cond = false; 
                for (int i = 0; i < InSessions.Count - 1; i++)
                {
                    Session session = InSessions[i];
                    Session nextSession = InSessions[i + 1];

                    if (IsOverlap(session, nextSession))
                    {
                        // Replace A with merge
                        InSessions[i] = MergeSessions(session, nextSession);
                        // Consume B
                        InSessions.RemoveAt(i + 1);
                        cond = true;
                    }
                }
            }
            return InSessions;
        }

        private static bool IsOverlap(Session InFirst, Session InSecond)
        {
            DateTime maxStart = InFirst.Start > InSecond.Start ? InFirst.Start : InSecond.Start;
            DateTime minEnd = InFirst.End < InSecond.End ? InFirst.End : InSecond.End;
            return minEnd >= maxStart;
        }

        private static Session MergeSessions(Session InFirst, Session InSecond)
        {
            //Trace.WriteLine("Merging sessions: ");
            //Trace.WriteLine(InFirst.Start.ToString() + " - " + InFirst.End.ToString());
            //Trace.WriteLine(InSecond.Start.ToString() + " - " + InSecond.End.ToString());
            return new() { 
                Start = InFirst.Start < InSecond.Start ? InFirst.Start : InSecond.Start, 
                End = InFirst.End > InSecond.End ? InFirst.End : InSecond.End
            };
        }
    }
}
