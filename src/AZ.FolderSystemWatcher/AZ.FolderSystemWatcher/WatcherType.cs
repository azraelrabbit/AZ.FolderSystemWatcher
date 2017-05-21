using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZ.IO.FileSystem
{
    public enum WatcherType
    {
        FileCreate,
        FileReplace,
        FileRename,

        FolderCreate,
        FolderRename,
        FolderReplace,
        Delete
    }
}
