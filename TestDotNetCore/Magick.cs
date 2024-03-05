using System.Collections.Concurrent;
using System.Diagnostics;

namespace TestDotNetCore
{
    
    /// <summary>
    /// File Holder.
    /// </summary>
    public class FileHolder
    {
        public string Name { get; set; }
        public long Size { get; set; }

        /// <summary>
        /// Implement tostring
        /// </summary>
        public override string ToString()
        {
            return $"{Magick.FormatFileSize(Size), 16} - {Name}";
        }
    }
    
    /// <summary>
    /// Able to call magick. 
    /// </summary>
    public class Magick
    {
        public bool Debug { get; set; } = true;
        private static string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        
        /// <summary>
        /// Resizes all images within a specified folder.
        /// </summary>
        public void ResizeAllImagesInFolder(string folder, int percentage, long minSize = 0L)
        {
            NBConsole.Log($"{GetType().Name}.ResizeAllImagesInFolder:-- START, folder: {folder}, percentage: {percentage}, minSize: {minSize/(1024*1024)}MB");
            
            ConcurrentQueue<FileHolder> allfiles = new ConcurrentQueue<FileHolder>();
            ConcurrentQueue<string> folders = new ConcurrentQueue<string>();

            BatchAndSplitTopLevel(folder, allfiles, folders);
            BatchAndProcessAllFiles(folders, allfiles);

            List<FileHolder> allFilesList = FlattenList(allfiles);

            int bs = allFilesList.Count / Math.Min(16, allFilesList.Count);
            NBConsole.Log($"{GetType().Name}.ResizeAllImagesInFolder top level folders: foldersList: {allFilesList.Count}, bs: {bs}");

            Parallel.ForEach(Partitioner.Create(0, allFilesList.Count, bs), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    try
                    {
                        FileHolder fileholder = allFilesList[i];
                        Resize(fileholder.Name, percentage, minSize);
                    } //try
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{GetType().Name}.BatchAndProcessAllFiles Warning: {ex}");
                    }
                } //for
            }); //Parallel
            
            NBConsole.Log($"{GetType().Name}.ResizeAllImagesInFolder top level folders: foldersList: {allFilesList.Count}, bs: {bs} DONE!!!");
            
        }
        
        
        /// <summary>
        /// Group and tally folder sizes.
        /// </summary>
        private List<FileHolder> FlattenList(ConcurrentQueue<FileHolder> allfiles)
        {
            List<FileHolder> allFilesList = new List<FileHolder>();
            while (allfiles.Count > 0)
            {
                FileHolder fld = null;
                allfiles.TryDequeue(out fld);
                if (fld != null) allFilesList.Add(fld);
            } //while

            // sort
            allFilesList = allFilesList.OrderBy(o => o.Name).ToList();
            if (Debug) allFilesList.ForEach(x => NBConsole.Log($"{GetType().Name}.FlattenList file: {x}"));
            return allFilesList;
        }

        /// <summary>
        /// Batch and process all files.
        /// </summary>
        private void BatchAndProcessAllFiles(ConcurrentQueue<string> folders, ConcurrentQueue<FileHolder> allfiles)
        {
            NBConsole.Log($"{GetType().Name}.BatchAndProcessAllFiles top level folders: folders.Count : {folders.Count }");
            if (folders.Count < 1) return;
            List<string> foldersList = new List<string>();
            while (folders.Count > 0)
            {
                string fld = null;
                folders.TryDequeue(out fld);
                if (fld != null) foldersList.Add(fld);
                NBConsole.Log($"{GetType().Name}.BatchAndProcessAllFiles top level folders: folder: {fld}");
            } //while
            foldersList.Sort();

            // now find all files
            int bs = foldersList.Count / Math.Min(16, foldersList.Count);
            NBConsole.Log($"{GetType().Name}.BatchAndProcessAllFiles top level folders: foldersList: {foldersList.Count}, bs: {bs}");

            Parallel.ForEach(Partitioner.Create(0, foldersList.Count, bs), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    string[] fles = Directory.GetFiles(foldersList[i], "*", new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = true });
                    Parallel.ForEach(fles, x =>
                    {
                        try
                        {
                            if (!x.ToLower().EndsWith(".png") && !x.ToLower().EndsWith(".tga") &&
                                !x.ToLower().EndsWith(".exr") && !x.ToLower().EndsWith(".jpg") &&
                                !x.ToLower().EndsWith(".bmp")) return;
                            FileInfo fi = new FileInfo(x);
                            FileHolder fh = new FileHolder() { Name = x, Size = fi.Length };
                            allfiles.Enqueue(fh);
                        } //try
                        catch (Exception ex)
                        {
                            Console.WriteLine($"{GetType().Name}.BatchAndProcessAllFiles Warning: {ex}");
                        }
                    });
                } //for
            }); //Parallel

        }

        /// <summary>
        /// Batch and split top level (3) folders and top level files.
        /// </summary>
        private void BatchAndSplitTopLevel(string folder, ConcurrentQueue<FileHolder> allfiles, ConcurrentQueue<string> folders)
        {
            // first 3 level folders.
            //folders.Enqueue(folder);
            Batch(folder, allfiles);

            string[] dirs = Directory.GetDirectories(folder, "*", new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = false });           // lvl1
            Parallel.ForEach(dirs, x =>
            {
                Batch(x, allfiles);
                //folders.Enqueue(x);
                string[] dirs2 = Directory.GetDirectories(x, "*", new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = false });           // lvl2
                Parallel.ForEach(dirs2, x2 =>
                {
                    Batch(x2, allfiles);
                    //folders.Enqueue(x2);
                    string[] dirs3 = Directory.GetDirectories(x2, "*", new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = false });      // lvl3
                    Parallel.ForEach(dirs3, x3 => folders.Enqueue(x3));
                });

            });

        }

        /// <summary>
        /// Batch top level files only.
        /// </summary>
        public void Batch(string folder, ConcurrentQueue<FileHolder> allfiles)
        {
            string[] fles = Directory.GetFiles(folder, "*", new EnumerationOptions() { IgnoreInaccessible = true, RecurseSubdirectories = false });
            Parallel.ForEach(fles, x =>
            {
                try
                {
                    if (!x.ToLower().EndsWith(".png") && !x.ToLower().EndsWith(".tga") &&
                        !x.ToLower().EndsWith(".exr") && !x.ToLower().EndsWith(".jpg") &&
                        !x.ToLower().EndsWith(".bmp")) return;
                    FileInfo fi = new FileInfo(x);
                    FileHolder fh = new FileHolder() { Name = x, Size = fi.Length };
                    allfiles.Enqueue(fh);
                } //try
                catch (Exception ex)
                {
                    Console.WriteLine($"{GetType().Name}.Batch Warning: {ex}");
                }
            });
        }        
        /// <summary>
        /// Resize based on percentage, if file size exceeds threashold.
        /// </summary>
        public void Resize(string file, int percentage, long minSize = 0L)
        {
            if (!File.Exists(file)) return;
            if (new FileInfo(file).Length <= minSize) return;

            string cmd = $"/c magick \"{file}\" -resize {percentage}% \"{file}\"";
            NBConsole.Log($"{GetType().Name}.Resize cmd: {cmd}");
            ExecuteCommand("cmd.exe", cmd);
        }
        
        /// <summary>
        /// Execute the command.
        /// </summary>
        public static string ExecuteCommand(string command, string arguments)
        {
            ProcessStartInfo processStartInfo = new ProcessStartInfo(command, arguments)
            {
                RedirectStandardOutput = true, // Enable stdout redirection
                UseShellExecute = false, // Do not use shell
                CreateNoWindow = true, // Do not create a window
            };

            Process process = Process.Start(processStartInfo);
            process?.WaitForExit(); // Wait for the process to finish

            string output = process.StandardOutput.ReadToEnd(); // Read the output
            return output;
        }
        
        /// <summary>
        /// Format file size.
        /// </summary>
        public static string FormatFileSize(long fileSize) { return FormatFileSize((ulong)fileSize); }

        /// <summary>
        /// Format file size.
        /// </summary>
        public static string FormatFileSize(ulong fileSize)
        {

            double len = (double)fileSize;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order += 1;
                len /= 1024.0d;
            }

            return String.Format("{0:0.##} {1}", len, sizes[order]);
        }
        
    }
    
    /// <summary>
    /// Console logging.
    /// </summary>
    public static class NBConsole
    {
        /// <summary>
        /// Collection.
        /// </summary>
        private static readonly ConcurrentQueue<string> QUEUE = new ConcurrentQueue<string>();

        /// <summary>
        /// Constructor.
        /// </summary>
        static NBConsole()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (QUEUE.Count > 0)
                    {
                        string msg = null;
                        QUEUE.TryDequeue(out msg);

                        if (msg != null)
                        {
                            Console.WriteLine(msg);
                        } //if
                    } //if
                }
            });
        }

        /// <summary>
        /// Done?
        /// </summary>
        /// <returns></returns>
        public static bool Done()
        {
            return QUEUE.Count < 1;
        }


        /// <summary>
        /// Log to console.
        /// </summary>
        /// <param name="msg"></param>
        public static void Log(string msg)
        {
            QUEUE.Enqueue($"{DateTime.UtcNow.ToString()} - [DEBUG] {msg}");
        }

        /// <summary>
        /// Stamp now.
        /// </summary>
        /// <returns></returns>
        public static long Stamp()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <summary>
        /// Stamps from time.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string Stamp(long time)
        {
            return TimeSpan.FromMilliseconds(Stamp() - time).ToString();
        }
    }
    
}
