using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
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

            //folderWatcher.FileCreated += FolderWatcher_FileCreated;
            //folderWatcher.FolderCopied += FolderWatcher_FolderCopied;
            //folderWatcher.FolderReplaced += FolderWatcher_FolderReplaced;

            folderWatcher.WatchItemCompleted += FolderWatcher_WatchItemCompleted;

            folderWatcher.StartWatch();

            Console.ReadLine();

            folderWatcher.StopWatch();
            //var intlist = NetworkInterface.GetAllNetworkInterfaces().ToList();
            //foreach (var ints in intlist)
            //{
            //    Console.WriteLine(ints.Description);
            //}

            Console.ReadLine();
        }

        private static void FolderWatcher_WatchItemCompleted(object sender, FolderFileEventArgs e)
        {
            Console.WriteLine("{0} : {1}",e.WatchType, e.FullPath);
        }

  


    }
}
