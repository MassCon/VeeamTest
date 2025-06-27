using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Collections;


class FolderSync
{
    // private string sourcePath;
    // private string replicaPath;
    // private string logFilePath;
    // private int intervalSeconds;

    public string SourcePath { get; set; }
    public string ReplicaPath { get; set; }
    public string LogFilePath { get; set; }
    public int IntervalSeconds { get; set; }


   public void syncFolders(string source, string replica)
{
    Directory.CreateDirectory(replica); // Ensure replica root exists

    //create all directories from source in replica
    var sourceDirs = Directory.GetDirectories(source, "*", SearchOption.AllDirectories);
    foreach (var dir in sourceDirs)
    {
        var relativePath = Path.GetRelativePath(source, dir);
        var targetDir = Path.Combine(replica, relativePath);
        Directory.CreateDirectory(targetDir);
    }

    //copy/update files
    var sourceFiles = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
    var replicaFiles = Directory.GetFiles(replica, "*", SearchOption.AllDirectories);

    foreach (var srcFile in sourceFiles)
    {
        var relativePath = Path.GetRelativePath(source, srcFile);
        var targetFile = Path.Combine(replica, relativePath);
        var targetDir = Path.GetDirectoryName(targetFile);
        Directory.CreateDirectory(targetDir);

        if (!File.Exists(targetFile) || !FilesAreEqual(srcFile, targetFile))
        {
            File.Copy(srcFile, targetFile, true);
            Log($"Copied/Updated: {relativePath}");
        }
    }

    //delete extra files in replica
    foreach (var repFile in replicaFiles)
    {
        var relativePath = Path.GetRelativePath(replica, repFile);
        var srcFile = Path.Combine(source, relativePath);
        if (!File.Exists(srcFile))
        {
            File.Delete(repFile);
            Log($"Deleted file: {relativePath}");
        }
    }

    // delete extra directories in replica (bottom-up)
    var replicaDirs = Directory.GetDirectories(replica, "*", SearchOption.AllDirectories)
                               .OrderByDescending(d => d.Length); // delete deepest folders first
    foreach (var repDir in replicaDirs)
    {
        var relativePath = Path.GetRelativePath(replica, repDir);
        var srcDir = Path.Combine(source, relativePath);

        if (!Directory.Exists(srcDir) && Directory.Exists(repDir) && IsDirectoryEmpty(repDir))
        {
            Directory.Delete(repDir);
            Log($"Deleted folder: {relativePath}");
        }
    }
}

//check if directory is empty
private bool IsDirectoryEmpty(string path)
{
    return !Directory.EnumerateFileSystemEntries(path).Any();
}

    static bool FilesAreEqual(string file1, string file2)
    {
        using (var md5 = MD5.Create())
        {
            using var stream1 = File.OpenRead(file1);
            using var stream2 = File.OpenRead(file2);
            var hash1 = md5.ComputeHash(stream1);
            var hash2 = md5.ComputeHash(stream2);
            return StructuralComparisons.StructuralEqualityComparer.Equals(hash1, hash2);
        }
    }


    public void Log(string message)
    {
        var logMessage = $"[{DateTime.Now}] {message}";
        Console.WriteLine(logMessage);
        File.AppendAllText(LogFilePath, logMessage + Environment.NewLine);
    }

    public void Start()
    {
        //check the inputs
        if (SourcePath == null)
        {
            Console.WriteLine("source path is not set");
            return;
        }
        if (ReplicaPath == null)
        {
            Console.WriteLine("replica path is not set, using default: Replica");
            ReplicaPath = "Replica";
        }
         if (LogFilePath == null)
        {
            Console.WriteLine("log file path is not set, using default : app.log");
            LogFilePath = "app.log";
        }
        if (IntervalSeconds <= 0)
        {
            Console.WriteLine("interval is not set, using default: 1 second");
            IntervalSeconds = 1;
        }

        //actual start after the eveything is fine
        Log("Starting synchronization...");
        while (true)
        {
            try
        {
            syncFolders(SourcePath, ReplicaPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during synchronization: {ex.Message}");
            Log($"Error during synchronization: {ex.Message}");
        }
        Thread.Sleep(IntervalSeconds * 1000);
        }
        

    }
}

class Program
{
    static void Main(string[] args)
    {
        
        /* var folderSync = new FolderSync();

        Console.WriteLine("Input Source path: ");
        folderSync.SourcePath = Console.ReadLine();

        Console.WriteLine("Input Replica path: ");
        folderSync.ReplicaPath = Console.ReadLine();

        Console.WriteLine("Input Log file path: ");
        folderSync.LogFilePath = Console.ReadLine();

        Console.WriteLine("Input interval (seconds): ");
        folderSync.IntervalSeconds = int.Parse(Console.ReadLine());


        folderSync.Start(); */

        if (args.Length < 4)
        {
            Console.WriteLine("Usage: FolderSync.exe <sourcePath> <replicaPath> <logFilePath> <intervalSeconds>");
            return;
        }

        var folderSync = new FolderSync
        {
            SourcePath = args[0],
            ReplicaPath = args[1],
            LogFilePath = args[2],
            IntervalSeconds = int.TryParse(args[3], out var seconds) ? seconds : 1
        };

        folderSync.Start();
    
    }
}


