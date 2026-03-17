using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System.IO.Compression;

public class CommandResponse
{
    public DateTime DateOfResponse { get; set; }
    public bool? Success { get; set; }
    public string? ResponseMessage { get; set; } = "";
}

public static class MyExtensions
{
    public static string AppendTimeStamp(this string fileName)
    {
        return string.Concat(
            Path.GetFileNameWithoutExtension(fileName), "_",
            DateTime.Now.ToString("yyyyMMdd-HHmmssfff"),
            Path.GetExtension(fileName)
        );
    }
}

public static class s3commands
{
    public static string GetObjectList(S3Connector commandobject)
    {
        var objects = new List<string>();

        // AWS SDK v3: AmazonS3Client constructor with credentials
        var config = BuildS3Config(commandobject);
        using var client = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret, config);

        var r = new ListObjectsRequest { BucketName = commandobject.Bucket, MaxKeys = 100 };

        // SDK v3 uses async methods; call .GetAwaiter().GetResult() to keep synchronous interface
        ListObjectsResponse response = client.ListObjectsAsync(r).GetAwaiter().GetResult();

        foreach (S3Object entry in response.S3Objects)
        {
            objects.Add(string.Format("key = {0}, size = {1}, storage = {2}", entry.Key, entry.Size, entry.StorageClass));
        }

