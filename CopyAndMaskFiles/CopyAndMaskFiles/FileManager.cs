using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security;
using System.Text;

public static class FileManager
{
    public static readonly string FAKE_FILE = "FakeData.txt";
    public static readonly  char   DELIMITER = '\t';

    public static bool IsValidDirectoryPath(string path)
    {
        try
        {
            //First let's see if the directory exists (if it exists, I would guess it is valid)
            if ( ! Directory.Exists(path))
            {
                //However, if it does not exist, let's just create it.  If that fails then we'll catch the error below
                Directory.CreateDirectory(path);
            }

            //exercising path in a variety of ways to determine if the path is valid.
            //This is not an complete way to be 100% sure that the path is correct, but 
            //it will catch some of the obvious problems
            var test0 = Path.GetDirectoryName(path);
            
            var test1 = new DirectoryInfo(path);
            var test2 = test1.Name;
            var test3 = test1.GetFileSystemInfos();
        }
        catch (ArgumentNullException ex)
        {
            ConsoleLog.WriteLine("Path is empty, contains only white spaces, or contains invalid characters."
                               , ConsoleLog.LoggingFlags.Failure
                               , $"DirectoryPath: {path}{Environment.NewLine}Complete error:{Environment.NewLine}{ex.ToString()}");

            return false;
        }
        catch (ArgumentException ex)
        {
            ConsoleLog.WriteLine("Path is empty, contains only white spaces, or contains invalid characters."
                               , ConsoleLog.LoggingFlags.Failure
                               , $"DirectoryPath: {path}{Environment.NewLine}Complete error:{Environment.NewLine}{ex.ToString()}");

            return false;
        }
        catch (PathTooLongException ex)
        {
            ConsoleLog.WriteLine("The specified path, file name, or both exceed the system - defined maximum length."
                               , ConsoleLog.LoggingFlags.Failure
                               , $"DirectoryPath: {path}{Environment.NewLine}Complete error:{Environment.NewLine}{ex.ToString()}");

            return false;
        }
        catch (SecurityException ex)
        {
            ConsoleLog.WriteLine("You may not have access to this directory"
                               , ConsoleLog.LoggingFlags.Failure
                               , $"DirectoryPath: {path}{Environment.NewLine}Complete error:{Environment.NewLine}{ex.ToString()}");

            return false;
        }
        catch (Exception ex)
        {
            ConsoleLog.WriteLine($"DirectoryPath: {path}{Environment.NewLine}Complete error:{Environment.NewLine}{ex.ToString()}"
                               , ConsoleLog.LoggingFlags.Failure);

            return false;
        }

        return true;
    }

    internal static IEnumerable<FileInfo> GetFilesToMask(List<FileInfo> missingFiles
                                                       , List<string> listOfFileNamePatterns)
    {
        return from   file    in missingFiles
               from   pattern in listOfFileNamePatterns
               where  file.FullName.Contains(pattern)
               select file;
    }

    /// <summary>
    /// Takes fake file info (reference to the file, not the contents),
    ///     fake file must be validated before passing it (should, at least, have column headers)
    /// Populates the fake file with missing info from missing files
    /// Reads from the fake file and populates the fake data dataTable
    /// And finally returns the datatable
    /// </summary>
    /// <param name="fakeFile"></param>
    /// <param name="realSsnColumnName"></param>
    /// <returns></returns>
    internal static Dictionary<string, Person> BuildFakeDataFile(string                  realSsnColumnName
                                                               , List<FileInfo> missingFiles
                                                               , List<string> filesToMaskSearchPattern
                                                               , Dictionary<string, int> fieldsToMask)
    {
        ConsoleLog.WriteLine("Building data for masking...");

        FileInfo fakeFile = GetFakeDataFile(realSsnColumnName);

        IEnumerable<FileInfo> filesToMask = GetFilesToMask(missingFiles
                                                         , filesToMaskSearchPattern);

        var files = filesToMask.ToList();

        //Ignores what is already stored and puts all people with fake data in file
        //Remove when FakeFile has been built correctly.
        //PopulateFakeFile(fieldsToMask
        //               , files
        //               , fakeFile
        //               , realSsnColumnName);

        //Inserts only people that have not already been added
        //Uncomment when:
        //  1) Cities are put in the right column.  Currently they are being put in the 'Residential Address Line 2' column
        //  2) Duplicate New SSN column is removed.  Currently there are two columns for the New SSN and the second column is blank.
        Dictionary<string, Person> people = PopulateDataFromNewPeopleIntoFakeFile(fieldsToMask 
                                                                                , files
                                                                                , fakeFile
                                                                                , realSsnColumnName);


        ConsoleLog.WriteLine("Masking data built."
                           , ConsoleLog.LoggingFlags.Success);

        return people;
    }

