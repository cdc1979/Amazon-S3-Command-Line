#Amazon S3 Command Line Client

Use this client to easily upload files or folders to an Amazon S3 bucket from the windows command line.

<h3><a href="http://dv2e2roxiy8vr.cloudfront.net/S3commandline_1.0.0.0.zip">Download Windows installer</a></h1>

(automatically adds s3cli.exe to windows path)

* Requires .NET framework 4.5

##Features

- Command via arguments or via JSON config file (see example)
- Upload Files/Folders to an Amazon S3 Bucket
- Upload Progress displayed
- List objects in a bucket
- Get object
- Delete object
- Create/Delete Bucket
- Automatically Save amazon access credentials into registry
- Standard or Reduced Redundancy
- Automatic Zip Compression of files
- Integration of AlertScale Notifications

##Arguments
<pre>
Usage: s3cli options

   OPTION                   TYPE              DESCRIPTION
   -Action(-A)              s3action*         Available action types: PUTFILE,PUTFOLDER,GET,DELETE,DELETEBUCKET,CREATEBUCKET,LIST,USECONFIG
                                                USECONFIG
                                                PUTFILE
                                                PUTFOLDER
                                                GET
                                                DELETE
                                                DELETEBUCKET
                                                CREATEBUCKET
                                                LIST
   -AccessKey(-Ac)          string            Your Amazon Access Key
   -Secret(-S)              string            Your Amazon Secret Key
   -Bucket(-B)              string            Your Amazon S3 Bucket Name
   -FileLocal(-F)           string            Local File or Folder
   -FileRemote(-Fi)         string            Remote Object Name (The file or folder name stored in S3)
   -ReducedRedundancy(-R)   switch            Use Reduced Redundancy Storage
   -ConfigFile(-C)          string            Load settings from a config file
   -DateStampFilename(-D)   switch            Automatically adjust filename to include current date and time
   -ProxyAddress(-P)        string            Proxy server address e.g. http://myproxy
   -ProxyPort(-Pr)          string            Proxy server port e.g. 8080
   -AlertScaleApiKey(-Al)   string            AlertScale API Key (For success/fail notifications)
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

The client is capable of sending a success/failure notification to AlertScale when a file has uploaded so you can be alerted if you are using this from a scehduled task for example.  
You need to provide your API Key from your AlertScale account as an argument.  This is very useful if you are uploading files to S3 for backup purposes and want
to get notified that the upload worked.