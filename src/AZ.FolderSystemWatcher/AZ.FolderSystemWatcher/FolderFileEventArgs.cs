using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AZ.IO.FileSystem
{
    public class FolderFileEventArgs:EventArgs
    {
        public string FullPath { get; set; }
    }
}
