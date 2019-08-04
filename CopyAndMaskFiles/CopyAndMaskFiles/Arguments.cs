using System;
using System.Collections.Generic;
using System.Linq;

public class Arguments
{
    public string[] ArgumentsFromUser   { get; set; }
    
    public string   SourcePath          { get; set; }
    
    public string   DestinationPath     { get; set; }
    
    public string   RealSsnColumnName { get; set; }

    public Dictionary<string, int> FieldsToMask  { get; set; }
    
    public List<string>   FilesToMaskSearchPattern { get; set; }

    public string   LoggingOptions      { get; set; }

    public bool LogToScreen => LoggingOptions.Split(" ")[0]
                                             .ToUpper()
                            == "ON";

    public string   ManualRun           { get; set; }

    private readonly int ExpectedNumberOfArguments = 7;
    
    public Arguments(string[] arguments)
    {
        ArgumentsFromUser = arguments;

        FieldsToMask = new Dictionary<string, int>();

        if(IsTheNumberOfArgumentsCorrect())
        {
            ParseArguments();
            ValidateArguments();
        }
    }

    private void ParseArguments()
    {
        SourcePath               = ArgumentsFromUser[0];
        DestinationPath          = ArgumentsFromUser[1];
        RealSsnColumnName        = GetRealSsnColumn(ArgumentsFromUser[2]);
        FieldsToMask             = GetListOfFields(ArgumentsFromUser[3]);
        FilesToMaskSearchPattern = GetListOfFileNamePatterns(ArgumentsFromUser[4]);
        LoggingOptions           = ArgumentsFromUser[5];
        ManualRun                = ArgumentsFromUser[6];
    }

    private string GetRealSsnColumn(string realSsnColumnName)
    {
        //If FieldsToMask is empty, then add the RealSsnColumnName to the list
        //This assumes that the "RealSsnColumnName" argument is before the FieldsToMask
        FieldsToMask.Add(realSsnColumnName, 0);

        return realSsnColumnName;
    }

    private Dictionary<string, int> GetListOfFields(string fields)
    {
        if (fields.Trim().Length != 0)
        {
            var arrayOfString = fields.Split(",").ToList();
            var dictionaryOfFields = FieldsToMask; // ?? new Dictionary<string, int>();

            foreach (var definition in from item in arrayOfString
                                       let definition = item.Split("|")
                                       select definition)
            {
                dictionaryOfFields.Add(definition[0], ConvertStringToInt(definition[1]));
            }

            return dictionaryOfFields;
        }
        else
        {
            return new Dictionary<string, int>();
        }
    }

    private List<string> GetListOfFileNamePatterns(string patterns)
    {
        if( patterns.Trim().Length != 0)
        {
            return patterns.Split('|').ToList();
        }
        else
        {
            return new List<string>();
        }
    }

    private  int ConvertStringToInt(string value)
    {
        int outInt = 0;

        if( ! int.TryParse(value, out outInt))
        {
            throw new ArgumentException("The number of characters to mask is not a number.", "FieldsToMask");
        }
        return outInt;
    }

    private void ValidateArguments()
    {
        if ( ! AreArgumentValid())
        {
            string errorMessage = $"Arguments passed in are not correct.  See log for more information: {ConsoleLog.FullPathToLogFile}";
            
            ConsoleLog.WriteLine(errorMessage, 
                                 ConsoleLog.LoggingFlags.Failure);
            ConsoleLog.WriteToFile(errorMessage);

            throw new ArgumentException(errorMessage);
        }
    }

    private bool IsTheNumberOfArgumentsCorrect()
    {
        if (ArgumentsFromUser.Length == 0) throw new ArgumentNullException("No parameters were set.");

        if ( ! (ArgumentsFromUser.Length == ExpectedNumberOfArguments))
        {
            throw new ArgumentException($"An incorrect number of arguments were passed in.  {Environment.NewLine}There were: {ArgumentsFromUser.Length.ToString()}. {ExpectedNumberOfArguments} are required.{Environment.NewLine}Arguments passed in: {string.Join(",", ArgumentsFromUser)}");
        }
        return true;
    }

    public bool AreArgumentValid()
    {
        return IsSourceValid() &&
               IsDestinationValid() &&
               IsFieldsToMaskValid() &&
               IsLoggingOptionsValid();
    }
    
    private bool IsPathValid(string path, string nameOfPath)
    {
        try
        {
            if ( ! FileManager.IsValidDirectoryPath(path)) throw new System.IO.DirectoryNotFoundException($"{path} is not a valid directory.");
        }
        catch (System.IO.DirectoryNotFoundException ex)
        {
            ConsoleLog.WriteLine(ex.ToString(),
                                 ConsoleLog.LoggingFlags.Failure,
                                 $"{nameOfPath}: {path}");
            return false;
        }

        return true;
    }
    
    private bool IsSourceValid()
    {
        return IsPathValid(SourcePath, nameof(SourcePath));
    }

    private bool IsDestinationValid()
    {
        return IsPathValid(DestinationPath, nameof(DestinationPath));
    }

    private static void ValidateFieldLengths(Dictionary<string, int> fieldsToMask)
    {
        foreach (var definition in fieldsToMask)
        {
            if ( definition.Value < 0)
            {
                throw new ArgumentException($"The field length for the field {definition.Key} must be 0 or greater.{Environment.NewLine}The field length passed in: {definition.Value}");
            }
        }
    }

    private bool IsFieldsToMaskValid()
    {
        if (FieldsToMask.Count == 0)
        {
            ConsoleLog.WriteLine("No fields were speficied to mask.",
                                 ConsoleLog.LoggingFlags.Failure,
                                 $"\t  Try passing a list of comma separated field names, like so:{Environment.NewLine}\t  Field1|5,Field2|11,Field3|4");
            return false;
        }
        ValidateFieldLengths(FieldsToMask);

        return true;
    }

    private bool IsLoggingOptionsValid()
    {
        var options = LoggingOptions.Split(" ");

        if(options.Length == 0) throw new ArgumentException("No value set.", 
                                                            nameof(LoggingOptions));

        if( ! (options.Length == 2)) throw new ArgumentException("Too few values set. Acceptable options are: 'on on' or 'on off' or 'off on' or 'off off.'", 
                                                                nameof(LoggingOptions));
        
        return true;
    }
}