    private static FileInfo GetPersonFile(IEnumerable<FileInfo> filesToMask)
    {
        foreach (var realFile in filesToMask)
        {
            if (realFile.Name.ToUpper()
                        .Contains("PERSON"))
            {
                return realFile;
            }
        }

        var errorMessage = "No file for Person data was passed in as a parameter.";
        ConsoleLog.WriteLine(errorMessage, ConsoleLog.LoggingFlags.Failure);
        throw new ApplicationException(errorMessage);
    }

    private static Dictionary<string, Person> PopulateDataFromNewPeopleIntoFakeFile(Dictionary<string, int> fieldsToMask
                                                                                  , IEnumerable<FileInfo> filesToMask
                                                                                  , FileInfo fakeFileInfo
                                                                                  , string realSsnColumnName)
    {
        string[] linesOfPersonFile  = GetAllFileContents(GetPersonFile(filesToMask));
        string[] realColumns        = linesOfPersonFile[0].Split(DELIMITER);
        string   columnHeader       = GetFakeFileColumnHeaders(fieldsToMask
                                                             , fakeFileInfo);
        string[] linesOfFakeFile    = GetAllFileContents(fakeFileInfo);
        string[] fakeColumns        = linesOfFakeFile[0].Split(DELIMITER);
        var      people             = new Dictionary<string, Person>();

        for (var index = 1; index < linesOfPersonFile.Length-1; index++)
        {
            var aPerson = GetEachPerson(linesOfPersonFile,
                                        index,
                                        realSsnColumnName,
                                        realColumns,
                                        linesOfFakeFile,
                                        fakeColumns,
                                        fakeFileInfo);

            if(!people.ContainsKey(aPerson.Ssn))
            {
                people.Add(aPerson.Ssn, aPerson);
            }
        }
        return people;
    }

    private static Person GetEachPerson(string[] linesOfPersonFile,
                                        int      index,
                                        string   realSsnColumnName,
                                        string[] realColumns,
                                        string[] linesOfFakeFile,
                                        string[] fakeColumns,
                                        FileInfo fakeFileInfo)
    {
        Person aPerson = new Person();
        var personLine = linesOfPersonFile[index];

        if (IsLineValid(linesOfPersonFile))
        {
            var currentSsn = GetCurrentSsn(realSsnColumnName
                                         , realColumns
                                         , personLine);

            //look through the fake file and check if the SSN is in there
            var matchFound = HasPersonDataBeenMasked(realSsnColumnName
                                                  , linesOfFakeFile
                                                  , fakeColumns
                                                  , currentSsn);

            aPerson = InsertFakeDataToFile(fakeFileInfo
                                         , matchFound
                                         , currentSsn);
        }

        return aPerson;
    }

    private static void PopulateFakeFile(Dictionary<string, int> fieldsToMask
                                       , IEnumerable<FileInfo> filesToMask
                                       , FileInfo fakeFile
                                       , string realSsnColumnName)
    {
        string columnHeader = GetFakeFileColumnHeaders(fieldsToMask
                                                     , fakeFile);

        string[] linesOfFakeFile = GetAllFileContents(fakeFile);

        using (StreamWriter fakeFileStream = new StreamWriter(path: fakeFile.FullName
                                                            , append: true))
        {
            WriteColumnsToFile(columnHeader
                             , linesOfFakeFile[0].Trim()
                             , fakeFileStream);

            var people = new List<Person>();

            //Populate fake file
            foreach (var realFile in filesToMask)
            {
                if (realFile.Name.ToUpper().Contains("PERSON"))
                {
                    var fakeData = new Person();
                    var aPerson = new Person(fakeData);

                    string[] linesOfRealFile = GetAllFileContents(realFile);

                    string[] realColumns = linesOfRealFile[0].Split(DELIMITER);

                    for (int i = 1; i < linesOfRealFile.Length; i++)
                    {
                        var fakeLine = new StringBuilder();

                        var realLine = linesOfRealFile[i].Trim().Split(DELIMITER);

                        BuildFakeLine(fieldsToMask
                                    , fakeLine
                                    , realLine
                                    , realColumns
                                    , realSsnColumnName
                                    , aPerson);

                        fakeFileStream.WriteLine(aPerson.FakeData.ToString());
                    }
                }
            }
        }
    }

