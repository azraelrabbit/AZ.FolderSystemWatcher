using System.ComponentModel;

namespace AZ.FolderSystemWatcher.Next
{
    public class AZFileSystemWatcherPoll
    {
        /// <summary>
        ///   当在指定 <see cref="P:System.IO.FileSystemWatcher.Path" /> 中创建文件和目录时发生。
        /// </summary>

        public event FileSystemEventHandler Created;

        /// <summary>
        ///   删除指定 <see cref="P:System.IO.FileSystemWatcher.Path" /> 中的文件或目录时发生。
        /// </summary>

        public event FileSystemEventHandler Deleted;

        /// <summary>
        ///   当 <see cref="T:System.IO.FileSystemWatcher" /> 的实例无法继续监视更改或内部缓冲区溢出时发生。
        /// </summary>
        [Browsable(false)]
        public event ErrorEventHandler Error;

        /// <summary>
        ///   重命名指定 <see cref="P:System.IO.FileSystemWatcher.Path" /> 中的文件或目录时发生。
        /// </summary>

        public event RenamedEventHandler Renamed;

        /// <summary>Occurs when a file or directory in the specified <see cref="P:System.IO.FileSystemWatcher.Path"></see> is changed.</summary>
        /// <returns></returns>
        public event FileSystemEventHandler Changed;

        private List<FileWatchItem> _fileList;

        private string _path;

        private int _milSeconds = 1000;

        public bool Enabled { get; set; }

        private bool _firstInit = true;

        public AZFileSystemWatcherPoll(string path, int intervalMilseconds = 1000)
        {
            _fileList = new List<FileWatchItem>();
            _path = path;
            _milSeconds = intervalMilseconds;
        }

        public void Start()
        {
            Enabled = true;
            IntervalFiles();
        }

        public void Stop()
        {
            Enabled = false;
        }

        private void IntervalFiles()
        {
            Task.Run(() =>
            {
                while (Enabled)
                {
                    try
                    {
                        var d = new DirectoryInfo(_path);

                        var files = d.GetFiles().Where(p => !Utility.IsDir(p)).ToList();
                        //var files = d.GetFiles().ToList();

                        var hashList = new List<FileWatchItem>();

                        foreach (var fileInfo in files)
                        {
                            var stampHash = GetFileIdentity(fileInfo);// Helper.ComputeFileHash(fileInfo.FullName);////  get hash code for file.
                            var path = fileInfo.FullName;
                            var hashKey = Utility.ComputeMd5(path);

                            hashList.Add(new FileWatchItem()
                            {
                                HashKey = hashKey,
                                Path = path,
                                StampHash = stampHash
                            });
                        }

                       
                        //process directory

                        //var dirhashList=new List<FileWatchItem>();
                        var dirlist = d.GetDirectories();

                        foreach (var dirInfo in dirlist)
                        {
                            var stampHash = GetFolderIdentity(dirInfo);// Helper.ComputeFileHash(fileInfo.FullName);////  get hash code for file.
                            var path = dirInfo.FullName;
                            var hashKey = Utility.ComputeMd5(path);

                            hashList.Add(new FileWatchItem()
                            {
                                HashKey = hashKey,
                                Path = path,
                                StampHash = stampHash
                            });
                        }


                        if (_firstInit)
                        {
                            _fileList.AddRange(hashList);
                            _firstInit = false;
                          
                        }
                        else
                        {
 
                            var exists = new List<FileWatchItem>();
                            var created = new List<FileWatchItem>();
                            var deleted = new List<FileWatchItem>();
 
                            //find not exist, created file

                            exists = hashList.Where(p => _fileList.Exists(v => v.HashKey == p.HashKey)).ToList();

                            deleted = _fileList.Where(p => !hashList.Exists(v => v.HashKey == p.HashKey)).ToList();

                            created = hashList.Where(p => !_fileList.Exists(v => v.HashKey == p.HashKey)).ToList();

                            //created
                            foreach (var kp in created)
                            {
                                if (_fileList.Exists(v => v.StampHash == kp.StampHash))
                                { //rename
                                    var rnItem = _fileList.FirstOrDefault(p => p.StampHash == kp.StampHash);
                                    //_fileList.Remove(rnItem.Key);
                                    _fileList.Remove(rnItem);
                                    deleted.RemoveAll(p => p.HashKey == rnItem.HashKey);

                                    var oldName = rnItem.Path;
                                    var newName = kp.Path;

                                    _fileList.Add(kp);

                                    RenameFileItem(oldName, newName);

                                }
                                else
                                {
                                    NewFileItem(kp);
                                }
                            }

                            //delete
                            foreach (var kd in deleted)
                            {
                                DeleteFileItem(kd);

                            }


                            //changed
                            foreach (var ke in exists)
                            {
                                var oldItem = _fileList.FirstOrDefault(p => p.HashKey == ke.HashKey);// _fileList[ke.Key];
                                //Console.WriteLine($"kestamp:{ke.StampHash}\t|\toldStamp:{oldItem.StampHash}\t|\tisEqule:{ke.StampHash==oldItem.StampHash}");
                                if (ke.StampHash != oldItem.StampHash)
                                {
                                    //changing
                                    ChangeFileItem(ke.HashKey, ke.Path);
                                    oldItem.StampHash = ke.StampHash;
                                }
                                //}
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                   ;

                    }
                    // Console.WriteLine($"watching:][deleted:{deleted.Count}][created:{created.Count}][exists:{exists.Count}]");
 
                    //Thread.Sleep(_milSeconds);
                    Task.Delay(_milSeconds).Wait();

                    
                }
            });
        }

        private void DeleteFileItem(FileWatchItem kd)
        {
            _fileList.Remove(kd);

            var fi = new FileInfo(kd.Path);
            OnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, fi.DirectoryName, fi.Name));
        }

