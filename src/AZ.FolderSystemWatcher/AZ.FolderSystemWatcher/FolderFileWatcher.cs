using System;
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

        private List<string> files=new List<string>(); 

        private List<FolderCompleteWatcher> subfolderWatcher;
            List<string> subFolders=new List<string>();


        public event EventHandler<FolderFileEventArgs> FolderCreated;


        public event EventHandler<FolderFileEventArgs> FolderCopied;

        public event EventHandler<FolderFileEventArgs> FolderReplaced;

        public event EventHandler<FolderFileEventArgs> FileCreated;

        //public event EventHandler<FolderFileEventArgs> FileReplaced;
 

        public FolderFileWatcher(string watchRootPath)
        {
            _watchingFolder = watchRootPath;
            fileWatcher=new List<FileCompleteWatcher>();
            subfolderWatcher=new List<FolderCompleteWatcher>();
        }

        public void StartWatch()
        {
            folderWatcher=new FileSystemWatcher();
            folderWatcher.Path = _watchingFolder;
            folderWatcher.Filter = "*";

            folderWatcher.NotifyFilter=NotifyFilters.LastWrite|NotifyFilters.LastWrite;
            folderWatcher.Created += FolderWatcher_Created;
            folderWatcher.Changed += FolderWatcher_Changed;
            folderWatcher.IncludeSubdirectories = true;
            folderWatcher.EnableRaisingEvents = true;

            Console.WriteLine("start watching : {0}",_watchingFolder);
        }

        private void FolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
          //  Console.WriteLine("Creaed: "+e.Name);
            var path = e.FullPath;
            OnFolderCreated(new FolderFileEventArgs() {FullPath = path});
            
        }

        private void FolderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            var path = e.FullPath;
           // var isReplace = File.Exists(path);


            if (IsDir(path) && !IsInSubFolder(path))
            {
                // if folder copy 
               // Console.WriteLine("copying folder :" + path);

                subFolders.Add(path);

                var subfw =
                    new FolderCompleteWatcher(new WatcherItem() {FullPath = path, WatcherType = WatcherType.FolderCopy});
                subfw.FolderCompleted += Subfw_FolderCompleted;
                subfw.Start();
                subfolderWatcher.Add(subfw);
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
            {//单个文件复制

                var fileWatchFype = WatcherType.FileCreate;// isReplace ? WatcherType.FileReplace : WatcherType.FileCreate;
                //Console.WriteLine(fileWatcher);
                if (!files.Contains(path))
                {
                    files.Add(path);

                  //  Console.WriteLine("changed file :" + path);
                    var fw = new FileCompleteWatcher(new WatcherItem() {FullPath = path,WatcherType = fileWatchFype });
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
            if (!subDir.Equals(_watchingFolder)&& subDir.Contains(_watchingFolder))
            {
                return true;
            }

            return false;

        }

        string GetSubFolder(string filePath)
        {
            var fi=new FileInfo(filePath);
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
          //  Console.WriteLine("folder {0} completed: {1}.", e.FileItem.WatcherType,e.FileItem.FullPath);

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


            if (e.FileItem.WatcherType == WatcherType.FolderCopy)
            {
                OnFolderCopied(new FolderFileEventArgs() {FullPath = e.FileItem.FullPath});
            }
            else if(e.FileItem.WatcherType==WatcherType.FolderReplace)
            {
                OnFolderReplaced(new FolderFileEventArgs() { FullPath = e.FileItem.FullPath });
            }
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
            var fa = File.GetAttributes(path);
            if ((fa & FileAttributes.Directory) != 0)
            {
                return true;
            }
            return false;
        }

        private void Fw_FileCompleted(object sender, FileCompleteEventArgs e)
        {
         //   Console.WriteLine("file {0} completed: {1}.", e.FileItem.WatcherType, e.FileItem.FullPath);
           // Console.WriteLine("file completed: {0}.",e.FileItem.FullPath);

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

            OnFileCreated(new FolderFileEventArgs() {FullPath = e.FileItem.FullPath});

        }

        public void StopWatch()
        {
            folderWatcher.Changed -= FolderWatcher_Changed;
            folderWatcher.Created -= FolderWatcher_Created;

            folderWatcher.Dispose();

            Console.WriteLine("stop watch {0}",_watchingFolder);
        }

        protected virtual void OnFolderCreated(FolderFileEventArgs e)
        {
            FolderCreated?.Invoke(this, e); 
        }

        protected virtual void OnFolderReplaced(FolderFileEventArgs e)
        {
            FolderReplaced?.Invoke(this, e);
        }

        protected virtual void OnFileCreated(FolderFileEventArgs e)
        {
            FileCreated?.Invoke(this, e);
        }

        protected virtual void OnFolderCopied(FolderFileEventArgs e) 
        {
            FolderCopied?.Invoke(this, e);
        }
    }
}
