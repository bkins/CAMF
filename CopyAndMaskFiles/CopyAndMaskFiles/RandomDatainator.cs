using System;
using System.Collections.Generic;
using System.IO;

public static class RandomDatainator
{
    private static string _randomDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RandomData");

    private static readonly string NAMES_FILE_PATH      = Path.Combine(_randomDataFolder, "Names.txt");
    private static readonly string LAST_NAMES_FILE_PATH = Path.Combine(_randomDataFolder, "LastNames.txt");
    private static readonly string STREETS_FILE_PATH    = Path.Combine(_randomDataFolder, "StreetNames.txt");
    private static readonly string DIRECTIONS_FILE_PATH = Path.Combine(_randomDataFolder, "Directions.txt");
    private static readonly string STREET_TYPE_FILE_PATH= Path.Combine(_randomDataFolder, "StreetTypes.txt");
    private static readonly string CITIES_FILE_PATH     = Path.Combine(_randomDataFolder, "Cities.txt");
                                                
    private static List<string> _firstNames     = new List<string>();
    private static List<string> _middlesNames   = new List<string>();
    private static List<string> _lastNames      = new List<string>();
    private static List<string> _streetNames    = new List<string>();
    private static List<string> _streetTypes    = new List<string>();
    private static List<string> _directions     = new List<string>();
    private static List<string> _cities         = new List<string>();
    
    public static void Initialize()
    {
        _firstNames     = FileManager.ReadFileToList(NAMES_FILE_PATH);
        _middlesNames   = FileManager.ReadFileToList(NAMES_FILE_PATH);
        _lastNames      = FileManager.ReadFileToList(LAST_NAMES_FILE_PATH);
        _streetNames    = FileManager.ReadFileToList(STREETS_FILE_PATH);
        _streetTypes    = FileManager.ReadFileToList(STREET_TYPE_FILE_PATH);
        _directions     = FileManager.ReadFileToList(DIRECTIONS_FILE_PATH);
        _cities         = FileManager.ReadFileToList(CITIES_FILE_PATH);
    }

    public static void Initialize(string binFolder)
    {
        _randomDataFolder = Path.Combine(binFolder
                                        , "RandomData");
        Initialize();
    }

    public static Boolean AllListHaveValues()
    {
        return _firstNames.Count   > 0
            && _middlesNames.Count > 0
            && _lastNames.Count    > 0
            && _streetNames.Count  > 0
            && _streetTypes.Count  > 0
            && _directions.Count   > 0
            && _cities.Count       > 0;
    }

    public static int GenerateRandomNumberBetweenNumbers(int start, int end)
    {
        if (AreStartAndEndValid(start
                              , end))
        {
            return new Random().Next(start, end);
        }

        return start;
    }

    private static Boolean AreStartAndEndValid(int start
                                             , int end)
    {
        if (end < start) { throw new ArithmeticException("start must be less than end."); }

        return start != end;
    }

    private static string GetUniqueName(string newName, string otherName)
    {
        while (newName == otherName)
        {
            newName = GetName();
        }
        return newName;
    }
    public static string GetFirstName(string middleName)
    {
        return GetUniqueName(GetName(), middleName);
    }
    public static string GetName()
    {
        return _firstNames[GenerateRandomNumberBetweenNumbers(0, _firstNames.Count)];
    }

    public static string GetMiddleName(string firstName)
    {
        return GetUniqueName(GetName(), firstName);
    }

    public static string GetLastName()
    {
        return _lastNames[GenerateRandomNumberBetweenNumbers(0, _lastNames.Count)];
    }

    public static string GetAddress()
    {
        int     streetNumber = GenerateRandomNumberBetweenNumbers(100, 9999);
        string  streetName   = _streetNames[GenerateRandomNumberBetweenNumbers(0, _streetNames.Count)];
        string  streetType   = _streetTypes[GenerateRandomNumberBetweenNumbers(0, _streetTypes.Count)];
        string  direction    = _directions[GenerateRandomNumberBetweenNumbers(0, _directions.Count)];
        
        return $"{streetNumber.ToString()} {streetName} {streetType} {direction}";
    }

    public static string GetCity()
    {
        return _cities[GenerateRandomNumberBetweenNumbers(0, _cities.Count)];
    }


}
