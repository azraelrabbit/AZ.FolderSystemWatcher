using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZ.IO.FileSystem
{
    public class FolderFileEventArgs:EventArgs
    {
        /// <summary>
        /// this property only effective on renaming
        /// </summary>
        public string OldFullPath { get; set; }
        public string FullPath { get; set; }

        public WatcherType WatchType { get; set; }
    }
}