    private static string GetCurrentSsn(string   realSsnColumnName
                                      , string[] columns
                                      , string   record)
    {
        int ssnColumnIndex = GetColumnIndex(columns
                                          , realSsnColumnName);

        var currentSsn = record.Split(DELIMITER)[ssnColumnIndex];
        return currentSsn;

    }

    private static Person InsertFakeDataToFile(FileInfo fakeFileInfo
                                           , bool     matchFound
                                           , string   currentSsn)
    {
        if (matchFound 
         || String.IsNullOrEmpty(currentSsn.Trim()))
            return new Person();

        //Build fake data for person
        Person aFakePerson = BuildFakePersonData(currentSsn);

        //append data to fake file
        using (StreamWriter fakeFileWriter = new StreamWriter(fakeFileInfo.FullName
                                                            , append: true))
        {
            fakeFileWriter.WriteLine(aFakePerson.FakeData.ToString());
        }

        return aFakePerson;
    }

    private static bool HasPersonDataBeenMasked(string   realSsnColumnName
                                             , string[] linesOfFakeFile
                                             , string[] fakeColumns
                                             , string   currentSsn)
    {
        var matchFound = false;

        for (var index = 1; index < linesOfFakeFile.Length; index++)
        {
            var record = linesOfFakeFile[index];

            if (record.Trim() != String.Empty)
            {
                var currentFakeSsn = GetCurrentSsn(realSsnColumnName
                                                 , fakeColumns
                                                 , record);

                //If it is NOT, create fake data for person and append to fake file.
                //   Otherwise, continue to the next person
                if (currentFakeSsn == currentSsn)
                {
                    matchFound = true;
                }
            }
        }

        return matchFound;
    }

    private static Person BuildFakePersonData(string currentSsn)
    {
        var fakeData   = new Person();
        var fakePerson = new Person(fakeData);

        fakePerson.Ssn                     = currentSsn;
        fakePerson.FakeData.Ssn            = currentSsn;
        fakePerson.FakeData.FirstName      = RandomDatainator.GetFirstName(fakePerson.FakeData.MiddleName);
        fakePerson.FakeData.MiddleName     = RandomDatainator.GetMiddleName(fakePerson.FakeData.FirstName);
        fakePerson.FakeData.LastName       = RandomDatainator.GetLastName();
        fakePerson.FakeData.AddressLineOne = RandomDatainator.GetAddress();
        fakePerson.FakeData.City           = RandomDatainator.GetCity();

        return fakePerson;
    }

    private static void WriteColumnsToFile(string       columnHeader
                                         , string       fakeColumnHeader
                                         , StreamWriter fakeFileStream)
    {
        if (fakeColumnHeader.Length == 0)
        {
            WriteColumnHeadersToFile(columnHeader
                                   , fakeFileStream);
        }
    }

    private static string BuildFakeLine(Dictionary<string, int> fieldsToMask
                                      , StringBuilder           fakeLine
                                      , string[]                realLine
                                      , string[]                realColumns
                                      , string                  realSsnColumnName
                                      , Person                  aPerson)
    {
        var columnIndex = GetColumnIndex(realColumns
                                       , realSsnColumnName);
        
        foreach (var column in fieldsToMask)
        {
            if (IsLineValid(realLine
                          , realColumns
                          , column.Key))
            {
                try
                {
                    columnIndex = GetColumnIndex(realColumns
                                               , column.Key); //fieldsToMask[column.Key];

                    BuildPersonsData(fakeLine
                                   , realLine
                                   , column
                                   , columnIndex
                                   , aPerson);
                }
                catch
                {
                    ConsoleLog.WriteLine("ugh!"
                                       , ConsoleLog.LoggingFlags.Failure);
                }
            }
        }

        return fakeLine.ToString();
    }

