using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZ.IO.FileSystem
{
    public class WatcherItem
    {
        public WatcherType WatcherType { get; set; }

        public string FullPath { get; set; }


    }
}
