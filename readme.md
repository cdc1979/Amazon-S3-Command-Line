#Amazon S3 Windows Command Line Client

Latest Version: 1.4.0.0

Use this client to easily upload files or folders to an Amazon S3 bucket 
from the windows command line.  I use this tool to schedule automatic backups 
of selected folders and files using windows task scheduler - no need to worry about overwrites 
as the files can automatically get the current date and time added onto the filename.

<h3><a href="https://github.com/cdc1979/Amazon-S3-Command-Line/archive/master.zip">Download Latest Version 1.4.0.0</a></h1>

(Windows installer is in /install subfolder - automatically adds s3cli.exe to windows path)

* Requires .NET framework 4.7.2
* You may get a warning when downloading, this is because I have not signed the installer.

##Features

- Command via arguments or via JSON config file (see example)
- Upload Files/Folders to an Amazon S3 Bucket
- Upload Progress displayed
- List objects in a bucket
- Get object
- Delete object
- Create/Delete Bucket
- Automatically Save amazon access credentials into registry
- Standard, Standard Infrequent Access, Reduced Redundancy or Glacier as Storage Options
- Automatic Zip Compression of files
- Calculate Bucket Size

##Arguments
<pre>
Usage: s3cli options

   OPTION                   TYPE              DESCRIPTION
   -Action(-A)              s3action*         Available action types: PUTFILE,PUTFOLDER,GET,DELETE,DELETEBUCKET,CREATEBUCKET,LIST,USECONFIG,BUCKETSIZE
                                                USECONFIG
                                                PUTFILE
                                                PUTFOLDER
                                                GET
                                                DELETE
                                                DELETEBUCKET
                                                CREATEBUCKET
                                                LIST
												BUCKETSIZE
   -AccessKey(-Ac)          string            Your Amazon Access Key
   -Secret(-S)              string            Your Amazon Secret Key
   -Bucket(-B)              string            Your Amazon S3 Bucket Name
   -FileLocal(-F)           string            Local File or Folder
   -FileRemote(-Fi)         string            Remote Object Name (The file or folder name stored in S3)
   -StorageOption(-St)		storageoptions    Set Storage Storage Type (see https://aws.amazon.com/s3/storage-classes/)
												STANDARD
												STANDARDIA
												REDUCEDREDUNDANCY
												GLACIER
   -ConfigFile(-C)          string            Load settings from a config file
   -DateStampFilename(-D)   switch            Automatically adjust filename to include current date and time
   -ProxyAddress(-P)        string            Proxy server address e.g. http://myproxy
   -ProxyPort(-Pr)          string            Proxy server port e.g. 8080
   -ZipThis(-Z)             switch            Zip files before uploading
   -ZipSpeed(-Zi)           zipspeedoptions   Zip compression level Best Compression/Normal/Least Compression
                                                SLOW
                                                DEFAULT
                                                FAST
   -PersistKeys(-Pe)        switch            Remember Amazon Credentials in Registry
</pre>

All files/folders uploaded will select AES-256 encryption by default.

##Command Line Examples


####Upload a file

<pre>s3cli -Ac ACCESSKEY -S SECRET -A PUTFILE -B BucketName -F D:\s3temp\aws_preview.gif</pre>

####Create a bucket

<pre>s3cli -Ac ACCESSKEY -S SECRET -A CREATEBUCKET -B s3cli-testbucket</pre>

Note that the bucket name must be unique and cannot have been used before by any other S3 user.

####Upload a folder, zip and rename based on current date and time

<pre>s3cli -Ac ACCESSKEY -S SECRET -A PUTFOLDER -F D:\sqlllite -B BucketName -D -Z</pre>

This uploads a single file, __D&#95;sqlllite&#95;20140403-095440206.zip__

####Calculate S3 Bucket Size

Use BUCKETSIZE action to calculate the size of a bucket (nicely formatted as MB/GB/TB) and also displays
a count of the number of total files.

<b>Note:</b> This call can be slow on very large buckets, as we can only manually page 1000 files in the bucket per request.

####Save credentials to registry

It is useful to persist the access key and secret into the registry so that the credentials
are not required for every subsequent request.  To do this simply as -Pe to any command

<pre>-Pe</pre>

Note: Registry key is saved here: __HKEY&#95;CURRENT&#95;USER\s3cli__ (on a per user basis)

####Load settings From Config File

If you want to script/schedule S3 commands,I recommend putting your settings into a config file, which can then be loaded from a single command and easily edited

<pre>s3cli -A USECONFIG -C config.json</pre>

####Date Stamp Your Upload

Automatically adds the current date and time to your file, to allow for repeat uploads of the same file.

<pre>-D</pre>

####Automatic Zip Compression

Automatically Zip your local files, and upload the resulting Zip file. (Useful to reduce storage costs)

<pre>-Z -Zi DEFAULT</pre>

You can adjust the compression level, to optimise for speed or for compression

##Notifications

To Do (add support for Raygun etc.)

##Version Changes

1.4.0.0

- Updated Restsharp, PowerArgs,Json.NET, DotNetZip, AWSSDK, Automapper libraries to Latest

1.3.0.0 

- Updated Restsharp, PowerArgs,Json.NET, DotNetZip, AWSSDK, Automapper libraries to Latest
- Added STANDARDIA and GLACIER as storage classes and changed the options slightly to support this