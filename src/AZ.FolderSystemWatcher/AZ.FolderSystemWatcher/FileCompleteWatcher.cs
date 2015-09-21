using System;
using System.IO;
using System.Threading;

namespace AZ.IO.FileSystem
{
    public class FileCompleteWatcher
    {
        private WatcherItem _fileItem;

        public event EventHandler<FileCompleteEventArgs> FileCompleted;
        public  Guid FwId { get; private set; }
        private System.Threading.Timer _timer;

        public FileCompleteWatcher(WatcherItem fileItem)
        {
            _fileItem = fileItem;
            FwId = Guid.NewGuid();

        }

        private void onInterval(object state)
        {
            if (FileUnlocked())
            {
                _timer.Change(1000, 0);
                _timer.Dispose();
                OnFileCompleted(new FileCompleteEventArgs() { FwId = FwId, FileItem = _fileItem });
               
            }
        }

        public void Start()
        {
            _timer = new Timer(onInterval, null, 500, 500);
        }

        public bool FileUnlocked()
        {

            try
            {
                var fi=new FileInfo(_fileItem.FullPath);
                var fs= fi.OpenRead();
                fs.Close();
               // var fs = File.Open(_filePath, FileMode.Open);
                return true;
            }
            catch
            {
                return false;
            }

        }


        protected virtual void OnFileCompleted(FileCompleteEventArgs e)
        {
            FileCompleted?.Invoke(this, e);
        }
    }

    public class FileCompleteEventArgs : EventArgs
    {
        public Guid FwId { get; set; }

        public WatcherItem FileItem { get; set; }
    }
}