    private static bool IsLineValid(string[] realLine)
    {
        return realLine.Length    > 0
            && realLine[0]        != "---End of File---"
            && realLine[0].Trim() != String.Empty;
    }

    private static bool IsLineValid(string[] realLine
                                  , string[] realColumns
                                  , string   columnName)
    {
        return realColumns.Contains(columnName
                                  , StringComparer.OrdinalIgnoreCase)
            && IsLineValid(realLine);
    }

    private static void BuildPersonsData(StringBuilder             fakeLine
                                       , string[]                  realLine
                                       , KeyValuePair<string, int> column
                                       , int                       columnIndex
                                       , Person                    aPerson)
    {
        var thisColumn = column.Key.ToUpper();
        var realValue  = $"{realLine[columnIndex]}";

        //TODO:  Find a better way to do this:
        if      (thisColumn == "PERSON FIRST NAME")
        {
            SetFirstName(fakeLine
                       , aPerson
                       , realValue);
        }
        else if (thisColumn == "PERSON MIDDLE NAME")
        {
            SetMiddleName(fakeLine
                        , aPerson
                        , realValue);
        }
        else if (thisColumn == "PERSON LAST NAME")
        {
            SetLastName(fakeLine
                      , aPerson
                      , realValue);
        }
        else if (thisColumn == "RESIDENCE ADDRESS LINE ONE TEXT"
              || thisColumn == "RESIDENCE ADDRESS LINE 1")
        {
            SetAddress(fakeLine
                     , aPerson
                     , realValue);
        }
        else if (thisColumn == "RESIDENCE ADDRESS CITY NAME")
        {
            SetCity(fakeLine
                  , aPerson
                  , realValue);
        }
        else if (thisColumn == "NEW SOCIAL SECURITY NUMBER")
        {
            SetSsn(fakeLine
                 , aPerson
                 , realValue);
        }
    }

    private static void SetCity(StringBuilder fakeLine
                              , Person        aPerson
                              , string        realValue)
    {
        aPerson.FakeData.City = RandomDatainator.GetCity();
        aPerson.City          = realValue;

        //fakeLine.Append($"{aPerson.FakeData.City}\t");
    }

    private static void SetAddress(StringBuilder fakeLine
                                 , Person        aPerson
                                 , string        realValue)
    {
        aPerson.FakeData.AddressLineOne = RandomDatainator.GetAddress();
        aPerson.AddressLineOne          = realValue;

        //fakeLine.Append($"{aPerson.FakeData.AddressLineOne}\t");
    }

    private static void SetLastName(StringBuilder fakeLine
                                  , Person        aPerson
                                  , string        realValue)
    {
        aPerson.FakeData.LastName = RandomDatainator.GetLastName();
        aPerson.LastName          = realValue;

        //fakeLine.Append($"{aPerson.FakeData.LastName}\t");
    }

    private static void SetMiddleName(StringBuilder fakeLine
                                    , Person        aPerson
                                    , string        realValue)
    {
        aPerson.FakeData.MiddleName = RandomDatainator.GetMiddleName(aPerson.FakeData.FirstName);
        aPerson.MiddleName          = realValue;

        //fakeLine.Append($"{aPerson.FakeData.MiddleName}\t");
    }

    private static void SetFirstName(StringBuilder fakeLine
                                   , Person        aPerson
                                   , string        realValue)
    {
        aPerson.FakeData.FirstName = RandomDatainator.GetFirstName(aPerson.FakeData.MiddleName);
        aPerson.FirstName          = realValue;

        //fakeLine.Append($"{aPerson.FakeData.FirstName}\t");
    }

    private static void SetSsn(StringBuilder fakeLine
                             , Person aPerson
                             , string        realValue)
    {
        aPerson.FakeData.Ssn = realValue;
        aPerson.Ssn          = realValue;

        //fakeLine.Append(aPerson.FakeData.Ssn);
    }

    private static int GetColumnIndex(string[] realColumns
                                    , string   columnName)
    {
        int columnIndex = 0;

        for (int i = 0; i < realColumns.Length; i++)
        {
            if (realColumns[i].ToUpper() == columnName.ToUpper())
            {
                columnIndex = i;
            }
        }

        return columnIndex;
    }

