using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using AutoMapper;
using Microsoft.Win32;
namespace s3cli
{
    public static class Launch
    {
        public static bool ValidateComand(s3CommandObject cobj)
        {
            bool valid = false;

            if (cobj.Action == S3Action.PUTFOLDER)
            {
                if (Directory.Exists(cobj.FileLocal))
                {
                    valid = true;
                }
                else
                {
                    Console.WriteLine("Folder Not Found (" + cobj.Action + ") : " + cobj.FileLocal);
                    valid = false;
                }
            }
            else if (cobj.Action == S3Action.PUTFILE)
            {
                if (File.Exists(cobj.FileLocal))
                {
                    valid = true;
                }
                else
                {
                    Console.WriteLine("File Not Found (" + cobj.Action + ") : " + cobj.FileLocal);
                    valid = false;
                }
            }
            else if (cobj.Action == S3Action.LIST) 
            {
                valid = true;
            }
            else if (cobj.Action == S3Action.CREATEBUCKET)
            {
                valid = true;
            }
            else if (cobj.Action == S3Action.BUCKETSIZE)
            {
                valid = true;
            }
            else if (cobj.Action == S3Action.GET) {
                if (!String.IsNullOrEmpty(cobj.FileRemote))
                {
                    valid = true;
                }
                else
                {
                    Console.WriteLine("Remote file name is required (" + cobj.Action + ") : " + cobj.FileRemote);
                    valid = false;
                }                
            }
            else if (cobj.Action == S3Action.DELETE) 
            {
                if (!String.IsNullOrEmpty(cobj.FileRemote))
                {
                    valid = true;
                }
                else
                {
                    Console.WriteLine("Remote file name is required (" + cobj.Action + ") : " + cobj.FileRemote);
                    valid = false;
                }    
            }
            else
            {
                
                valid = false;
            }

            if (
                cobj.Action == S3Action.CREATEBUCKET || cobj.Action == S3Action.DELETE || cobj.Action == S3Action.PUTFILE ||
                cobj.Action == S3Action.PUTFOLDER || cobj.Action == S3Action.LIST || cobj.Action == S3Action.GET || cobj.Action == S3Action.DELETEBUCKET || cobj.Action == S3Action.BUCKETSIZE)
            {

                if (String.IsNullOrEmpty(cobj.Bucket))
                {
                    Console.WriteLine("Bucket Name is required");
                    valid = false;
                }
            }


            return valid;
        }

        public static s3CommandObject CheckCreds(s3CommandObject o)
        {
            var keyfound = false;
            var secretfound = false;

            if (!String.IsNullOrEmpty(o.AccessKey) && o.PersistKeys)
            {
                SaveToRegistry("s3cli", "AccessKey", o.AccessKey);
                secretfound = true;
            }
            if (!String.IsNullOrEmpty(o.Secret) && o.PersistKeys)
            {
                SaveToRegistry("s3cli", "Secret", o.Secret);
                keyfound = true;
            }

            if (String.IsNullOrEmpty(o.AccessKey))
            {
                try
                {
                    o.AccessKey = GetKey("s3cli", "AccessKey");
                    keyfound = true;
                }
                catch { keyfound = false; }
            }
            if (String.IsNullOrEmpty(o.Secret))
            {
                try
                {
                    o.Secret = GetKey("s3cli", "Secret");
                    secretfound = true;
                }
                catch { secretfound = false; }
            }

            if (!secretfound) { throw new Exception("Amazon secret not supplied"); }
            if (!keyfound) { throw new Exception("Amazon access key not supplied"); }

            return o;
        }

        public static string GetKey (string regkeyname, string regkey) 
        {
            var res = "";
            var subkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regkeyname);
            if (subkey != null)
            {
                res = subkey.GetValue(regkey).ToString();
                subkey.Close();
            }
            else
            {
                throw new Exception("Amazon credentials not found");
            }
            return res;
        }

        public static void SaveToRegistry(string regkeyname, string regkey, string keyvalue)
        {
            var subkey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(regkeyname, true);
            if (subkey == null)
            {
                Microsoft.Win32.RegistryKey exampleRegistryKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(regkeyname);
                exampleRegistryKey.SetValue(regkey, keyvalue);
                exampleRegistryKey.Close();
            }
            else
            {
                subkey.SetValue(regkey, keyvalue);
                subkey.Close();
            }
        }

