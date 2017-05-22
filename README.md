# AZ.FolderSystemWatcher
a filesystemwatcher wrapper only for watching folder or file 's  copy/create/replace when it really finished.

# Working With MONO on Linux [if you do not watching file/folder on CIFS with unc/samba ignore this]

  1. FilesystemWatcher working with mono is using inotify mode on linux default.<br />
      but there are some issue to watching mounted path which filesystem is CIFS that mount from an unc/samba path by network,
      unfortunately,there has no way to resolve that issue,may be will fixed in nexe CIFS or not.<br />
  2. the only way to make the filesystemwatcher working with mono on linux, to enable the mono default impl,not inotify.<br />
     the mono default impl scan the watching target every 750 ms ( found from mono source code.). <br />
     and the default impl working normally, but there may be caused some performance issue or not.<br />
     <br />
     
      if you really want to enable watching on cifs mounted from unc/samba path, <br />
      There are two ways to do that :<br />
    <br />
      a. set linux environmentvariable  in shell command,before run your application:<br />
      export MONO_MANAGED_WATCHER=1<br />
       
      b. in your code before init filesystemwatcher to call :<br />
        Environment.SetEnvironmentVariable ("MONO_MANAGED_WATCHER", "1");<br />
   <br />
    

# Nuget Gallery

https://www.nuget.org/packages/AZ.FolderSystemWatcher.Packager/
