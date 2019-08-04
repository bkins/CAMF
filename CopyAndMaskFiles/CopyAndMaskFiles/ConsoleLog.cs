using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;

static class ConsoleLog
{
    private static readonly string  LOG_FILE_NAME       = "LogFile.txt";
    private static readonly string  BACKUP_FILE_PATTERN = "LogFile.*.txt";
    private static readonly long    LOG_FILE_THRESHOLD  = 1000000; //in bytes
    private static readonly string  SWITCH_ON           = "ON";
    private static readonly string  SWITCH_OFF          = "OFF";
    private static readonly string  SWITCH_TRUE         = "T";
    private static readonly string  SWITCH_FALSE        = "F";

    public static bool ShouldLogToScreen { get; set; }
    public static bool ShouldWriteToFile { get; set; }
    public static bool IsManualRun       { get; set; }

    public static ConsoleColor SuccessColor      = ConsoleColor.Green;
    public static ConsoleColor WarningColor      = ConsoleColor.Yellow;
    public static ConsoleColor FailureColor      = ConsoleColor.Red;
    public static string       FullPathToLogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LOG_FILE_NAME);

    private static readonly StringBuilder EntireLog = new StringBuilder();

    private static readonly ConsoleSpinner Spinner = new ConsoleSpinner();

    static ConsoleLog()
    {
        //Defaulted to 'true' to ensure logging takes place
        ShouldLogToScreen = true;
        ShouldWriteToFile = true;
        
        IsManualRun = false;
    }

    public static void Initialize()
    {
        WriteLine("***********************************************************", LoggingFlags.Normal);
        WriteLine($"Program stated at {DateTime.Now}");
        CleanupLogFile();
    }

    public static void SetOptionsFromArguments(string writeToScreen,
                                               string writeToFile, 
                                               string manualRun)
    {
        ShouldLogToScreen = writeToScreen.ToUpper() == SWITCH_ON;
        ShouldWriteToFile = writeToFile.ToUpper()   == SWITCH_ON;
        IsManualRun       = manualRun.ToUpper()     == SWITCH_TRUE;
    }
    
    public static void CleanupLogFile()
    {
        RemoveLogsThatAreTooOld();
        BackupAndDeleteLogFileIfTooLarge();
    }

    public static void WriteLine(string message,
                                 LoggingFlags fontColorFlag = LoggingFlags.Normal,
                                 string moreDetails = "")
    {
        WriteToLog(line:          true,
                   message:       message,
                   fontColorFlag: fontColorFlag,
                   moreDetails:   moreDetails);
    }

    public static void Write(string message,
                             LoggingFlags fontColorFlag,
                             string moreDetails = "")
    {
        WriteToLog(line:          false,
                   message:       message,
                   fontColorFlag: fontColorFlag,
                   moreDetails:   moreDetails);
    }

    private static void RemoveLogsThatAreTooOld()
    {
        DirectoryInfo   folder               = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        FileInfo[]      logFiles             = folder.GetFiles(BACKUP_FILE_PATTERN, SearchOption.TopDirectoryOnly);
        int             numberOfFilesDeleted = 0;

        foreach (var logFile in from   logFile in logFiles
                                where  logFile.LastWriteTime <= DateTime.Now.AddMonths(-3)
                                select logFile)
        {
            FileManager.DeleteFile(logFile.FullName);
            numberOfFilesDeleted++;
        }

        ConsoleLog.WriteLine($"Deleted {numberOfFilesDeleted} old log files that were as old, or older than, {DateTime.Now.AddMonths(-3)}");
    }   

    private static void BackupAndDeleteLogFileIfTooLarge()
    {
        if (IsLogFileGettingTooLarge())
        {
            ConsoleLog.WriteLine($"Backing up and deleting log file because it is getting too large (>{LOG_FILE_THRESHOLD} bytes).", LoggingFlags.Warning);

            var logFileInfo     = new FileInfo(FullPathToLogFile);
            var backupFileName  = GetLogFileBackupName(logFileInfo);

            FileManager.CopyFilesWithNewName(new List<FileInfo> { logFileInfo },
                                             logFileInfo.Directory.FullName,
                                             backupFileName);

            FileManager.DeleteFile(FullPathToLogFile);
        }
    }

    private static void WriteToLog(bool          line,
                                   string        message,
                                   LoggingFlags  fontColorFlag,
                                   string        moreDetails = "")
    {
        if (ShouldLogToScreen)
        {
            SetConsoleTextColor(fontColorFlag);

            ConsoleWrite(line, message);

            WriteToLogFile(message);

            Console.ResetColor();

            LogMoreDetails(moreDetails);
        }
    }

    private static void LogMoreDetails(string moreDetails)
    {
        if (moreDetails != string.Empty)
        {
            string details = $"{"\t"} ({moreDetails})";

            Console.WriteLine(details);
            EntireLog.AppendLine(details);
            WriteToLogFile(details);
        }
    }

    public static void WriteToFile(string message)
    {
        using (StreamWriter logFile = new StreamWriter(FullPathToLogFile, true))
        {
            logFile.Write($"{message}{Environment.NewLine}");
        }
    }
    private static void WriteToLogFile(string message)
    {
        if (ShouldWriteToFile)
        {
            WriteToFile(message);
        }
    }

    private static bool IsLogFileGettingTooLarge()
    {
        FileInfo logFile = FileManager.CreateNonExistingFile(FullPathToLogFile);

        return logFile.Length > LOG_FILE_THRESHOLD;
    }

    private static string GetLogFileBackupName(FileInfo logFileInfo)
    {   
        //Changes "LogFile.txt" to "LogFile.<DateTime>.txt"
        string fileName = logFileInfo
                          .Name
                          .Replace(LOG_FILE_NAME,
                                   LOG_FILE_NAME.Replace(".txt",
                                                         $".{DateTime.Now}.txt"));
        
        //Removes special chars ('/' and ':' from date and time, respectfully)
        return fileName.Replace("/", "").Replace(":", "");
    }

    private static void ConsoleWrite(bool line, string message)
    {
        if(line)
        {
            Console.WriteLine(message);
            EntireLog.AppendLine(message);
        }
        else
        {
            Console.Write(message);
            EntireLog.Append(message);
        }

        if(IsManualRun)
        {
            SetConsoleTextColor(LoggingFlags.Normal);
            
            Console.WriteLine("");
            Console.WriteLine("Press enter to continue");
            Console.ReadLine();

            SetConsoleTextColor(LoggingFlags.Normal);
        }
    }

    public static void WriteLongRunningProgressSpinner()
    {
        Spinner.Turn();
    }

    private static void SetConsoleTextColor(LoggingFlags logFlag)
    {
        switch (logFlag)
        {
            case LoggingFlags.Success:

                Console.ForegroundColor = SuccessColor;

                break;

            case LoggingFlags.Failure:

                Console.ForegroundColor = FailureColor;

                break;

            case LoggingFlags.Warning:

                Console.ForegroundColor = WarningColor;

                break;

            case LoggingFlags.Normal:

                Console.ResetColor();
                break;

            default:
                break;
        }
    }

    public enum LoggingFlags
    {
        Success,
        Failure,
        Warning,
        Normal
    }
}

public class ConsoleSpinner
{
    int _counter;
    private string _spinnerParts = @"/-\|";

    public ConsoleSpinner()
    {
        _counter = 0;
    }
    public void Turn()
    {
        _counter++;

        var index = _counter % _spinnerParts.Length;

        Console.Write(_spinnerParts[index]);

        //switch ()
        //{
        //    case 0: Console.Write(_spinnerParts);  break;
        //    case 1: Console.Write("-");  break;
        //    case 2: Console.Write(@"\"); break;
        //    case 3: Console.Write("|");  break;
        //}
        Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
    }
}
