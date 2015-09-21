using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace AZ.IO.FileSystem
{
    public class FolderCompleteWatcher
    {
        private WatcherItem _folderItem;

        public event EventHandler<FileCompleteEventArgs> FolderCompleted;
        public Guid FwId { get; private set; }

        private System.Threading.Timer _timer;

        private long oldSize;

        public FolderCompleteWatcher(WatcherItem folderItem)
        {
            _folderItem = folderItem;
            FwId = Guid.NewGuid();

        }

        private void onInterval(object state)
        {
            //if (FileUnlocked())
            //{
            //    OnFileCompleted(new FileCompleteEventArgs() { FwId = FwId, FilePath = _filePath });
            //    _timer.Dispose();
            //}

            var size = GetDirectorySize(_folderItem.FullPath);
            //  Console.WriteLine("old size:{0}, new size: {1}", oldSize, size);

            if (size == oldSize)
            {
                _timer.Change(1000, 0);
                _timer.Dispose();
                OnFolderCompleted(new FileCompleteEventArgs() { FwId = FwId, FileItem = _folderItem });
            }
            else
            {
                oldSize = size;
            }
        }

        private static long GetDirectorySize(string folderPath)
        {
            DirectoryInfo di = new DirectoryInfo(folderPath);
            return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
        }

        public void Start()
        {
            _timer = new Timer(onInterval, null, 1000, 3000);//3秒钟检测一次
        }

        protected virtual void OnFolderCompleted(FileCompleteEventArgs e)
        {
            FolderCompleted?.Invoke(this, e);
        }
    }
}
