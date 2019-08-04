using Xunit;
using System;

public class FileManagerTests : IDisposable
{
    [Theory]
    [InlineData(@"c\there\is\no\colon\after\the\drive", true)]
    [InlineData("thisIsNotAPath", true)]
    [InlineData(@"C:\This\is\a\valid\path\but\it\does\not\exist", true)]
    [InlineData(@"C:\Program Files", true)]
    [InlineData("..", true)]
    [InlineData("...", true)]
    [InlineData("~", true)]
    [InlineData(".", true)]
    [InlineData(",", true)]
    [InlineData("`", true)]
    [InlineData("*", false)]
    [InlineData("", false)]
    [InlineData(":", false)]
    public void AreValidPaths(string path, bool expected)
    {
        Assert.Equal(expected, FileManager.IsValidDirectoryPath(path));
    }

    public void Dispose()
    {
        FileManager.DeleteFolder("thisIsNotAPath");
        FileManager.DeleteFolder(@"C:\This");
        FileManager.DeleteFolder("c");
        FileManager.DeleteFolder("~");
        FileManager.DeleteFolder(",");
        FileManager.DeleteFolder("`");
    }
}
