using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public class S3Connector
    {
        public S3Action Action { get; set; } // e.g. put/get
        public string AccessKey { get; set; }
        public string Secret { get; set; }
        public string Bucket { get; set; }
        public string FileLocal { get; set; }
        public string FileRemote { get; set; }
        public StorageOptions StorageOption { get; set; }
        public bool DateStampFilename { get; set; }
        public string ProxyAddress { get; set; }
        public string ProxyPort { get; set; }
        public string AlertScaleApiKey { get; set; }
        // zip functions
        public bool ZipThis { get; set; } // does the file/folder get zipped before sending
        public ZipSpeedOptions ZipSpeed { get; set; } // which compression level to use
    }

    public enum ZipSpeedOptions { SLOW,DEFAULT,FAST }
    public enum StorageOptions { STANDARD,STANDARDIA,REDUCEDREDUNDANCY,GLACIER }