    private static void WriteColumnHeadersToFile(string       columnHeader
                                               , StreamWriter fakeFileStream)
    {
        if (columnHeader != String.Empty) //FakeFile was just created
        {
            fakeFileStream.WriteLine($"{columnHeader}");
        }
    }

    private static string GetFakeFileColumnHeaders(Dictionary<string, int> fieldsToMask
                                                 , FileInfo                fakeFile)
    {
        string columnHeader = String.Empty;

        if (fakeFile.Length == 0) //FakeFile does not exist (and just created and FakeFile is empty)
        {
            foreach (KeyValuePair<string, int> field in fieldsToMask)
            {
                columnHeader += $"{field.Key}\t";
            }

            columnHeader = columnHeader.Trim();

            WriteColumnHeadersToFile(fakeFile
                                   , columnHeader);
        }

        return columnHeader;
    }

    private static void WriteColumnHeadersToFile(FileInfo fakeFile
                                               , string   columnHeader)
    {
        using (StreamWriter file = new StreamWriter(fakeFile.FullName))
        {
            file.WriteLine(columnHeader);
        }
    }

    private static string[] GetAllFileContents(FileInfo file)
    {
        string fileContents;

        using (StreamReader fileReader = new StreamReader(file.FullName))
        {
            fileContents = fileReader.ReadToEnd();
        }

        return fileContents.Split(Environment.NewLine);
    }

    private static string GetFileContents(string path)
    {
        string fileContents;

        using (var file = new StreamReader(path))
        {
            fileContents = file.ReadToEnd();
        }
        return fileContents;
    }

    internal static List<string> ReadFileToList(string filePath)
    {
        List<string> listOfFileContents = new List<string>();
        string       fileContents       = GetFileContents(filePath);

        foreach (var line in fileContents.Split(Environment.NewLine))
        {
            listOfFileContents.Add(line);
        }

        return listOfFileContents;
    }

