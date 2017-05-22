# AZ.FolderSystemWatcher
a filesystemwatcher wrapper only for watching folder or file 's  copy/create/replace when it really finished.

# Working With MONO on Linux [if you do not watching file/folder on CIFS with unc/samba ignore this]

  1. FilesystemWatcher working with mono is using inotify mode on linux default.
      but there are some issue to watching mounted path which filesystem is CIFS that mount from an unc/samba path by network,
      unfortunately,there has no way to resolve that issue,may be will fixed in nexe CIFS or not.
  2. the only way to make the filesystemwatcher working with mono on linux, to enable the mono default impl,not inotify.
     the mono default impl scan the watching target every 750 ms ( found from mono source code.). 
     and the default impl working normally, but there may be caused some performance issue or not.
     
     
      if you really want to enable watching on cifs mounted from unc/samba path, 
      There are two ways to do that :
    
      a. set linux environmentvariable  in shell command,before run your application:
      export MONO_MANAGED_WATCHER=1
       
      b. in your code before init filesystemwatcher to call :
        Environment.SetEnvironmentVariable ("MONO_MANAGED_WATCHER", "1");
   
    

# Nuget Gallery

https://www.nuget.org/packages/AZ.FolderSystemWatcher.Packager/
