using System.Collections.Concurrent;
 
namespace AZ.FolderSystemWatcher.Next
{
    /// <summary>
    /// 
    /// </summary>
    public class AZFileSystemWatcher : IDisposable
    {
        /// <summary>
        /// 系统监视器
        /// </summary>
        readonly FileSystemWatcher _watcher;

        private readonly AZFileSystemWatcherPoll _watcherPoll;

        /// <summary>
        /// 计时器集合
        /// </summary>
        readonly Dictionary<string, Timer> _dicChangeWatcherTimers;
        /// <summary>
        /// Checker集合
        /// </summary>
        readonly ConcurrentDictionary<string, AZFolderAndFileCopyCompletedChecker> _dicCopyCompletedCheckers;

        /// <summary>
        /// Created 事件处理程序
        /// </summary>
        /// <param name="sender">事件发出者</param>
        /// <param name="e">事件参数</param>
        public delegate void OnCreatedHandle(object sender, FileSystemEventArgs e);
        /// <summary>
        /// Created 事件
        /// </summary>
        public event OnCreatedHandle Created;
        /// <summary>
        /// Changed事件处理程序
        /// </summary>
        /// <param name="sender">事件发出者</param>
        /// <param name="e">事件参数</param>
        public delegate void OnChangedHandle(object sender, FileSystemEventArgs e);
        /// <summary>
        /// Changed事件
        /// </summary>
        public event OnChangedHandle Changed;
        /// <summary>
        /// Renamed事件处理程序
        /// </summary>
        /// <param name="sender">事件发出者</param>
        /// <param name="e">事件参数</param>
        public delegate void OnRenamedHandle(object sender, RenamedEventArgs e);
        /// <summary>
        /// Renamed事件处理程序
        /// </summary>
        public event OnRenamedHandle Renamed;
        /// <summary>
        /// 删除事件处理程序
        /// </summary>
        /// <param name="sender">事件发出者</param>
        /// <param name="e">事件参数</param>
        public delegate void OnDeletedHandle(object sender, FileSystemEventArgs e);
        /// <summary>
        /// 删除事件处理程序
        /// </summary>
        public event OnDeletedHandle Deleted;

        /// <summary>
        /// 过滤器
        /// </summary>
        public string Filter
        {
            get
            {
                return _watcher?.Filter;
            }
            set
            {
                if (_watcher != null)
                {
                    _watcher.Filter = value;
                }

            }
        }
        /// <summary>
        /// 是否启用此组件
        /// </summary>
        public bool EnableRaisingEvents
        {
            get
            {
                if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    return _watcherPoll.Enabled;
                }
                else
                {
                    return _watcher.EnableRaisingEvents;
                }
            }
            set
            {
                if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    if (value)
                    {
                        _watcherPoll.Start();
                    }
                    else
                    {
                        _watcherPoll.Stop();
                    }

                }
                else
                {
                    _watcher.EnableRaisingEvents = value;
                }
            }
        }

        /// <summary>
        /// 是否包含子文件夹
        /// </summary>
        public bool IncludeSubdirectories
        {
            get
            {
                return (bool) _watcher?.IncludeSubdirectories;
            }
            set
            {
                if (_watcher != null)
                {
                    _watcher.IncludeSubdirectories = value;
                }
               
            }
        }
        /// <summary>
        /// 要监视的事件类型
        /// </summary>
        public NotifyFilters NotifyFilter
        {
            get
            {
                if (_watcher != null)
                {
                    return _watcher.NotifyFilter;
                }
                else
                {
                    return NotifyFilters.FileName;
                }

            }
            set
            {
                if (_watcher != null)
                {
                    _watcher.NotifyFilter = value;
                }
                
            }
        }

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }


        /// <summary>
        /// AirBox File System 监视器
        /// </summary>
        /// <param name="path">要监视的路径</param>
        public AZFileSystemWatcher(string path)
        {
            _dicChangeWatcherTimers = new Dictionary<string, Timer>();
            _dicCopyCompletedCheckers = new ConcurrentDictionary<string, AZFolderAndFileCopyCompletedChecker>();
            //<string, ZPFolderAndFileCopyCompletedChecker>();

            if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
            {

                _watcherPoll = new AZFileSystemWatcherPoll(path, 2000);
                _watcherPoll.Created += _watcher_Created;
                _watcherPoll.Changed += _watcher_Changed;
                _watcherPoll.Renamed += _watcher_Renamed;
                _watcherPoll.Deleted += _watcher_Deleted;
            }
            else
            {
                _watcher = new FileSystemWatcher(path);
                _watcher.Created += _watcher_Created;
                _watcher.Changed += _watcher_Changed;
                _watcher.Renamed += _watcher_Renamed;
                _watcher.Deleted += _watcher_Deleted;
            }

        }

        private void _watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            Deleted?.Invoke(sender, e);
        }

        private void _watcher_Renamed(object sender, RenamedEventArgs e)
        {
            Renamed?.Invoke(sender, e);
        }

        /// <summary>
        /// Changed事件。屏蔽了一秒内的多次触发。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (Changed == null) return;
            // 如果没有该路径的计时器
            if (!_dicChangeWatcherTimers.ContainsKey(e.FullPath))
            {
                // 添加新的计时器
                _dicChangeWatcherTimers.Add(e.FullPath, new Timer(ChangeHandle, e, 1000, Timeout.Infinite));
            }
            else
            {
                // 改变计时器至1秒后触发
                _dicChangeWatcherTimers[e.FullPath].Change(1000, Timeout.Infinite);
            }
        }

        /// <summary>
        /// 处理Created事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void _watcher_Created(object sender, FileSystemEventArgs e)
        {
            if (Created == null) return;
            if (_dicCopyCompletedCheckers.ContainsKey(e.FullPath)) return;

            // 创建Checker对象
            var checker = new AZFolderAndFileCopyCompletedChecker(e);
            checker.Priority = Priority;
            // 设置Checker对象的完成事件
            checker.CopyCompleted += Checker_CopyCompleted;
            // 设置Checker对象的错误事件
            checker.CopyError += Checker_CopyError;
            _dicCopyCompletedCheckers.TryAdd(e.FullPath, checker);
        }

        /// <summary>
        /// 拷贝错误事件处理
        /// </summary>
        /// <param name="e"></param>
        /// <param name="ex"></param>
        private void Checker_CopyError(FileSystemEventArgs e, Exception ex)
        {
            _dicCopyCompletedCheckers.TryRemove(e.FullPath,out var removeItem);
            
            //LogHelper.Error(e.FullPath,ex);
        }

        /// <summary>
        /// 拷贝完成事件，触发Created事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void Checker_CopyCompleted(object sender, FileSystemEventArgs e)
        {
            _dicCopyCompletedCheckers.TryRemove(e.FullPath,out var removeItem);//.Remove(e.FullPath);
            Created?.Invoke(this, e);
        }

        /// <summary>
        /// 触发Changed事件
        /// </summary>
        /// <param name="state"></param>
        private void ChangeHandle(object state)
        {
            var e = (FileSystemEventArgs)state;
            // 释放监视器
            _dicChangeWatcherTimers[e.FullPath].Dispose();
            // 移除计时器
            _dicChangeWatcherTimers.Remove(e.FullPath);
            Changed?.Invoke(this, e);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            try
            {
                _dicChangeWatcherTimers.Clear();
                _dicCopyCompletedCheckers.Clear();
                _watcher?.Dispose();

                _watcherPoll?.Stop();
            }
            catch
            {
                // ignored
            }
        }
    }
}