        private void RenameFileItem(string oldName, string newName)
        {


            var oldFi = new FileInfo(oldName);
            var newFi = new FileInfo(newName);
            OnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, oldFi.DirectoryName, newFi.Name, oldFi.Name));

        }

        private void NewFileItem(FileWatchItem fitem)
        {

            _fileList.Add(fitem);
            var fi = new FileInfo(fitem.Path);
            OnCreated(new FileSystemEventArgs(WatcherChangeTypes.Created, fi.DirectoryName, fi.Name));
        }
        private void ChangeFileItem(string key, string fullPath)
        {

            //_fileList.Add(key, fullPath);
            var fi = new FileInfo(fullPath);
            // OnCreated(new FileSystemEventArgs(WatcherChangeTypes.Created, fi.DirectoryName, fi.Name));
            OnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, fi.DirectoryName, fi.Name));
        }


        protected virtual void OnCreated(FileSystemEventArgs e)
        {
            //LogHelper.Debug("[ZW-POLL]-C:"+e.FullPath);
            Created?.Invoke(this, e);
        }

        protected virtual void OnDeleted(FileSystemEventArgs e)
        {
            Deleted?.Invoke(this, e);
        }

        protected virtual void OnError(ErrorEventArgs e)
        {
            Error?.Invoke(this, e);
        }

        protected virtual void OnRenamed(RenamedEventArgs e)
        {
            Renamed?.Invoke(this, e);

        }

        public string GetFileIdentity(FileInfo fileinfo)
        {
            var fileLength = fileinfo.Length.ToString();
            var fileCreateTime = fileinfo.LastAccessTime.Ticks.ToString();

            var key = fileLength + "[]" + fileCreateTime;

            //  Console.WriteLine(key);
            return key;
        }

        public string GetFolderIdentity(DirectoryInfo dirinfo)
        {
            //var fileLength = fileinfo.Length.ToString();
            var fileCreateTime = dirinfo.LastAccessTime.Ticks.ToString();

            var key = dirinfo.Name+"[]" + fileCreateTime;

            //  Console.WriteLine(key);
            return key;
        }

        protected virtual void OnChanged(FileSystemEventArgs e)
        {
            Changed?.Invoke(this, e);

        }
    }


    public class FileWatchItem
    {
        public string HashKey { get; set; }
        public string Path { get; set; }

        public string StampHash { get; set; }
    }


}
