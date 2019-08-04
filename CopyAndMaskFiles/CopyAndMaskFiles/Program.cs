using System;
using System.Collections.Generic;
using System.Data;
using System.IO;

class Program
{
    static void  Main(string[] args)
    {
        try
        {
            var arguments         = Setup(args);
            var realSsnColumnName = arguments.RealSsnColumnName;

            //Begin work
            //Step 1:  Determine what files are missing from source
            List<FileInfo> missingFiles = FileManager.GetSourceFilesNotInDestination(arguments.SourcePath
                                                                                   , arguments.DestinationPath);
            if (missingFiles.Count > 0)
            {
                //Step 2:  Build or add to FakeDataFile
                Dictionary<string, Person> people = FileManager.BuildFakeDataFile(realSsnColumnName
                                                                                , missingFiles
                                                                                , arguments.FilesToMaskSearchPattern
                                                                                , arguments.FieldsToMask);


                CopyAndMaskMissingFiles(missingFiles
                                      , arguments.DestinationPath
                                      , arguments.FieldsToMask
                                      , arguments.FilesToMaskSearchPattern
                                      , realSsnColumnName
                                      , arguments.LogToScreen);
            }

            //Work complete

            ConsoleLog.WriteLine($"Program completed normally at: {DateTime.Now}"
                               , ConsoleLog.LoggingFlags.Success);
        }
        catch (FileNotFoundException)
        {
            ConsoleLog.WriteLine("Couldn't find file.  Try rerunning the application.", 
                                 ConsoleLog.LoggingFlags.Failure);
            Environment.Exit(-1);
        }
        catch(Exception ex)
        {
            ConsoleLog.WriteLine(ex.ToString(), 
                                 ConsoleLog.LoggingFlags.Failure);

            ConsoleLog.WriteLine($"Something went wrong.  Please review the log for more information: {ConsoleLog.FullPathToLogFile}", 
                                 ConsoleLog.LoggingFlags.Failure);
            
            Environment.Exit(-1);
        }

        Environment.Exit(0);
     }

    private static Arguments Setup(string[] args)
    {
        ConsoleLog.Initialize();

        var arguments = new Arguments(args);

        SetOptionsFromArguments(arguments);

        RandomDatainator.Initialize();

        return arguments;
    }

    static void SetOptionsFromArguments(Arguments arguments)
     {
         ConsoleLog.SetOptionsFromArguments(arguments.LoggingOptions.Split(" ")[0].ToString(),
                                            arguments.LoggingOptions.Split(" ")[1].ToString(),
                                            arguments.ManualRun);
     }

     static void CopyAndMaskMissingFiles(List<FileInfo>          missingFiles, 
                                         string                  destinationPath,
                                         Dictionary<string, int> fieldsToMask,
                                         List<string>            searchPatterns,
                                         string                  realSsnColumnName,
                                         bool                    logToScreen)
     {
         if (missingFiles.Count <= 0) return;

         FileManager.CopyFiles(missingFiles, destinationPath);

         //Copy files
         var listOfMissingFiles = FileManager.SetMissingFilesToDestinationFolder(missingFiles, 
                                                                                 destinationPath);
         //Mask data
         FileMasker.MaskEachMissingFile(listOfMissingFiles, 
                                        fieldsToMask, 
                                        searchPatterns,
                                        realSsnColumnName,
                                        logToScreen);
     }
 }
