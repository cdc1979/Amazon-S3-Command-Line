using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PowerArgs;

    public static class PowerArgsHelper
    {
        public static s3CommandObject GetArgs(string[] args)
        {
            try
            {
                return Args.Parse<s3CommandObject>(args);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GetStyledUsage<s3CommandObject>());
                return new s3CommandObject();
            }
        }

        public static ConsoleString WriteArgs()
        {
            return ArgUsage.GetStyledUsage<s3CommandObject>();
        }
    }

    public class s3CommandObject
    {
        [ArgRequired]
        [ArgDescription("Available action types: PUTFILE,PUTFOLDER,GET,DELETE,DELETEBUCKET,CREATEBUCKET,LIST,USECONFIG")]
        public S3Action Action { get; set; }
        [ArgDescription("Your Amazon Access Key")]
        public string AccessKey { get; set; }
        [ArgDescription("Your Amazon Secret Key")]
        public string Secret { get; set; }
        [ArgDescription("Your Amazon S3 Bucket Name")]
        public string Bucket { get; set; }
        [ArgDescription("Local File or Folder")]
        public string FileLocal { get; set; }
        [ArgDescription("Remote Object Name (The file or folder name stored in S3)")]
        public string FileRemote { get; set; }
        [ArgDescription("Use Reduced Redundancy Storage")]
        public bool ReducedRedundancy { get; set; }
        [ArgDescription("Load settings from a config file")]
        public string ConfigFile { get; set; }
        [ArgDescription("Automatically adjust filename to include current date and time")]
        public bool DateStampFilename { get; set; }
        [ArgDescription("Proxy server address e.g. http://myproxy")]
        public string ProxyAddress { get; set; }
        [ArgDescription("Proxy server port e.g. 8080")]
        public string ProxyPort { get; set; }
        [ArgDescription("AlertScale API Key (For success/fail notifications)")]
        public string AlertScaleApiKey { get; set; }
        [ArgDescription("Zip files before uploading")]
        public bool ZipThis { get; set; } // does the file/folder get zipped before sending
        [ArgDescription("Zip compression level Best Compression/Normal/Least Compression")]
        public ZipSpeedOptions ZipSpeed { get; set; } // which compression level to use
        [ArgDescription("Remember Amazon Credentials in Registry")] // not yet functional
        public bool PersistKeys { get; set; }
    }
    public enum S3Action { USECONFIG,PUTFILE, PUTFOLDER, GET, DELETE, DELETEBUCKET,CREATEBUCKET,LIST }
