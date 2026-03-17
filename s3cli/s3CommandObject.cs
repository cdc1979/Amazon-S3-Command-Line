using PowerArgs;

public class S3Connector
{
    public S3Action Action { get; set; }
    public string AccessKey { get; set; } = "";
    public string Secret { get; set; } = "";
    public string Bucket { get; set; } = "";
    public string FileLocal { get; set; } = "";
    public string FileRemote { get; set; } = "";
    public StorageOptions StorageOption { get; set; }
    public bool DateStampFilename { get; set; }
    public string ProxyAddress { get; set; } = "";
    public string ProxyPort { get; set; } = "";
    public bool ZipThis { get; set; }
    public ZipSpeedOptions ZipSpeed { get; set; }
}

public enum ZipSpeedOptions { SLOW, DEFAULT, FAST }
public enum StorageOptions { STANDARD, STANDARDIA, REDUCEDREDUNDANCY, GLACIER }

public class s3CommandObject
{
    [ArgRequired]
    [ArgShortcut("-A")]
    [ArgDescription("Available action types: PUTFILE,PUTFOLDER,GET,DELETE,DELETEBUCKET,CREATEBUCKET,LIST,USECONFIG,BUCKETSIZE")]
    public S3Action Action { get; set; }

    [ArgShortcut("-Ac")]
    [ArgDescription("Your Amazon Access Key")]
    public string? AccessKey { get; set; }

    [ArgShortcut("-S")]
    [ArgDescription("Your Amazon Secret Key")]
    public string? Secret { get; set; }

    [ArgShortcut("-B")]
    [ArgDescription("Your Amazon S3 Bucket Name")]
    public string? Bucket { get; set; }

    [ArgShortcut("-F")]
    [ArgDescription("Local File or Folder")]
    public string? FileLocal { get; set; }

    [ArgShortcut("-Fi")]
    [ArgDescription("Remote Object Name (The file or folder name stored in S3)")]
    public string? FileRemote { get; set; }

    [ArgShortcut("-St")]
    [ArgDescription("Set Storage Type: STANDARD, STANDARDIA, REDUCEDREDUNDANCY, GLACIER")]
    public StorageOptions StorageOption { get; set; }

    [ArgShortcut("-C")]
    [ArgDescription("Load settings from a config file")]
    public string? ConfigFile { get; set; }

    [ArgShortcut("-D")]
    [ArgDescription("Automatically adjust filename to include current date and time")]
    public bool DateStampFilename { get; set; }

    [ArgShortcut("-P")]
    [ArgDescription("Proxy server address e.g. http://myproxy")]
    public string? ProxyAddress { get; set; }

    [ArgShortcut("-Pr")]
    [ArgDescription("Proxy server port e.g. 8080")]
    public string? ProxyPort { get; set; }

    [ArgShortcut("-Z")]
    [ArgDescription("Zip files before uploading")]
    public bool ZipThis { get; set; }

    [ArgShortcut("-Zi")]
    [ArgDescription("Zip compression level: SLOW (best compression), DEFAULT, FAST (best speed)")]
    public ZipSpeedOptions ZipSpeed { get; set; }

    [ArgShortcut("-Pe")]
    [ArgDescription("Remember Amazon Credentials for future use (stored in user environment variables)")]
    public bool PersistKeys { get; set; }
}

public enum S3Action { USECONFIG, PUTFILE, PUTFOLDER, GET, DELETE, DELETEBUCKET, CREATEBUCKET, LIST, BUCKETSIZE }
