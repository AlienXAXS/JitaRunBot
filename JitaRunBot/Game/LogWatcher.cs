using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace JitaRunBot.Game
{
    public delegate void OnFileChangedHandler(object source, FileContentsChanged e);
    public class FileContentsChanged : EventArgs
    {
        private string _fileContents;
        public FileContentsChanged(string FileContents)
        {
            _fileContents = FileContents;
        }
        public string GetInfo()
        {
            return _fileContents;
        }
    }

    internal class LogWatcher
    {
        private string _rootPath;
        private string _currentLogFile;
        private FileSystemWatcher _fileSystemWatcher;
        private FileSystemWatcher _directoryWatcher;

        private int lastLineRead;

        public event OnFileChangedHandler OnFileChanged;

        public LogWatcher()
        {
            _rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EVE", "logs", "Gamelogs");

            if (!Directory.Exists(_rootPath))
                throw new Exception($"EVE Log Path not found in {_rootPath}");

            _directoryWatcher = new FileSystemWatcher(_rootPath);
            _directoryWatcher.Created += (sender, args) =>
            {
                // Ignore directory creation
                var fileAttr = File.GetAttributes(args.FullPath);
                if (fileAttr.HasFlag(FileAttributes.Directory))
                    return;

                var previousLogFile = _currentLogFile;
                ConsoleUtil.WriteToConsole($"New File detected in gamelogs {args.Name}. Ensuring we are reading it if required.", ConsoleUtil.LogLevel.INFO, ConsoleColor.Yellow);
                if ( LocateCurrentLogFile() )
                {
                    if ( previousLogFile != _currentLogFile )
                    {
                        ConsoleUtil.WriteToConsole(" -> Reading this file now.", ConsoleUtil.LogLevel.INFO, ConsoleColor.Yellow);
                    } else {
                        ConsoleUtil.WriteToConsole(" -> Not required, skipping.", ConsoleUtil.LogLevel.INFO, ConsoleColor.Yellow);
                    }
                }
            };
            _directoryWatcher.EnableRaisingEvents = true;
        }

        public string GetCurrentFileName()
        {
            return _currentLogFile;
        }

        internal void Dispose()
        {
            if (_fileSystemWatcher != null)
            {
                _fileSystemWatcher.EnableRaisingEvents = false;
                _fileSystemWatcher.Dispose();
            }

            if (_directoryWatcher != null)
            {
                _directoryWatcher.EnableRaisingEvents = false;
                _directoryWatcher.Dispose();
            }
        }

        public bool LocateCurrentLogFile()
        {
            var files = GetFileInfos(_rootPath);
            var result = files.
                Where(x => x.Name.Count(y => y.Equals('_')) == 2).
                Where(x => x.LastWriteTime.Date == DateTime.Today.Date).
                OrderByDescending(x => x.LastWriteTime).
                ToList();

            foreach ( var file in result )
            {
                var fileContents = File.ReadAllLines(file.FullName);

                // Do we have an actual file here
                if ( fileContents.Count() > 2 )
                {
                    // Is it actually a EVE Log file?
                    if ( fileContents[0] == "------------------------------------------------------------" )
                    {
                        // See if we can get the correct pilot name
                        if ( fileContents[2].StartsWith("  Listener:") )
                        {
                            var pilotName = fileContents[2].Substring("  Listener:".Length+1);
                            if ( pilotName == Configuration.Handler.Instance.Config.PilotName )
                            {
                                // If we found the same file again, then ignore it.
                                if (_currentLogFile == file.FullName)
                                    return false;

                                if (_fileSystemWatcher != null)
                                    _fileSystemWatcher.Dispose();

                                _currentLogFile = file.FullName;
                                _fileSystemWatcher = new FileSystemWatcher(file.DirectoryName);
                                _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                                _fileSystemWatcher.EnableRaisingEvents = true;
                                lastLineRead = File.ReadAllLines(file.FullName).Count();
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (e.FullPath == _currentLogFile)
            {
                var allLines = File.ReadAllLines(_currentLogFile);
                foreach (var line in allLines.Skip(lastLineRead))
                {
                    OnFileChanged?.Invoke(this, new FileContentsChanged(line));
                }

                lastLineRead = lastLineRead + (allLines.Count() - lastLineRead);
            }
        }

        private List<FileInfo> GetFileInfos(string folderName)
        {
            List<FileInfo> files = new List<FileInfo>();
            DirectoryInfo directoryInfo = new DirectoryInfo(folderName);
            try
            {
                foreach (FileInfo f in directoryInfo.GetFiles())
                {
                    files.Add(f);
                }
                foreach (DirectoryInfo d in directoryInfo.GetDirectories())
                {
                    files.AddRange(GetFileInfos(d.FullName));
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
            return files;
        }
    }
}