        return string.Join("\n", objects);
    }

    public static string DeleteObject(S3Connector commandobject)
    {
        var config = BuildS3Config(commandobject);
        using var client = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret, config);

        var deleteObjectRequest = new DeleteObjectRequest
        {
            BucketName = commandobject.Bucket,
            Key = commandobject.FileRemote
        };

        try
        {
            // SDK v3: DeleteObjectAsync
            DeleteObjectResponse d = client.DeleteObjectAsync(deleteObjectRequest).GetAwaiter().GetResult();
            return d != null ? d.HttpStatusCode.ToString() : "Object not found";
        }
        catch
        {
            return "Object not found";
        }
    }

    public static string GetBucketSize(S3Connector commandObject)
    {
        long filesize = 0;
        long? filecount = 0;

        var config = BuildS3Config(commandObject);
        using var client = new AmazonS3Client(commandObject.AccessKey, commandObject.Secret, config);

        var request = new ListObjectsRequest { BucketName = commandObject.Bucket };

        do
        {
            ListObjectsResponse response = client.ListObjectsAsync(request).GetAwaiter().GetResult();

            foreach (var s3obj in response.S3Objects)
            {
                filesize += (long)s3obj.Size;
                filecount++;
            }

            if ((bool)response.IsTruncated)
            {
                request.Marker = response.NextMarker;
            }
            else
            {
                request = null!;
            }
        }
        while (request != null);

        return FormatBytes(filesize) + " [" + filecount + " files]";
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dblSByte = bytes;
        if (bytes > 1024)
            for (i = 0; (bytes / 1024) > 0; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;
        return string.Format("{0:0.##}{1}", dblSByte, suffix[i]);
    }

    public static string CreateBucket(S3Connector commandObject)
    {
        var config = BuildS3Config(commandObject);
        using var client = new AmazonS3Client(commandObject.AccessKey, commandObject.Secret, config);

        var request = new PutBucketRequest { BucketName = commandObject.Bucket };

        try
        {
            client.PutBucketAsync(request).GetAwaiter().GetResult();
            return "Bucket " + commandObject.Bucket + " created";
        }
        catch (AmazonS3Exception s)
        {
            return s.Message;
        }
    }

    public static string GetObject(S3Connector commandobject)
    {
        var config = BuildS3Config(commandobject);
        using var client = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret, config);

        var request = new GetObjectRequest
        {
            BucketName = commandobject.Bucket,
            Key = commandobject.FileRemote
        };

        using GetObjectResponse response = client.GetObjectAsync(request).GetAwaiter().GetResult();

        string dest = Path.Combine(commandobject.FileLocal, commandobject.FileRemote);

        if (!File.Exists(dest))
        {
            response.WriteResponseStreamToFileAsync(dest, false, default).GetAwaiter().GetResult();
        }

        return "";
    }

    public static string UploadDirectory(S3Connector commandobject)
    {
        if (commandobject.ZipThis)
        {
            // Zip the folder and upload as a single file
            UploadFile(commandobject);
            return "";
        }

        var config = BuildS3Config(commandobject);
        using var client = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret, config);
        var fileTransferUtility = new TransferUtility(client);

        var keyprefix = commandobject.FileRemote;
        if (commandobject.DateStampFilename)
        {
            keyprefix = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
        }

        var dirrequest = new TransferUtilityUploadDirectoryRequest
        {
            BucketName = commandobject.Bucket,
            KeyPrefix = keyprefix,
            Directory = commandobject.FileLocal,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            StorageClass = ResolveStorageClass(commandobject.StorageOption)
        };

        dirrequest.UploadDirectoryProgressEvent += DirectoryTransferUtilityRequest_UploadProgressEvent;

        fileTransferUtility.UploadDirectoryAsync(dirrequest).GetAwaiter().GetResult();

        return "Upload completed";
    }

    public static string UploadFile(S3Connector commandobject)
    {
        Console.WriteLine("Uploading " + commandobject.FileLocal);

        FileInfo? f = null;

        if (commandobject.ZipThis)
        {
            string tempPath = Path.GetTempPath();


                string zipfilename;
                if (commandobject.Action == S3Action.PUTFOLDER)
                {
                    zipfilename = commandobject.FileLocal.Replace(":", "").Replace("\\", "_").Replace("/", "_");
                }
                else
                {
                    f = new FileInfo(commandobject.FileLocal);
                    zipfilename = Path.GetFileNameWithoutExtension(f.Name);
                }

                var compressionLevel = commandobject.ZipSpeed switch
                {
                    ZipSpeedOptions.FAST => CompressionLevel.Fastest,
                    ZipSpeedOptions.SLOW => CompressionLevel.SmallestSize,
                    _ => CompressionLevel.Optimal
                };

                // Single file
                ZipFile.CreateFromDirectory(
                    Path.GetDirectoryName(commandobject.FileLocal)!,
                    zipfilename,
                    compressionLevel,
                    includeBaseDirectory: false
                );

            /*

            if (commandobject.Action == S3Action.PUTFILE)
                zip.AddFile(commandobject.FileLocal);
            if (commandobject.Action == S3Action.PUTFOLDER)
                zip.AddDirectory(commandobject.FileLocal);

            var tempZipFile = Path.Combine(tempPath, zipfilename + ".zip");
            Console.WriteLine("Please wait... zipping files");
            zip.Save(tempZipFile);
            f = new FileInfo(tempZipFile);
            */

        }
        else
        {
            f = new FileInfo(commandobject.FileLocal);
        }

        var s3Config = BuildS3Config(commandobject);
        var cli = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret, s3Config);
        var fileTransferUtility = new TransferUtility(cli);

        var filename = f.Name;
        if (commandobject.DateStampFilename)
        {
            filename = filename.AppendTimeStamp();
        }
        Console.WriteLine("Uploading.. " + f.FullName + " as " + filename);

        var fileTransferUtilityRequest = new TransferUtilityUploadRequest
        {
            BucketName = commandobject.Bucket,
            Key = filename,
            FilePath = f.FullName,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            StorageClass = ResolveStorageClass(commandobject.StorageOption),
            PartSize = (10 * 1024 * 1024)
        };

        fileTransferUtilityRequest.UploadProgressEvent += FileTransferUtilityRequest_UploadProgressEvent;

        fileTransferUtility.UploadAsync(fileTransferUtilityRequest).GetAwaiter().GetResult();

        if (File.Exists(f.FullName) && commandobject.ZipThis)
        {
            File.Delete(f.FullName);
        }

        return "Upload completed";
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static AmazonS3Config BuildS3Config(S3Connector commandobject)
    {
        var config = new AmazonS3Config();

        if (!string.IsNullOrEmpty(commandobject.ProxyAddress))
        {
            config.ProxyHost = commandobject.ProxyAddress;
            config.ProxyCredentials = CredentialCache.DefaultCredentials;
        }
        if (!string.IsNullOrEmpty(commandobject.ProxyPort) &&
            int.TryParse(commandobject.ProxyPort, out int port))
        {
            config.ProxyPort = port;
        }

        return config;
    }

    private static S3StorageClass ResolveStorageClass(StorageOptions option) => option switch
    {
        StorageOptions.REDUCEDREDUNDANCY => S3StorageClass.ReducedRedundancy,
        StorageOptions.STANDARDIA        => S3StorageClass.StandardInfrequentAccess,
        StorageOptions.GLACIER           => S3StorageClass.Glacier,
        _                                => S3StorageClass.Standard
    };

    private static void FileTransferUtilityRequest_UploadProgressEvent(object? c, UploadProgressArgs e)
    {
        Console.Write("\r{0}/{1}", e.TransferredBytes, e.TotalBytes);
    }

    private static void DirectoryTransferUtilityRequest_UploadProgressEvent(object? c, UploadDirectoryProgressArgs e)
    {
        Console.Write("\r{0}/{1}", e.CurrentFile, e.TotalNumberOfFiles);
    }
}
