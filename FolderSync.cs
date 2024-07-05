using System.Security.Cryptography;
using NLog;


class FolderSync
{
    // Instantiate 'logger' objecto from NLog framework
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    static void Main(string[] args)
    {
        string sourcePath = args[0];
        string destinationPath = args[1];
        int intervalInMinutes = int.Parse(args[2]);        
        int intervalInMillis = intervalInMinutes * 60000;

        Logger.Info("Starting folder synchronization program");
        Console.WriteLine("Starting folder synchronization program");

        /*
         * Create timer to repeatedly execute 'SynchronizeFolders' method:
         * 
         * I tried to use the .Net Timer but the synchronization doesn't work well
         * It just syncs the main directory files and the first subdirectory, the other subdirectories don't sinc
         * I think the timer has, somehow, a conflit with the Recursive method
         * So I used the Thread.Sleep methor instead
        */

        /*
         * Timer timer = new Timer(
         * e => Synchronize(sourcePath, destinationPath),
         * null,
         * TimeSpan.Zero,
         * TimeSpan.FromMinutes(intervalInMinutes));
        */

        while (true)
        {
            SynchronizeFolders(sourcePath, destinationPath);
            Thread.Sleep(intervalInMillis);
        }
    }

    // This method synchronizes the contents of the source and destination directories
    private static void SynchronizeFolders(string source, string destination)
    {
        try
        {
            DirectoryInfo sourceDir = new DirectoryInfo(source);
            DirectoryInfo destDir = new DirectoryInfo(destination);

            // Ensures the source directory exists
            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist: {source}");
            }

            // Creates the replica directory if it does not exist
            if (!destDir.Exists)
            {
                destDir.Create();
                Logger.Info($"Created destination directory: {destination}");
                Console.WriteLine($"Created destination directory: {destination}");
            }

            // Copy files from source to replica
            foreach (FileInfo sourceFile in sourceDir.GetFiles())
            {
                string destFilePath = Path.Combine(destination, sourceFile.Name);
                if (!File.Exists(destFilePath) || !CompareFileHashes(sourceFile.FullName, destFilePath))
                {
                    sourceFile.CopyTo(destFilePath, true);
                    Logger.Info($"Copied file: {sourceFile.FullName} to {destFilePath}");
                    Console.WriteLine($"Copied file: {sourceFile.FullName} to {destFilePath}");
                }
            }

            // Recursively synchronize subdirectories
            foreach (DirectoryInfo sourceSubDir in sourceDir.GetDirectories())
            {
                string destSubDirPath = Path.Combine(destination, sourceSubDir.Name);
                SynchronizeFolders(sourceSubDir.FullName, destSubDirPath);
            }

            // Delete files in destination that do not exist in source
            foreach (FileInfo destFile in destDir.GetFiles())
            {
                string sourceFilePath = Path.Combine(source, destFile.Name);
                if (!File.Exists(sourceFilePath))
                {
                    destFile.Delete();
                    Logger.Info($"Deleted file: {destFile.FullName}");
                    Console.WriteLine($"Deleted file: {destFile.FullName}");
                }
            }

            // Delete subdirectories in destination that do not exist in source
            foreach (DirectoryInfo destSubDir in destDir.GetDirectories())
            {
                string sourceSubDirPath = Path.Combine(source, destSubDir.Name);
                if (!Directory.Exists(sourceSubDirPath))
                {
                    destSubDir.Delete(true);
                    Logger.Info($"Deleted directory: {destSubDir.FullName}");
                    Console.WriteLine($"Deleted directory: {destSubDir.FullName}");
                }
            }

            Logger.Info($"Synchronization completed at {DateTime.Now}");
            Console.WriteLine($"Synchronization completed at {DateTime.Now}");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "An error occurred during synchronization");
            Console.WriteLine(ex.Message + "An error occurred during synchronization");
        }
    }

    // This method compares the MD5 hashes (32 bit hexadecimal code) of two files to determine if they are identical
    private static bool CompareFileHashes(string filePath1, string filePath2)
    {
        using (var md5 = MD5.Create())
        {
            byte[] hash1;
            byte[] hash2;

            using (var stream1 = File.OpenRead(filePath1))
            {
                hash1 = md5.ComputeHash(stream1);
            }

            using (var stream2 = File.OpenRead(filePath2))
            {
                hash2 = md5.ComputeHash(stream2);
            }

            for (int i = 0; i < hash1.Length; i++)
            {
                if (hash1[i] != hash2[i])
                {
                    return false;
                }
            }
        }

        return true;
    }
}