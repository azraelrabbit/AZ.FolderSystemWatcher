using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace AZ.IO.FileSystem
{
    internal class FolderCompleteWatcher
    {
        private WatcherItem _folderItem;

        public event EventHandler<FileCompleteEventArgs> FolderCompleted;
         
        public Guid FwId { get; private set; }

        private System.Threading.Timer _timer;

        private int _timerInterval = 3000;// default Millisecond to execute onInterval function

        private long oldSize;

        public FolderCompleteWatcher(WatcherItem folderItem)
        {
            _folderItem = folderItem;
            FwId = Guid.NewGuid();

        }

        public FolderCompleteWatcher(WatcherItem folderItem,int interval)
        {
            _folderItem = folderItem;
            FwId = Guid.NewGuid();

            if (interval > 0)
            {
                _timerInterval = interval;
            }
        }

        private void onInterval(object state)
        {
            var size = GetDirectorySize(_folderItem.FullPath);

            if (size == oldSize)
            {
                try
                {
                    _timer.Change(1000, 0);
                    _timer.Dispose();
                }
                catch { }

                //Synchronize event
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
            _timer = new Timer(onInterval, null, 1000, _timerInterval);
        }

        protected virtual void OnFolderCompleted(FileCompleteEventArgs e)
        {
            FolderCompleted?.Invoke(this, e);
        }
 
    }
}
