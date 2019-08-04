using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.IO;

public static class FileMasker
{
    private const char MASKING_CHARACTER = '*';
    public const string PERSON_AND_JOB_FOLDER = "Person&JobSubsets";

    public static void MaskEachMissingFile(List<FileInfo>             missingFiles,
                                           Dictionary<string, int>    fieldsToMask,
                                           List<string>               listOfFileNamePatterns,
                                           string                     realSsnColumnName,
                                           bool                       logToScreen)
    {
        ConsoleLog.WriteLine("Masking files...");

        foreach (var file in FileManager.GetFilesToMask(missingFiles, listOfFileNamePatterns))
        {
            //person or job file:
            MaskEachSpecifiedColumnInFile(file.FullName,
                                          fieldsToMask,
                                          realSsnColumnName,
                                          logToScreen);
        }

        ConsoleLog.WriteLine("All copied files masked.", ConsoleLog.LoggingFlags.Success);
    }

    private static DataTable MaskFields(DataTable                  table, 
                                        DataTable                  fakeTable,
                                        string                     realSsnColumnName,
                                        Dictionary<string, int>    fieldsToMask)
    {
        try
        {
            if (TableContainsAnyColumnInList(fieldsToMask.Keys
                                           , table)
             && table.Columns.Contains(realSsnColumnName))
            {

                table.Select()
                     .ToList()
                     .ForEach(row =>
                              {
                                  MaskFields(fakeTable
                                           , realSsnColumnName
                                           , row
                                           , fieldsToMask);
                              });
            }
        }
        catch (ArgumentException ex)
        {
            ConsoleLog.WriteLine("");
            ConsoleLog.WriteLine($"See log for more information: {ConsoleLog.FullPathToLogFile}", ConsoleLog.LoggingFlags.Failure);
            ConsoleLog.WriteLine(ex.ToString(), ConsoleLog.LoggingFlags.Failure);
            ConsoleLog.WriteLine("");

            throw;
        }

        return table;
    }

    private static bool TableContainsAnyColumnInList(Dictionary<string,int>.KeyCollection listOfColumns
                                                   , DataTable    table)
    {
        foreach (var column in listOfColumns)
        {
            if (table.Columns.Contains(column))
            {
                return true;
            }
        }

        return false;
    }

    private static void MaskFields(DataTable                 fakeTable, 
                                   string                    realSsnColumnName,
                                   DataRow                   row,
                                   Dictionary<string, int> fieldsToMask)
    {
        
        fakeTable.Select()
                 .ToList()
                 .ForEach(fakeRow =>
                 {
                     if (row[realSsnColumnName].ToString() == fakeRow[realSsnColumnName].ToString())
                     {
                         foreach(var field in row.Table.Columns)
                         {
                            var fieldName = field.ToString();

                            if(fieldsToMask.Keys.Contains(fieldName))
                             {
                                 if(row[fieldName].ToString().Trim() == fakeRow[fieldName].ToString().Trim())
                                     continue;

                                 ConsoleLog.WriteLine($"{row[fieldName]} masked to {fakeRow[fieldName]}");
                                 row[fieldName] = fakeRow[fieldName];
                             }
                         }

                         
                     }

                     ConsoleLog.WriteLongRunningProgressSpinner();
                 });


        //Not right:
        //string rowValue = row[fieldToMask.Key].ToString();
        //int numberToMask = MakeSureNumberToMaskIsValid(fieldToMask.Value, rowValue.Length);
        //string beginOfString = RepeatMaskingCharacter(numberToMask);
        //string endOfString = rowValue.Substring(numberToMask, rowValue.Length - numberToMask);

        //row[fieldToMask.Key] = $"{beginOfString}{endOfString}";
        
    }

    private static void MaskEachSpecifiedColumnInFile(string                     filePath,
                                                      Dictionary<string, int>    fieldsToMask,
                                                      string                     realSsnColumnName,
                                                      bool                       logToScreen)
    {
       string fakeFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory
                                         , FileManager.FAKE_FILE);

        DataTable fakeTable = FileManager.ConvertFileToDataTable(fakeFilePath);
        DataTable table     = FileManager.ConvertFileToDataTable(filePath);

        if (filePath.Contains("person.dat"))
        {
            //we only need to mask the person file
            table = MaskFields(table, fakeTable, realSsnColumnName, fieldsToMask);
        }

        ConsoleLog.WriteLine($"{FileManager.DELIMITER}{Path.GetFileName(filePath)} masked.");

        FileManager.SaveTableToTsv(table, 
                                   SetMaskedFileToSeparateFolderInDestination(filePath), 
                                   logToScreen);
    }

    public static string SetMaskedFileToSeparateFolderInDestination(string fileName)
    {   
        string maskedFolder = Path.Combine(Path.GetDirectoryName(fileName), PERSON_AND_JOB_FOLDER);

        if ( ! Directory.Exists(maskedFolder)) Directory.CreateDirectory(maskedFolder);
        
        return Path.Combine(maskedFolder, Path.GetFileName(fileName));
    }
}
