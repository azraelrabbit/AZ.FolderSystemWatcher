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

            folderWatcher.StartWatch();

            Console.ReadLine();

            folderWatcher.StopWatch();
        }
    }
}
