﻿using System;
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


        public event EventHandler<FolderFileEventArgs> WatchItemCompleted;

        public FolderFileWatcher(string watchRootPath)
        {
            _watchingFolder = watchRootPath;
            fileWatcher = new List<FileCompleteWatcher>();
            subfolderWatcher = new List<FolderCompleteWatcher>();
        }

        public void StartWatch()
        {
            folderWatcher = new FileSystemWatcher();
            folderWatcher.Path = _watchingFolder;
            folderWatcher.Filter = "*";

            folderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            folderWatcher.Created += FolderWatcher_Created;
            folderWatcher.Changed += FolderWatcher_Changed;
            folderWatcher.IncludeSubdirectories = true;
            folderWatcher.EnableRaisingEvents = true;

            Console.WriteLine("start watching : {0}", _watchingFolder);
        }

        private void FolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
            //  Console.WriteLine("Creating: "+e.Name);

            var path = e.FullPath;
            var watchType = WatcherType.FileCreate;
            if (IsDir(path))
            {//creating folder
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
            // Console.WriteLine("changing:{0}",e.FullPath);
            var path = e.FullPath;
            // var isReplace = File.Exists(path);


            if (IsDir(path) && !IsInSubFolder(path))
            {
                // if folder copy 
                // Console.WriteLine("copying folder :" + path);

                subFolders.Add(path);

                var subfw =
                    new FolderCompleteWatcher(new WatcherItem() { FullPath = path, WatcherType = WatcherType.FolderCopy });
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

            OnWatchItemCompleted(new FolderFileEventArgs() { FullPath = e.FileItem.FullPath, WatchType = e.FileItem.WatcherType });
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

            OnWatchItemCompleted(new FolderFileEventArgs() { FullPath = e.FileItem.FullPath, WatchType = e.FileItem.WatcherType });

        }

        public void StopWatch()
        {
            folderWatcher.Changed -= FolderWatcher_Changed;
            folderWatcher.Created -= FolderWatcher_Created;

            folderWatcher.Dispose();

            Console.WriteLine("stop watch {0}", _watchingFolder);
        }

        protected virtual void OnWatchItemCompleted(FolderFileEventArgs e)
        {
            WatchItemCompleted?.Invoke(this, e);
        }
    }
}
