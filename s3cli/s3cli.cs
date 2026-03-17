using System;
using System.IO;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using PowerArgs;

// ============================================================
// NOTE ON CREDENTIAL PERSISTENCE (-Pe flag)
// ============================================================
// The original .NET 4.7.2 project stored credentials in the
// Windows Registry (HKEY_CURRENT_USER\s3cli).
//
// This .NET 8 port retains Windows Registry support on Windows,
// and falls back to a JSON credentials file (~/.s3cli/credentials)
// on Linux/macOS, making the tool fully cross-platform.
// ============================================================

namespace s3cli
{
    public static class Launch
    {
        private const string CredentialsFileName = ".s3cli_credentials.json";

        public static bool ValidateCommand(s3CommandObject cobj)
        {
            bool valid = false;

            if (cobj.Action == S3Action.PUTFOLDER)
            {
                valid = Directory.Exists(cobj.FileLocal);
                if (!valid)
                    Console.WriteLine("Folder Not Found (" + cobj.Action + ") : " + cobj.FileLocal);
            }
            else if (cobj.Action == S3Action.PUTFILE)
            {
                valid = File.Exists(cobj.FileLocal);
                if (!valid)
                    Console.WriteLine("File Not Found (" + cobj.Action + ") : " + cobj.FileLocal);
            }
            else if (cobj.Action is S3Action.LIST or S3Action.CREATEBUCKET or S3Action.BUCKETSIZE or S3Action.DELETEBUCKET)
            {
                valid = true;
            }
            else if (cobj.Action is S3Action.GET or S3Action.DELETE)
            {
                valid = !string.IsNullOrEmpty(cobj.FileRemote);
                if (!valid)
                    Console.WriteLine("Remote file name is required (" + cobj.Action + ")");
            }

            if (cobj.Action is S3Action.CREATEBUCKET or S3Action.DELETE or S3Action.PUTFILE or
                S3Action.PUTFOLDER or S3Action.LIST or S3Action.GET or S3Action.DELETEBUCKET or S3Action.BUCKETSIZE)
            {
                if (string.IsNullOrEmpty(cobj.Bucket))
                {
                    Console.WriteLine("Bucket Name is required");
                    valid = false;
                }
            }

            return valid;
        }

        public static s3CommandObject CheckCreds(s3CommandObject o)
        {
            // Persist credentials if requested
            if (!string.IsNullOrEmpty(o.AccessKey) && o.PersistKeys)
                SaveCredential("AccessKey", o.AccessKey);
            if (!string.IsNullOrEmpty(o.Secret) && o.PersistKeys)
                SaveCredential("Secret", o.Secret);

            // Load from persistent store if not supplied on command line
            if (string.IsNullOrEmpty(o.AccessKey))
                o.AccessKey = LoadCredential("AccessKey")
                    ?? throw new Exception("Amazon access key not supplied. Pass -Ac or run once with -Pe to save credentials.");

            if (string.IsNullOrEmpty(o.Secret))
                o.Secret = LoadCredential("Secret")
                    ?? throw new Exception("Amazon secret not supplied. Pass -S or run once with -Pe to save credentials.");

            return o;
        }

        // ------------------------------------------------------------------
        // Credential persistence: Windows Registry on Windows,
        // JSON file on Linux/macOS
        // ------------------------------------------------------------------

