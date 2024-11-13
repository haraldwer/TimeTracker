
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using Synology.Parameters;

namespace TimeTracker
{
    public class JsonUtility
    {
        public static T? ReadJson<T>(string InPath)
        {
            try
            {
                if (!File.Exists(InPath))
                    return default;
                using Stream fileStream = File.OpenRead(InPath);
                if (fileStream.Length == 0)
                    return default;
                using StreamReader reader = new StreamReader(fileStream);
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex) { Trace.WriteLine(ex.Message); }
            return default;
        }

        public static bool WriteJson<T>(T InObj, string InFile)
        {
            try
            {
                FileInfo f = new FileInfo(InFile);
                if (f.Directory == null)
                    return false;
                if (!f.Directory.Exists)
                    Directory.CreateDirectory(f.Directory.FullName);
                string json = JsonConvert.SerializeObject(InObj, Formatting.Indented);
                File.WriteAllText(InFile, json);
                return true;
            }
            catch (Exception ex) { Trace.WriteLine(ex.Message); }
            return false;
        }

        public static async Task<T?> DownloadJson<T>(string InPath)
        {
            try
            {
                var c = CloudUtility.GetClient();
                if (c == null)
                    return default;
                Trace.WriteLine("Downloading file: " + InPath);
                var response = await c.FileStation.Download(InPath);
                if (!response.success || response.data == null)
                    return default;
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(response.data));
            }
            catch (Exception ex) { Trace.WriteLine(ex.Message); }
            return default;
        }

        public static async Task<bool> UploadJson<T>(T InObj, string InPath)
        {
            try
            {
                var c = CloudUtility.GetClient();
                if (c == null) 
                    return false;

                InPath.Replace("\\", "/");
                int split = InPath.LastIndexOf("/");
                string dir = InPath.Substring(0, split);
                string name = InPath.Substring(split + 1);

                string json = JsonConvert.SerializeObject(InObj, Formatting.Indented);
                byte[] data = Encoding.UTF8.GetBytes(json);
                Trace.WriteLine("Uploading file: " + name + " to " + dir);
                var response = await c.FileStation.Upload(dir, name, data, UploadFileExistBehaviorParameter.overwrite, true);
                return response.success;
            }
            catch (Exception ex) { Trace.WriteLine(ex.Message); }
            return false;
        }
    }
}
