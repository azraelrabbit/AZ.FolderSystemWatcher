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
 
        FolderCreate,
      
        FolderReplace,
        Delete
    }
}