        public static void Start(string[] args)
        {

            if (args != null)
            {
                try
                {
                    s3CommandObject cobj = PowerArgsHelper.GetArgs(args);

                    if (cobj.Action == S3Action.USECONFIG)
                    {
                        try
                        {
                            string file = File.ReadAllText(cobj.ConfigFile);
                            cobj = JsonConvert.DeserializeObject<s3CommandObject>(file);
                        }
                        catch (Exception u) {
                            //Console.WriteLine(u.ToString());
                        }
                    }

                    CheckCreds(cobj);

                    if (ValidateComand(cobj))
                    {

                        Mapper.Initialize(cfg => cfg.CreateMap<s3CommandObject, S3Connector>());
                        S3Connector s3c = Mapper.Map<S3Connector>(cobj);
                        
                        if (cobj.Action == S3Action.CREATEBUCKET)
                        {
                            if (!String.IsNullOrEmpty(cobj.Bucket))
                            {
                                Console.WriteLine("\r" + s3commands.CreateBucket(s3c));
                            }
                        }
                        if (cobj.Action == S3Action.BUCKETSIZE)
                        {
                            if (!String.IsNullOrEmpty(cobj.Bucket))
                            {
                                Console.WriteLine("\r" + s3commands.GetBucketSize(s3c));
                            }
                        }
                        if (cobj.Action == S3Action.LIST)
                        {
                            if (!String.IsNullOrEmpty(cobj.Bucket))
                            {
                                Console.WriteLine("\r" + s3commands.GetObjectList(s3c));
                            }
                        }
                        if (cobj.Action == S3Action.GET)
                        {
                            if (!String.IsNullOrEmpty(cobj.Bucket))
                            {
                                Console.WriteLine("\r" + s3commands.GetObject(s3c));
                            }
                        }
                        if (cobj.Action == S3Action.DELETE)
                        {
                            if (!String.IsNullOrEmpty(cobj.FileRemote))
                            {
                                Console.WriteLine("\r" + s3commands.DeleteObject(s3c));
                            }
                        }
                        if (cobj.Action == S3Action.PUTFILE)
                        {
                            if (!String.IsNullOrEmpty(cobj.FileLocal))
                            {
                                Console.WriteLine("\r" + s3commands.UploadFile(s3c));
                            }
                        }
                        if (cobj.Action == S3Action.PUTFOLDER)
                        {
                            if (!String.IsNullOrEmpty(cobj.FileLocal))
                            {
                                Console.WriteLine("\r" + s3commands.UploadDirectory(s3c));
                            }
                        }
                    }
                    else
                    {

                    }
                }
                catch (PowerArgs.ArgException ex)
                {
                    Console.WriteLine(PowerArgsHelper.WriteArgs());
                }
                catch (Exception u)
                {
                    Console.WriteLine(PowerArgsHelper.WriteArgs());
                    Console.WriteLine(u.ToString());

                }

            }
            else
            {

                Console.WriteLine(PowerArgsHelper.WriteArgs());
            }
        }

    }

    public class s3cli
    {
        static byte[] StreamToBytes(Stream input)
        {
            var capacity = input.CanSeek ? (int)input.Length : 0;
            using (var output = new MemoryStream(capacity))
            {
                int readLength;
                var buffer = new byte[4096];

                do
                {
                    readLength = input.Read(buffer, 0, buffer.Length);
                    output.Write(buffer, 0, readLength);
                }
                while (readLength != 0);

                return output.ToArray();
            }
        }

        public static Assembly getAssem(string name)
        {
            using (var input = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                return input != null
                     ? Assembly.Load(StreamToBytes(input))
                     : null;
            }
        }

        static void Main(string[] args)
        {
            
            Assembly a = Assembly.GetExecutingAssembly();
            //Console.WriteLine( String.Join(",",a.GetManifestResourceNames()));
            
            //Loads all assemblys included as embedded resources, so we can deploy a single executable without the need for lots of DLL's
            AppDomain.CurrentDomain.AssemblyResolve += (sender, argsx) =>
            {
                var resName = argsx.Name.Split(new char[] { ',' })[0] + ".dll";
                //Console.WriteLine("Missing aseembly:" + "s3cli.RES."+resName);
                var thisAssembly = Assembly.GetExecutingAssembly();
                using (var input = thisAssembly.GetManifestResourceStream("s3cli.RES." + resName))
                {
                    return input != null
                         ? Assembly.Load(StreamToBytes(input))
                         : null;
                }
            };

            Launch.Start(args);

        }

    }
}

