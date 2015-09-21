using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AZ.IO.FileSystem;

namespace Watcher.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var targetFolder = @"E:\watchertest";

            var folderWatcher = new FolderFileWatcher(targetFolder);

            folderWatcher.FileCreated += FolderWatcher_FileCreated;
            folderWatcher.FolderCopied += FolderWatcher_FolderCopied;
            folderWatcher.FolderReplaced += FolderWatcher_FolderReplaced;


            folderWatcher.StartWatch();

            Console.ReadLine();

            folderWatcher.StopWatch();
        }

        private static void FolderWatcher_FolderReplaced(object sender, FolderFileEventArgs e)
        {
            Console.WriteLine("Folder Replaced : {0}", e.FullPath);
        }

        private static void FolderWatcher_FolderCopied(object sender, FolderFileEventArgs e)
        {
            Console.WriteLine("Folder Copied : {0}",e.FullPath);
        }

        private static void FolderWatcher_FileCreated(object sender, FolderFileEventArgs e)
        {
            Console.WriteLine("File Created : {0}",e.FullPath);
        }


    }
}
