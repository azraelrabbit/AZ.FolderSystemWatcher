﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZ.IO.FileSystem
{
    internal class WatcherItem
    {
        public WatcherType WatcherType { get; set; }

        public string FullPath { get; set; }


    }
}
