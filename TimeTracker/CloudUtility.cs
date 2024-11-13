
using Newtonsoft.Json;
using Synology;
using System.Diagnostics;

namespace TimeTracker
{
    public class CloudUtility
    {
        public static Client? GetClient()
        {
            return Client;
        }

        private static Client? Client = null;

        public static async Task<bool> CreateClient(Auth InAuth)
        {
            Client = new Client(InAuth.Address, InAuth.Port, TimeSpan.FromSeconds(30));
            if ((await Client.API.Connect()).success)
                if ((await Client.API.Login(InAuth.User, InAuth.Password, "TimeTracker")).success)
                    return true;
            Client = null;
            return false;
        }

        public class Auth
        {
            public string Address = "https://nas.synology.me";
            public int Port = 5001;
            public string User = "";
            public string Password = "";

            public bool IsValid()
            {
                return User != "" && Password != "" && Address != "";
            }
        }

        static string GetPath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "auth.json");
        }

        public static async Task<Auth?> LoadNewAuth()
        {
            try
            {
                string json = "";
                if (!File.Exists(GetPath()))
                    return null;
                json = await File.ReadAllTextAsync(GetPath());
                if (json == "")
                    return null;
                var obj = JsonConvert.DeserializeObject<Auth>(json);
                if (obj == null)
                    return new();
                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to load new auth");
                Trace.WriteLine(ex);
                return null;
            }
        }

        public static async Task<Auth> LoadAuth()
        {
            // Is there an auth file in working dir? 
            Auth? newAuth = JsonUtility.ReadJson<Auth>(GetPath());
            if (newAuth != null)
            {
                try
                {
                    SaveAuth(newAuth);
                    if (File.Exists(GetPath()))
                        File.Delete(GetPath());
                    Trace.WriteLine("Auth consumed");
                    return newAuth;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Failed to consume auth");
                    Trace.WriteLine(ex);
                }
            }

            string? json = await SecureStorage.Default.GetAsync("auth");
            if (json == null || json == "") return new();
            var obj = JsonConvert.DeserializeObject<Auth>(json);
            if (obj == null) return new();
            return obj;
        }

        public static void SaveAuth(Auth InAuth)
        {
            string json = JsonConvert.SerializeObject(InAuth, Formatting.Indented);
            SecureStorage.Default.SetAsync("auth", json);
        }
    }
}