    internal static FileInfo GetFakeDataFile(string realSsnColumnName)
    {
        //Check if file exists
        //  Does not, create it
        //  If so validate structure.  Columns should be
        //      1)  Name of Real SSN column
        //      2)  Generated unique number for person (Generate if new person)
        //      3)  Each column name to be masked

        //Does file exists
        FileInfo fakeFile = CreateNonExistingFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory
                                                             , FAKE_FILE));

        if (!(fakeFile.Length > 0))
        {
            //Validate/build file structure
        }

        return fakeFile;
    }

    /// <summary>
    /// Identify if there are files in the source that are not in the destination
    /// </summary>
    /// <param name="source">Path to the source folder</param>
    /// <param name="destination">Path to the destination folder</param>
    /// <returns>True, if there are files in the source that are not in the destination.</returns>
    public static List<FileInfo> GetSourceFilesNotInDestination(string source
                                                              , string destination)
    {
        ConsoleLog.WriteLine("In Source:");
        IEnumerable<FileInfo> sourceFiles = GetAllFiles(source);

        ConsoleLog.WriteLine("In Destination:");
        IEnumerable<FileInfo> destinationFiles = GetAllFiles(destination);

        var listOfMissingFiles = GetMissingFiles(sourceFiles
                                               , destinationFiles);

        LogMissingFiles(sourceFiles
                      , destinationFiles
                      , listOfMissingFiles);

        return listOfMissingFiles;
    }

    /// <summary>
    /// Missing files are defined as in the source location and not in the Destination.  
    /// So the list of missing files has the sourece paths.
    /// This method swaps the source to the destination path for each file
    /// </summary>
    /// <param name="listOfMissingFiles">List of files that have the Source location</param>
    /// <param name="destinationFolder">The Destination location that the missing files should have</param>
    /// <returns></returns>
    public static List<FileInfo> SetMissingFilesToDestinationFolder(List<FileInfo> listOfMissingFiles
                                                                  , string         destinationFolder)
    {
        var newListOfMissingFiles = new List<FileInfo>();

        foreach (var missingFile in listOfMissingFiles)
        {
            newListOfMissingFiles.Add(new FileInfo(Path.Combine(destinationFolder
                                                              , missingFile.Name)));
        }

        return newListOfMissingFiles;
    }

    public static bool CopyFilesWithNewName(List<FileInfo> sourceFiles
                                          , string         destinationFolder
                                          , string         newFileName)
    {
        ConsoleLog.WriteLine("Copying files...");

        foreach (FileInfo file in sourceFiles)
        {
            file.CopyTo(Path.Combine(destinationFolder
                                   , newFileName));

            ConsoleLog.WriteLine($"\t{file.Name} copied to {newFileName}.");
        }

        ConsoleLog.WriteLine("Copying complete.");

        return true;
    }

    public static bool CopyFiles(List<FileInfo> sourceFiles
                               , string         destinationFolder)
    {
        ConsoleLog.WriteLine("Copying files...");

        foreach (FileInfo file in sourceFiles)
        {
            file.CopyTo(Path.Combine(destinationFolder
                                   , file.Name));

            ConsoleLog.WriteLine($"\t{file.Name} copied.");
        }

        ConsoleLog.WriteLine("Copying complete."
                           , ConsoleLog.LoggingFlags.Success);

        return true;
    }

    public static void DeleteFile(string filePath)
    {
        var file = new FileInfo(filePath);

        try
        {
            file.Delete();
            ConsoleLog.WriteLine($"{filePath} was deleted.");
        }
        catch (DirectoryNotFoundException ex)
        {
            LogDeleteException(ex.ToString(),
                               ex.Message);
        }
        catch (IOException ex)
        {
            LogDeleteException(ex.ToString()
                             , ex.Message);
        }
        catch (SecurityException ex)
        {
            LogDeleteException(ex.ToString()
                             , ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            LogDeleteException(ex.ToString()
                             , ex.Message);
        }
    }

    public static FileInfo CreateNonExistingFile(string fullPathFile)
    {
        FileInfo file = new FileInfo(fullPathFile);

        if (file.Exists)
            return file;

        file.Create()
            .Close();

        return file;
    }

    private static List<FileInfo> GetMissingFiles(IEnumerable<FileInfo> sourceFiles
                                                , IEnumerable<FileInfo> destinationFiles)
    {
        var fileComparer = new FileCompare();

        var missingFiles = (from file in sourceFiles
                            select file)
               .Except(destinationFiles
                     , fileComparer);

        return missingFiles.ToList();
    }

    private static void LogMissingFiles(IEnumerable<FileInfo> sourceFiles
                                      , IEnumerable<FileInfo> destinationFiles
                                      , List<FileInfo> listOfMissingFiles)
    {
        var puralFile = listOfMissingFiles.Count != 1 ?
                                "s" :
                                "";

        var puralIs = listOfMissingFiles.Count != 1 ?
                              "were" :
                              "is";

        ConsoleLog.WriteLine($"There {puralIs} {listOfMissingFiles.Count} missing file{puralFile}.");
    }

    private static IEnumerable<FileInfo> GetAllFiles(string folder)
    {
        DirectoryInfo folderInfo = GetFolder(folder);

        IEnumerable<FileInfo> enumeratedFiles = folderInfo.EnumerateFiles();

        var pural = enumeratedFiles.Count() != 1 ?
                            "s" :
                            "";

        ConsoleLog.WriteLine($"\tFound {enumeratedFiles.Count()} file{pural} in {folder}"
                           , ConsoleLog.LoggingFlags.Normal);

        return enumeratedFiles;
    }

    private static DirectoryInfo GetFolder(string folder)
    {
        DirectoryInfo folderInfo = new DirectoryInfo(folder);

        try
        {
            if (!folderInfo.Exists)
            {
                folderInfo.Create();
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            ConsoleLog.WriteLine($"You do not have access to the folder: {folder}"
                               , ConsoleLog.LoggingFlags.Failure
                               , ex.ToString());

            throw;
        }

        return folderInfo;
    }

    private static void LogDeleteException(string exceptionString
                                         , string exceptionMessage)
    {
        ConsoleLog.WriteLine($"{exceptionMessage}: while trying to cleanup (backup and delete) the Log File."
                           , ConsoleLog.LoggingFlags.Failure);

        ConsoleLog.WriteLine($"View the Log File for more information: {ConsoleLog.FullPathToLogFile}");
        ConsoleLog.WriteLine($"Details about this error:{Environment.NewLine}{exceptionString}");

        ConsoleLog.WriteLine("Program will continue."
                           , ConsoleLog.LoggingFlags.Warning);
    }

    public static void DeleteFolder(string path)
    {
        var folderInfo = new DirectoryInfo(path);

        try
        {
            if (folderInfo.Exists)
            {
                folderInfo.Delete(true);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);

            throw;
        }
    }

    public static DataTable ConvertFileToDataTable(string filePath)
    {
        var table     = new DataTable();
        var delimiter = new[] { FileManager.DELIMITER };

        using (var reader = new StreamReader(filePath) )
        {
            table = SetColumnHeaders(table, reader, delimiter);

            while (reader.Peek() > 0)
            {
                DataRow row = table.NewRow();

                row.ItemArray = reader.ReadLine().Split(FileManager.DELIMITER);

                table.Rows.Add(row);
            }
        }

        return table;
    }

    private static DataTable SetColumnHeaders(DataTable table, StreamReader fileStream, char[] delimiter)
    {
        string[] columnHeaders = fileStream.ReadLine()
                                          ?.Split(delimiter);

        if (columnHeaders != null)
        {
            foreach (string columnHeader in columnHeaders)
            {
                table.Columns.Add(columnHeader);
            }
        }

        return table;
    }

    /// <summary>
    /// Saves table data to a Tab Separated File
    /// </summary>
    /// <param name="table">the DataTable that contains the data</param>
    /// <param name="filePath">The path to the file </param>
    /// <param name="autoFlush"></param>
    public static void SaveTableToTsv(DataTable table, 
                                       string    filePath,
                                       bool      autoFlush = false)
    {
        using (StreamWriter streamToFile = new StreamWriter(filePath))
        {
            streamToFile.AutoFlush = autoFlush;

            WriteColumnsToFile(streamToFile, table); 
            WriteRowsToFile(streamToFile, table);

            streamToFile.Close();
        }

        ConsoleLog.WriteLine($"{FileManager.DELIMITER}{Path.GetFileName(filePath)} saved");
    }

    private static void WriteColumnsToFile(StreamWriter streamToFile, DataTable table)
    {
        int numberOfColumns = table.Columns.Count;

        for (int i = 0; i < numberOfColumns; i++)
        {
            streamToFile.Write(table.Columns[i]);
            WriteDelimiter(streamToFile, numberOfColumns, i);
        }

        //Write carriage return to end the row of column headers
        streamToFile.Write(streamToFile.NewLine);

    }

    private static void WriteRowsToFile(StreamWriter streamToFile, DataTable table)
    {
        int numberOfColumns = table.Columns.Count;

        foreach (DataRow row in table.Rows)
        {
            for (int i = 0; i < numberOfColumns; i++)
            {
                WriteLine(streamToFile, row, i);
                WriteDelimiter(streamToFile, numberOfColumns, i);
            }

            //Write carriage return to end the row
            streamToFile.Write(streamToFile.NewLine);
        }
    }

    private static void WriteDelimiter(StreamWriter streamToFile, 
                                       int          numberOfColumns, 
                                       int          index)
    {
        if (index < numberOfColumns - 1)
        {
            streamToFile.Write(FileManager.DELIMITER);
        }
    }

    private static void WriteLine(StreamWriter streamToFile, 
                                  DataRow      row, 
                                  int          index)
    {
        if ( ! Convert.IsDBNull(row[index]))
        {
            streamToFile.Write(row[index].ToString());
        }
    }
}

class FileCompare : IEqualityComparer<FileInfo>
{
    /// <summary>
    /// Defines how to compare files attributes
    /// Make sure the left and right sides are instantiated objects
    /// </summary>
    /// <param name="leftSide"></param>
    /// <param name="rightSide"></param>
    /// <returns></returns>
    public bool Equals(FileInfo leftSide
                     , FileInfo rightSide)
    {
        //if (leftSide == null 
        // && rightSide == null)
        //{
        //    return true;
        //}
        //else if (leftSide == null 
        //      || rightSide == null)
        //{
        //    return false;
        //}
        //else
        //{
        //    return (leftSide.Name == rightSide.Name);
        //}
        return (leftSide.Name == rightSide.Name);
    }

    public int GetHashCode(FileInfo leftSide)
    {
        string s = $"{leftSide.Name}";

        return s.GetHashCode();
    }
}
