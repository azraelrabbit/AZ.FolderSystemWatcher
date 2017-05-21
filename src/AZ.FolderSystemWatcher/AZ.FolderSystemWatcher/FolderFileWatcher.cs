using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AZ.IO.FileSystem
{
    public class FolderFileWatcher
    {
        private string _watchingFolder;

        private FileSystemWatcher folderWatcher;

        private List<FileCompleteWatcher> fileWatcher;

        private List<string> files = new List<string>();

        private List<FolderCompleteWatcher> subfolderWatcher;
        List<string> subFolders = new List<string>();

        //Synchronize event
        public event EventHandler<FolderFileEventArgs> WatchItemCompleted;

        //Asynchronous event ,the event subscriber run on separate thread.
        public event EventHandler<FolderFileEventArgs> WatchItemCompletedAsync;

        private ConcurrentDictionary<string, string> renameCacheList;

        public FolderFileWatcher(string watchRootPath)
        {
            _watchingFolder = watchRootPath;
            fileWatcher = new List<FileCompleteWatcher>();
            subfolderWatcher = new List<FolderCompleteWatcher>();
            renameCacheList=new ConcurrentDictionary<string, string>();
        }

        public void StartWatch()
        {
            folderWatcher = new FileSystemWatcher();
            folderWatcher.Path = _watchingFolder;
            folderWatcher.Filter = "*";//change from *.* to * to Compatible mono run on linux.

            folderWatcher.NotifyFilter = NotifyFilters.LastWrite| NotifyFilters.FileName | NotifyFilters.DirectoryName|NotifyFilters.LastAccess|NotifyFilters.Attributes;// | 
            folderWatcher.Renamed += FolderWatcher_Renamed;
            folderWatcher.Created += FolderWatcher_Created;
            folderWatcher.Changed += FolderWatcher_Changed;
            folderWatcher.Deleted += FolderWatcher_Deleted;

            
            folderWatcher.IncludeSubdirectories = true;
            folderWatcher.EnableRaisingEvents = true;

            //Console.WriteLine("start watching : {0}", _watchingFolder);
        }

        private void FolderWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            var oldPath = e.OldFullPath;
            if (renameCacheList.ContainsKey(oldPath.ToLower()))
            {
                //create new file/folder with right click and renaming item ,that only effective on less 5 second for typing name.
                renameCacheList[oldPath.ToLower()] = e.FullPath;
            }
            else
            {
                //this only effective in actual rename file/folder
                OnWatchItemCompleted(new FolderFileEventArgs() { FullPath = e.FullPath, WatchType = WatcherType.FileRename ,OldFullPath = e.OldFullPath});
                OnWatchItemCompletedAsync(new FolderFileEventArgs() { FullPath = e.FullPath, WatchType = WatcherType.FileRename, OldFullPath = e.OldFullPath });
            }
        }

        private void FolderWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            //Console.WriteLine("deleteing: {0}",e.FullPath);
            OnWatchItemCompleted(new FolderFileEventArgs() { FullPath = e.FullPath, WatchType = WatcherType.Delete });
            OnWatchItemCompletedAsync(new FolderFileEventArgs() { FullPath = e.FullPath, WatchType = WatcherType.Delete });
        }

        private void FolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
            //  Console.WriteLine("Creating: "+e.Name);

            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                //Console.WriteLine("deleted");
                return;
            }

            var path = e.FullPath;
            var watchType = WatcherType.FileCreate;
            if (IsDir(path) && !IsInSubFolder(path))
            {//creating folder

                renameCacheList.TryAdd(path.ToLower(),string.Empty);

                watchType = WatcherType.FolderCreate;
                subFolders.Add(path);

                var subfw =
                    new FolderCompleteWatcher(new WatcherItem() { FullPath = path, WatcherType = watchType }, 5000);// if the folder created, and 5 seconds passed the folder size not changed then the folder create completed.
                subfw.FolderCompleted += Subfw_FolderCompleted;
                subfw.Start();
                subfolderWatcher.Add(subfw);
            }
            if (IsInSubFolder(path))
            {// in creating folder 'file, do not care.

            }
            else if (IsSubFolderFile(path))
            {//if current file in exist subfolder ,do not care

            }
            else
            {
                renameCacheList.TryAdd(path.ToLower(), string.Empty);
                //single file  creating
                var fileWatchFype = WatcherType.FileCreate;
                // isReplace ? WatcherType.FileReplace : WatcherType.FileCreate;
                //Console.WriteLine(fileWatcher);
                if (!files.Contains(path))
                {
                    files.Add(path);

                    //  Console.WriteLine("changed file :" + path);
                    var fw = new FileCompleteWatcher(new WatcherItem() { FullPath = path, WatcherType = fileWatchFype });
                    fw.FileCompleted += Fw_FileCompleted;
                    fw.Start();
                    fileWatcher.Add(fw);
                }
            }

            // OnFolderCreated(new FolderFileEventArgs() {FullPath = path});

        }

        private void FolderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
          //   Console.WriteLine("changing:{0}",e.FullPath);
            var path = e.FullPath;
            // var isReplace = File.Exists(path);


            if (IsDir(path) && !IsInSubFolder(path))
            {
                //ignore this issue, do not care.
                // if folder copy 
                // Console.WriteLine("copying folder :" + path);

                //subFolders.Add(path);

                //var subfw =
                //    new FolderCompleteWatcher(new WatcherItem() { FullPath = path, WatcherType = WatcherType.FolderCopy });
                //subfw.FolderCompleted += Subfw_FolderCompleted;
                //subfw.Start();
                //subfolderWatcher.Add(subfw);
            }
            else if (IsInSubFolder(path))
            {// in copying folder 'file, do not care.

            }
            else if (IsSubFolderFile(path))
            {// if is file,and in existing subdir,then current is subfolder replacing
                var subDirItem = GetSubFolderItem(path);

                //  Console.WriteLine("replacing folder :" + subDirItem);

                subFolders.Add(subDirItem);

                var subfw =
                    new FolderCompleteWatcher(new WatcherItem() { FullPath = subDirItem, WatcherType = WatcherType.FolderReplace });
                subfw.FolderCompleted += Subfw_FolderCompleted;
                subfw.Start();
                subfolderWatcher.Add(subfw);
            }
            else
            {// single file copy

                var fileWatchFype = WatcherType.FileReplace;// isReplace ? WatcherType.FileReplace : WatcherType.FileCreate;
                //Console.WriteLine(fileWatcher);
                if (!files.Contains(path))
                {
                    files.Add(path);

                    //  Console.WriteLine("changed file :" + path);
                    var fw = new FileCompleteWatcher(new WatcherItem() { FullPath = path, WatcherType = fileWatchFype });
                    fw.FileCompleted += Fw_FileCompleted;
                    fw.Start();
                    fileWatcher.Add(fw);
                }
            }
        }

        bool IsSubFolderFile(string filePath)
        {
            var subDir = GetSubFolder(filePath);
            // Console.WriteLine(subDir);
            // Console.WriteLine(subDir.Equals(_watchingFolder));
            if (!subDir.Equals(_watchingFolder) && subDir.Contains(_watchingFolder))
            {
                return true;
            }

            return false;

        }

        string GetSubFolder(string filePath)
        {
            var fi = new FileInfo(filePath);
            return fi.Directory.FullName;
        }

        string GetSubFolderItem(string filePath)
        {
            var fi = new FileInfo(filePath);

            var di = fi.Directory;

            bool isRootItem = false;

            while (!isRootItem)
            {
                if (di.FullName.Equals(_watchingFolder))
                {
                    isRootItem = true;
                    continue;
                }
                var tdi = di.Parent;
                if (tdi.FullName.Equals(_watchingFolder))
                {
                    isRootItem = true;
                }
                else
                {
                    di = tdi;
                }
            }

            return di.FullName;

        }

        private void Subfw_FolderCompleted(object sender, FileCompleteEventArgs e)
        {

            try
            {
                if (subfolderWatcher.Exists(p => p.FwId == e.FwId))
                {
                    var fw = subfolderWatcher.FirstOrDefault(p => p.FwId.Equals(e.FwId));
                    subfolderWatcher.Remove(fw);
                }
            }
            catch { }

            try
            {
                if (subFolders.Contains(e.FileItem.FullPath))
                {
                    subFolders.Remove(e.FileItem.FullPath);
                }
            }
            catch { }

            var newPath = e.FileItem.FullPath;
            if (renameCacheList.ContainsKey(e.FileItem.FullPath.ToLower()))
            {
                //creating with rename operation
                renameCacheList.TryRemove(e.FileItem.FullPath.ToLower(), out newPath);

                if (string.IsNullOrEmpty(newPath))
                {
                    newPath = e.FileItem.FullPath;
                }
            }

            OnWatchItemCompleted(new FolderFileEventArgs() { FullPath = newPath, WatchType = e.FileItem.WatcherType });
            OnWatchItemCompletedAsync(new FolderFileEventArgs() { FullPath = newPath, WatchType = e.FileItem.WatcherType });
        }

        bool IsInSubFolder(string filePath)
        {
            var folder = subFolders.FirstOrDefault(p => filePath.Contains(p));
            if (string.IsNullOrEmpty(folder))
            {
                return false;
            }
            return true;
        }

        bool IsDir(string path)
        {
            //var fa = File.GetAttributes(path);
            //if ((fa & FileAttributes.Directory) != 0)
            //{
            //    return true;
            //}
            //return false;

            //new methods ,the old methods has some issue.
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
                return true;
            else
                return false;
        }

        private void Fw_FileCompleted(object sender, FileCompleteEventArgs e)
        {
            try
            {
                if (fileWatcher.Exists(p => p.FwId == e.FwId))
                {
                    var fw = fileWatcher.FirstOrDefault(p => p.FwId.Equals(e.FwId));
                    fileWatcher.Remove(fw);
                }
            }
            catch { }

            try
            {
                if (files.Contains(e.FileItem.FullPath))
                {
                    files.Remove(e.FileItem.FullPath);
                }
            }
            catch { }

            var newPath = e.FileItem.FullPath;
            if (renameCacheList.ContainsKey(e.FileItem.FullPath.ToLower()))
            {
                //creating with rename operation
                renameCacheList.TryRemove(e.FileItem.FullPath.ToLower(), out newPath);
                if (string.IsNullOrEmpty(newPath))
                {
                    newPath = e.FileItem.FullPath;
                }
            }

            OnWatchItemCompleted(new FolderFileEventArgs() { FullPath = newPath, WatchType = e.FileItem.WatcherType });

            OnWatchItemCompletedAsync(new FolderFileEventArgs() { FullPath = newPath, WatchType = e.FileItem.WatcherType });
        }

        public void StopWatch()
        {
            files = new List<string>();
            subfolderWatcher = new List<FolderCompleteWatcher>();
            subFolders = new List<string>();

            folderWatcher.Changed -= FolderWatcher_Changed;
            folderWatcher.Created -= FolderWatcher_Created;
            folderWatcher.Deleted -= FolderWatcher_Deleted;
            folderWatcher.Dispose();

            //Console.WriteLine("stop watch {0}", _watchingFolder);
        }

        protected virtual void OnWatchItemCompleted(FolderFileEventArgs e)
        {
            WatchItemCompleted?.Invoke(this, e);
        }

        protected virtual void OnWatchItemCompletedAsync(FolderFileEventArgs e)
        {
            if (WatchItemCompletedAsync != null)
            {
                var eventList = WatchItemCompletedAsync.GetInvocationList();
                foreach (EventHandler<FolderFileEventArgs> eventHandler in eventList)
                {
                    eventHandler.BeginInvoke(null, e, null, null);
                }
            }
           
        }
    }
}