        private static void SaveCredential(string key, string value)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SaveToRegistry("s3cli", key, value);
            }
            else
            {
                SaveToCredentialsFile(key, value);
            }
        }

        private static string? LoadCredential(string key)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try { return GetRegistryKey("s3cli", key); }
                catch { return null; }
            }
            else
            {
                return LoadFromCredentialsFile(key);
            }
        }

        // Windows Registry helpers (uses Microsoft.Win32.Registry NuGet package)
        private static string GetRegistryKey(string regkeyname, string regkey)
        {
            var subkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regkeyname);
            if (subkey != null)
            {
                var val = subkey.GetValue(regkey)?.ToString();
                subkey.Close();
                if (val != null) return val;
            }
            throw new Exception("Amazon credentials not found in registry");
        }

        private static void SaveToRegistry(string regkeyname, string regkey, string keyvalue)
        {
            var subkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regkeyname, true)
                      ?? Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regkeyname);
            subkey.SetValue(regkey, keyvalue);
            subkey.Close();
        }

        // Cross-platform credentials file (~/.s3cli_credentials.json)
        private static string CredentialsFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), CredentialsFileName);

        private static void SaveToCredentialsFile(string key, string value)
        {
            var creds = LoadCredentialsFile();
            creds[key] = value;
            File.WriteAllText(CredentialsFilePath, JsonConvert.SerializeObject(creds, Formatting.Indented));
            // Restrict permissions on non-Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try { File.SetUnixFileMode(CredentialsFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
                catch { /* ignore if not supported */ }
            }
        }

        private static string? LoadFromCredentialsFile(string key)
        {
            var creds = LoadCredentialsFile();
            return creds.TryGetValue(key, out var val) ? val : null;
        }

        private static Dictionary<string, string> LoadCredentialsFile()
        {
            if (!File.Exists(CredentialsFilePath))
                return new Dictionary<string, string>();
            try
            {
                var json = File.ReadAllText(CredentialsFilePath);
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            }
            catch { return new Dictionary<string, string>(); }
        }

        // ------------------------------------------------------------------

        public static void Start(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine(PowerArgsHelper.WriteArgs());
                return;
            }

            try
            {
                s3CommandObject cobj = PowerArgsHelper.GetArgs(args);

                if (cobj.Action == S3Action.USECONFIG)
                {
                    try
                    {
                        string file = File.ReadAllText(cobj.ConfigFile!);
                        cobj = JsonConvert.DeserializeObject<s3CommandObject>(file)!;
                    }
                    catch (Exception u)
                    {
                        Console.WriteLine("Error loading config file: " + u.Message);
                        return;
                    }
                }

                CheckCreds(cobj);

                if (!ValidateCommand(cobj))
                    return;

                S3Connector s3c = new S3Connector
                {
                    Action = cobj.Action,
                    AccessKey = cobj.AccessKey ?? "",
                    Secret = cobj.Secret ?? "",
                    Bucket = cobj.Bucket ?? "",
                    FileLocal = cobj.FileLocal ?? "",
                    FileRemote = cobj.FileRemote ?? "",
                    StorageOption = cobj.StorageOption,
                    DateStampFilename = cobj.DateStampFilename,
                    ProxyAddress = cobj.ProxyAddress ?? "",
                    ProxyPort = cobj.ProxyPort ?? "",
                    ZipThis = cobj.ZipThis,
                    ZipSpeed = cobj.ZipSpeed
                };

                switch (cobj.Action)
                {
                    case S3Action.CREATEBUCKET:
                        Console.WriteLine("\r" + s3commands.CreateBucket(s3c));
                        break;
                    case S3Action.BUCKETSIZE:
                        Console.WriteLine("\r" + s3commands.GetBucketSize(s3c));
                        break;
                    case S3Action.LIST:
                        Console.WriteLine("\r" + s3commands.GetObjectList(s3c));
                        break;
                    case S3Action.GET:
                        Console.WriteLine("\r" + s3commands.GetObject(s3c));
                        break;
                    case S3Action.DELETE:
                        Console.WriteLine("\r" + s3commands.DeleteObject(s3c));
                        break;
                    case S3Action.PUTFILE:
                        Console.WriteLine("\r" + s3commands.UploadFile(s3c));
                        break;
                    case S3Action.PUTFOLDER:
                        Console.WriteLine("\r" + s3commands.UploadDirectory(s3c));
                        break;
                    default:
                        Console.WriteLine("Unknown action: " + cobj.Action);
                        break;
                }
            }
            catch (ArgException)
            {
                Console.WriteLine(PowerArgsHelper.WriteArgs());
            }
            catch (Exception u)
            {
                Console.WriteLine(PowerArgsHelper.WriteArgs());
                Console.WriteLine(u.ToString());
            }
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            // .NET Core no longer needs the AppDomain.AssemblyResolve trick for
            // embedded DLLs — all dependencies are resolved via NuGet at build time
            // and can be published as a self-contained single-file executable using:
            //   dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
            Launch.Start(args);
        }
    }
}
