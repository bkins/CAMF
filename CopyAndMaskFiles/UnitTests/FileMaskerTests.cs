using Xunit;
using System.Collections.Generic;
using System.IO;
using System;

public class FileMaskerTests : IDisposable
{
    private readonly List<FileInfo> _aSetOfFiles;
    private static   string         _workingDirectory     = string.Empty;
    private static   string         _destinationDirectory = string.Empty;

    private readonly Arguments _arguments;

    public FileMaskerTests()
    {
        var setup = new FileMaskerSetup();

        _aSetOfFiles            = setup.SetOfFiles;
        _workingDirectory       = setup.WorkingDirectory;
        _destinationDirectory   = setup.DestinationDirectory;
        _arguments              = setup.Argument;
    }

    public void Dispose()
    {
        Directory.Delete(_workingDirectory, true);
    }

    [Fact]
    public void CopyAndMaskMissingFiles_FullTest()
    {
        if (_aSetOfFiles.Count > 0)
        {
            FileManager.CopyFiles(_aSetOfFiles, _destinationDirectory);

            var listOfMissingFiles = FileManager.SetMissingFilesToDestinationFolder(_aSetOfFiles, _destinationDirectory);

            FileMasker.MaskEachMissingFile(listOfMissingFiles,
                                           _arguments.FieldsToMask,
                                           _arguments.FilesToMaskSearchPattern,
                                           _arguments.RealSsnColumnName,
                                           _arguments.LogToScreen);

            int numberOfFilesInDestination = Directory.GetFiles(_destinationDirectory).Length;
            int numberOfFilesInMaskedFileFolder = Directory.GetFiles(Path.Combine(_destinationDirectory, 
                                                                                  FileMasker.PERSON_AND_JOB_FOLDER)).Length;

            Assert.Equal(_aSetOfFiles.Count, numberOfFilesInDestination);
            Assert.Equal(numberOfFilesInMaskedFileFolder, NumberOfFilesThatShouldBeMasked());
        }
    }

    private int NumberOfFilesThatShouldBeMasked()
    {
        int numberOfMaskedFiles = 0;

        foreach(var file in _aSetOfFiles)
        {
            if(ShouldBeMasked(file.FullName))
            {
                numberOfMaskedFiles++;
            }
        }
        return numberOfMaskedFiles;
    }

    private bool ShouldBeMasked(string fileName)
    {
        bool hasPattern = false;
        foreach (var pattern in _arguments.FilesToMaskSearchPattern)
        {
            if (fileName.Contains(pattern)) hasPattern = true;
        }
        return hasPattern;
    }

    private class FileMaskerSetup
    {
        public List<FileInfo> SetOfFiles { get; } = new List<FileInfo>();

        public string WorkingDirectory { get; private set; } = string.Empty;

        public string DestinationDirectory { get; private set; } = string.Empty;

        public Arguments Argument { get; private set; }

        public FileMaskerSetup()
        {
            Assumptions();
            CreateWorkingDirectories();
            CreateTestFiles();
            FillFilesWithTestData();
        }

        private void Assumptions()
        {
            WorkingDirectory     = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
            DestinationDirectory = Path.Combine(WorkingDirectory,               "Destination");

            Argument = new Arguments(new string[]
                                       {
                                           @"\\ENADBSHRdv01\Dataload\SWHR\HRHIED\Archive\376",
                                           @"\\filedepot\app\S-ETL\DEV\SWHR\HANA_POC_SWHR_HEFiles",
                                           "New Social Security Number",
                                           "Person Last Name|0,person first name|0,person middle name|0,residence address line 1|0,residence address line 2|0,Residence Address Line One Text|0,Residence Address City Name|0,Residence Address Line Two Text|0",
                                           ".person.dat|.job.dat",
                                           "on on",
                                           "F"
                                       });

        }

        private void CreateWorkingDirectories()
        {
            if ( ! Directory.Exists(WorkingDirectory))
            {
                Directory.CreateDirectory(WorkingDirectory);
            }

            if ( ! Directory.Exists(DestinationDirectory))
            {
                Directory.CreateDirectory(DestinationDirectory);
            }
        }
        
        private void CreateTestFiles()
        {
            foreach (var file in new List<string>()
                                 {
                                     "1.person.dat",
                                     "2.person.dat",
                                     "1.job.dat",
                                     "2.job.dat",
                                     "1.funding.dat",
                                     "2.funding.dat",
                                     "1.wage.dat",
                                     "2.wage.dat"
                                 })
            {
                CreateTestFile(file);
            }
        }

        private void CreateTestFile(string fileName)
        {
            string fullPath = Path.Combine(WorkingDirectory, fileName);

            if (!File.Exists(fullPath))
            {
                File.Create(fullPath).Close();
                SetOfFiles.Add(new FileInfo(fullPath));
            }
        }


        private void FillFilesWithTestData()
        {
            foreach (var file in SetOfFiles)
            {
                using (var testFile = new StreamWriter(file.FullName))
                {
                    if (ShouldBeMasked(file.Name))
                    {
                        testFile.WriteLine("Old Social Security Number\tNew Social Security Number\tpersonnel_number\tPerson Last Name\tperson first name\tperson middle name\tresidence address line 1\tresidence address line 2\tResidence Address Line One Text\tResidence Address Line Two Text");
                        testFile.WriteLine("000112222\t111223333\t002020202\tSmith\tJohn\tTee\t123 Road St\t321 Blvd Rd\tOlympia\tTumwater");
                        testFile.WriteLine("000112222\t111223333\t002020202\tSmith\tJohn\tTee\t123 Road St\t321 Blvd Rd\tOlympia\tTumwater");
                        testFile.WriteLine("000112222\t111223333\t002020202\tSmith\tJohn\tTee\t123 Road St\t321 Blvd Rd\tOlympia\tTumwater");
                    }
                    else
                    {
                        testFile.WriteLine($"field1\tfield2\tfield3");
                        testFile.WriteLine($"value1a\tvalue2a\tvalue3a");
                        testFile.WriteLine($"value1b\tvalue2b\tvalue3b");
                        testFile.WriteLine($"value1c\tvalue2c\tvalue3c");
                        testFile.WriteLine($"value1d\tvalue2d\tvalue3d");
                    }
                }
            }
        }

        private bool ShouldBeMasked(string fileName)
        {
            bool hasPattern = false;
            foreach (var pattern in Argument.FilesToMaskSearchPattern)
            {
                if (fileName.Contains(pattern)) hasPattern = true;
            }
            return hasPattern;
        }



    }
}
