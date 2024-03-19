namespace AZ.FolderSystemWatcher.Next
{
    /// <summary>
    /// 文件以及文件夹拷贝完成的检查者 by Zero Plus
    /// </summary>
    public class AZFolderAndFileCopyCompletedChecker
    {
        ///// <summary>
        ///// 拷贝完成事件处理程序
        ///// </summary>
        ///// <param name="e"></param>
        //public delegate void OnCopyCompletedHandle(FileSystemEventArgs e);
        /// <summary>
        /// 拷贝完成事件
        /// </summary>
        public event EventHandler<FileSystemEventArgs> CopyCompleted;

        private void OnCopyCompleted(object sender, FileSystemEventArgs e)
        {
            CopyCompleted?.Invoke(sender,e);
        }

        /// <summary>
        /// 拷贝错误事件处理程序
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ex">错误</param>
        public delegate void OnCopyErrorHandle(FileSystemEventArgs e, Exception ex);
        /// <summary>
        /// 拷贝错误事件
        /// </summary>
        public event OnCopyErrorHandle CopyError;
        /// <summary>
        /// 要检查的路径
        /// </summary>
        readonly string _path;
        /// <summary>
        /// 文件大小
        /// </summary>
        long _size = -1;
        /// <summary>
        /// 计时器
        /// </summary>
        readonly Timer _timer;
        /// <summary>
        /// 检查间隔
        /// </summary>
        readonly int _interval;
        /// <summary>
        /// 是否文件夹
        /// </summary>
        readonly bool _isDirectory;
        /// <summary>
        /// 文件及文件夹事件参数
        /// </summary>
        FileSystemEventArgs _eventArgs;

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 文件及文件夹拷贝完成检查器
        /// </summary>
        /// <param name="path">要检查的路径</param>
        /// <param name="interval">检查间隔</param>
        public AZFolderAndFileCopyCompletedChecker(string path, int interval = 3000)
        {
            _path = path;
            _interval = interval;

            // 是否是文件夹
            var isDir = Utility.IsDirectory(_path);
            // 如果为空，说明地址无法访问
            if (isDir == null)
            {
                throw new ArgumentNullException($"{_path} NOT FOUND or CANNOT BE FOUND.");
            }
            _isDirectory = isDir.Value;
            // 比较文件大小的计时器
            _timer = new Timer(CompareSize, null, 0, Timeout.Infinite);
        }
        /// <summary>
        /// 文件及文件夹拷贝完成检查器
        /// </summary>
        /// <param name="e"></param>
        /// <param name="interval">检查间隔</param>
        public AZFolderAndFileCopyCompletedChecker(FileSystemEventArgs e, int interval = 3000)
        {
            _path = e.FullPath;
            _interval = interval;
            _eventArgs = e;

            var exist = Utility.IsDirectory(_path);
            if (exist == null)
            {
                throw new ArgumentNullException($"{_path} NOT FOUND or CANNOT BE FOUND.");
            }
            _isDirectory = exist.Value;
            _timer = new Timer(CompareSize, null, 0, Timeout.Infinite);
        }

        /// <summary>
        /// 比较文件大小
        /// </summary>
        /// <param name="state"></param>
        void CompareSize(object state)
        {
            try
            {
                // 如果是文件夹
                if (_isDirectory)
                {
                    // 如果文件夹不存在 则抛出异常
                    if (!Directory.Exists(_path))
                    {
                        throw new ArgumentNullException($"Directory {_path} NOT FOUND or CANNOT BE FOUND.");
                    }
                    try
                    {
                        // 获取文件夹大小
                        var size = Utility.GetDirectorySize(_path);
                        // 如果与上一次计算的文件夹大小相等
                        if (size == _size)
                        {
                            // 释放计时器
                            _timer.Dispose();
                           
                            if (_eventArgs == null)
                            {
                                var di = new DirectoryInfo(_path);
                                if (di.Parent != null)
                                {
                                    _eventArgs = new FileSystemEventArgs(WatcherChangeTypes.All, di.Parent.FullName,
                                        di.Name);
                                }
                            }
                            // 触发拷贝完成事件
                            OnCopyCompleted(this,_eventArgs);
                        }
                        else
                        {
                            // 将本次计算的文件夹大小缓存
                            _size = size;
                            // 间隔时间后，再次比对
                            _timer.Change(_interval, Timeout.Infinite);
                        }
                    }
                    catch
                    {
                        // 间隔时间后，再次比对
                        _timer.Change(_interval, Timeout.Infinite);
                    }

                }
                // 如果是文件
                else
                {
                    if (!File.Exists(_path))
                    {
                        throw new ArgumentNullException($"File {_path} NOT FOUND or CANNOT BE FOUND.");
                    }
                    try
                    {
                        var fi = new FileInfo(_path);
                        var fs = fi.OpenRead();
                        fs.Close();

                        // 如果能够打开 说明文件未被只读占用，说明文件已经拷贝完成
                        _timer.Dispose();
                       
                        if (_eventArgs == null)
                        {
                            var di = new FileInfo(_path);
                            if (di.Directory != null)
                            {
                                _eventArgs = new FileSystemEventArgs(WatcherChangeTypes.All, di.Directory.FullName,
                                    di.Name);
                            }
                        }
                        // 触发拷贝完成事件
                        OnCopyCompleted(this,_eventArgs);
                    }
                    catch (Exception)
                    {
                        // 说明文件被占用，拷贝未完成。
                        // 间隔时间后，再次比对
                        _timer.Change(_interval, Timeout.Infinite);
                    }
                }
            }
            catch (Exception ex)
            {
                if (CopyError != null)
                {
                    if (_eventArgs == null)
                    {
                        var di = new DirectoryInfo(_path);
                        if (di.Parent != null)
                        {
                            _eventArgs = new FileSystemEventArgs(WatcherChangeTypes.All, di.Parent.FullName, di.Name);
                        }
                    }
                    CopyError(_eventArgs, ex);
                }
            }
        }
    }
}
