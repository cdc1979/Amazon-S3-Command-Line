using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Ionic.Zip;

public class CommandResponse
{
    public DateTime DateOfResponse { get; set; }
    public bool Success { get; set; }
    public string ResponseMessage { get; set; }
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
        List<string> objects = new List<string>();
        using (var client = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret))
        {
            ListObjectsRequest r = new ListObjectsRequest() { BucketName = commandobject.Bucket, MaxKeys = 100 };
            ListObjectsResponse response = client.ListObjects(r);

            // Process response.
            foreach (S3Object entry in response.S3Objects)
            {
                var s = String.Format("key = {0}, size = {1}, storage = {2}", entry.Key, entry.Size, entry.StorageClass);
                objects.Add(s);
            }
        }
        return String.Join("\n", objects);
    }

    public static string DeleteObject(S3Connector commandobject)
    {
        var result = "";
        DeleteObjectRequest deleteObjectRequest =
            new DeleteObjectRequest
            {
                BucketName = commandobject.Bucket,
                Key = commandobject.FileRemote
            };
        using (var client = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret))
        {
            try
            {
                DeleteObjectResponse d = client.DeleteObject(deleteObjectRequest);
                if (d != null)
                {
                    result = d.HttpStatusCode.ToString();
                }
                else
                {
                    result = "Object not found";
                }
            }
            catch
            {
                result = "Object not found";
            }
        }
        return result;
    }

    public static string GetBucketSize(S3Connector commandObject)
    {
        long filesize = 0;
        long filecount = 0;
        using (var client = new AmazonS3Client(commandObject.AccessKey, commandObject.Secret))
        {
            ListObjectsRequest request = new ListObjectsRequest();
            request = new ListObjectsRequest();
            request.BucketName = commandObject.Bucket;
            do
            {
                ListObjectsResponse response = client.ListObjects(request);

                foreach (var s3obj in response.S3Objects)
                {
                    filesize += s3obj.Size;
                    filecount++;
                }

                if (response.IsTruncated)
                {
                    request.Marker = response.NextMarker;
                }
                else
                {
                    request = null;
                }
            } while (request != null);
        }

        return FormatBytes(filesize) + " [" + filecount + " files]";
    }

    private static string FormatBytes(long bytes)
    {
        string[] Suffix = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double dblSByte = bytes;
        if (bytes > 1024)
            for (i = 0; (bytes / 1024) > 0; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;
        return String.Format("{0:0.##}{1}", dblSByte, Suffix[i]);
    }

    public static string CreateBucket(S3Connector commandObject)
    {
        var result = "";
        using (var client = new AmazonS3Client(commandObject.AccessKey, commandObject.Secret))
        {
            PutBucketRequest request = new PutBucketRequest
            {
                BucketName = commandObject.Bucket
            };
            try
            {
                client.PutBucket(request);
                result = "Bucket " + commandObject.Bucket + " created";
            }
            catch (Amazon.S3.AmazonS3Exception s)
            {
                return s.Message;
            }
        }
        return result;
    }

    public static string GetObject(S3Connector commandobject)
    {
        var result = "";
        using (var client = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret))
        {
            GetObjectRequest request = new GetObjectRequest
            {
                BucketName = commandobject.Bucket,
                Key = commandobject.FileRemote
            };

            using (GetObjectResponse response = client.GetObject(request))
            {
                string dest = Path.Combine(commandobject.FileLocal, commandobject.FileRemote);

                if (!File.Exists(dest))
                {
                    response.WriteResponseStreamToFile(dest);
                }
            }
        }
        return result;
    }

    public static string UploadDirectory(S3Connector commandobject)
    {
        if (commandobject.ZipThis)
        {
            // if we have elected to zip the folder, we are actually just sending a single zip file, so we process this as a file upload.
            UploadFile(commandobject);
            return "";
        }
        else
        {

            TransferUtility fileTransferUtility = new TransferUtility(commandobject.AccessKey, commandobject.Secret);

            var keyprefix = commandobject.FileRemote;
            if (commandobject.DateStampFilename)
            {
                keyprefix = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
            }

            TransferUtilityUploadDirectoryRequest dirrequest = new TransferUtilityUploadDirectoryRequest()
            {
                BucketName = commandobject.Bucket,
                KeyPrefix = keyprefix,
                Directory = commandobject.FileLocal,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
                StorageClass = S3StorageClass.Standard
            };

            dirrequest.UploadDirectoryProgressEvent += directoryTransferUtilityRequest_UploadProgressEvent;

            switch (commandobject.StorageOption)
            {
                case StorageOptions.REDUCEDREDUNDANCY:
                    dirrequest.StorageClass = S3StorageClass.ReducedRedundancy;
                    break;
                case StorageOptions.STANDARDIA:
                    dirrequest.StorageClass = S3StorageClass.ReducedRedundancy;
                    break;
                case StorageOptions.STANDARD:
                    dirrequest.StorageClass = S3StorageClass.Standard;
                    break;
                case StorageOptions.GLACIER:
                    dirrequest.StorageClass = S3StorageClass.Glacier;
                    break;
                default:
                    dirrequest.StorageClass = S3StorageClass.Standard;
                    break;
            }

            fileTransferUtility.UploadDirectory(dirrequest);

            return "Upload completed";
        }
    }

    public static string UploadFile(S3Connector commandobject)
    {
        Console.WriteLine("Uploading " + commandobject.FileLocal);
        FileInfo f = null;

        if (commandobject.ZipThis)
        {
            string tempPath = System.IO.Path.GetTempPath();
            using (ZipFile zip = new ZipFile())
            {
                var zipfilename = "";
                if (commandobject.Action == S3Action.PUTFOLDER)
                {

                    zipfilename = commandobject.FileLocal.Replace(":", "").Replace("\\", "_");
                }
                else
                {
                    f = new FileInfo(commandobject.FileLocal);
                    zipfilename = Path.GetFileNameWithoutExtension(f.Name);
                }


                if (commandobject.ZipSpeed == ZipSpeedOptions.DEFAULT)
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                }
                if (commandobject.ZipSpeed == ZipSpeedOptions.FAST)
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestSpeed;
                }
                if (commandobject.ZipSpeed == ZipSpeedOptions.SLOW)
                {
                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
                }

                if (commandobject.Action == S3Action.PUTFILE)
                {
                    zip.AddFile(commandobject.FileLocal);
                }
                if (commandobject.Action == S3Action.PUTFOLDER)
                {
                    zip.AddDirectory(commandobject.FileLocal);
                }

                var tempZipFile = Path.Combine(tempPath, zipfilename + ".zip");
                Console.WriteLine("Please wait... zipping files");
                zip.Save(tempZipFile);
                f = new FileInfo(tempZipFile);
            }
        }
        else
        {
            f = new FileInfo(commandobject.FileLocal);
        }

        AmazonS3Config c = new AmazonS3Config();
        if (!String.IsNullOrEmpty(commandobject.ProxyAddress))
        {
            c.ProxyHost = commandobject.ProxyAddress;
            var proxy = WebRequest.GetSystemWebProxy();
            c.ProxyCredentials = CredentialCache.DefaultCredentials;
        }
        if (!String.IsNullOrEmpty(commandobject.ProxyPort))
        {
            c.ProxyPort = int.Parse(commandobject.ProxyPort);
        }
        AmazonS3Client cli = new AmazonS3Client(commandobject.AccessKey, commandobject.Secret, c);

        TransferUtility fileTransferUtility = new TransferUtility(cli);

        var filename = f.Name;
        if (commandobject.DateStampFilename)
        {
            filename = filename.AppendTimeStamp();
        }
        Console.WriteLine("Uploading.." + f.FullName + " as " + filename);

        TransferUtilityUploadRequest fileTransferUtilityRequest = new TransferUtilityUploadRequest()
        {
            BucketName = commandobject.Bucket,
            Key = filename,
            FilePath = f.FullName,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            StorageClass = S3StorageClass.Standard,
            PartSize = (10 * 1024 * 1024)
        };

        fileTransferUtilityRequest.UploadProgressEvent += fileTransferUtilityRequest_UploadProgressEvent;

        switch (commandobject.StorageOption)
        {
            case StorageOptions.REDUCEDREDUNDANCY:
                fileTransferUtilityRequest.StorageClass = S3StorageClass.ReducedRedundancy;
                break;
            case StorageOptions.STANDARDIA:
                fileTransferUtilityRequest.StorageClass = S3StorageClass.ReducedRedundancy;
                break;
            case StorageOptions.STANDARD:
                fileTransferUtilityRequest.StorageClass = S3StorageClass.Standard;
                break;
            case StorageOptions.GLACIER:
                fileTransferUtilityRequest.StorageClass = S3StorageClass.Glacier;
                break;
            default:
                fileTransferUtilityRequest.StorageClass = S3StorageClass.Standard;
                break;
        }

        fileTransferUtility.Upload(fileTransferUtilityRequest);

        if (File.Exists(f.FullName) && commandobject.ZipThis)
        {
            File.Delete(f.FullName);
        }

        return "Upload completed";

    }

    public static void fileTransferUtilityRequest_UploadProgressEvent(object c, UploadProgressArgs e)
    {
        Console.Write("\r{0}/{1}", e.TransferredBytes, e.TotalBytes);
    }

    public static void directoryTransferUtilityRequest_UploadProgressEvent(object c, UploadDirectoryProgressArgs e)
    {
        Console.Write("\r{0}/{1}", e.CurrentFile, e.TotalNumberOfFiles);
    }
}

