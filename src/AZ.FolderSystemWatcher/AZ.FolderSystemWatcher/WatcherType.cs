using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZ.IO.FileSystem
{
    internal enum WatcherType
    {
        FileCreate,
        FileReplace,
        FolderCreate,
        FolderCopy,
        FolderReplace
    }
}
