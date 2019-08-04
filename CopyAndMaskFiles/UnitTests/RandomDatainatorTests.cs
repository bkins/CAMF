using Xunit;
using System;
using static RandomDatainator;

public class RandomDatainatorTests : IClassFixture<SetupFixture>
{
    [Fact]
    public void AllListsHaveBeenPopulated()
    {
        Assert.True(AllListHaveValues());
    }

    [Fact]
    public void RandomNumberWithStartLargerThanEnd()
    {
        ArithmeticException ex = Assert.Throws<ArithmeticException>(() => GenerateRandomNumberBetweenNumbers(1, 0));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 10000)]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void RandomNumberBetweenNumbers(int start
                                         , int end)
    {
        int testInt = GenerateRandomNumberBetweenNumbers(start
                                                       , end);

        Assert.True(testInt >= start && testInt <= end);
    }

    [Fact]
    public void FirstNameDoesNotEqualMiddleName()
    {
        var middleName = string.Empty;
        var firstName = RandomDatainator.GetFirstName(middleName);

        Assert.True(firstName != middleName);
    }

    [Fact]
    public void MiddleNameDoesNotEqualFirstName()
    {
        var firstName  = string.Empty;
        var middleName = RandomDatainator.GetFirstName(firstName);

        Assert.True(middleName != firstName);
    }

    [Theory]
    [InlineData(100)]
    public void FirstAndMiddleNeverEqualForGivenNumberOfTimes(int tries)
    {
        for (int i = 0; i < tries; i++)
        {
            FirstNameDoesNotEqualMiddleName();
            MiddleNameDoesNotEqualFirstName();
        }
    }
}

public class SetupFixture : IDisposable
{
    public SetupFixture()
    {
        Initialize(@"../../");
    }

    public void Dispose()
    {
        //Cleanup any resources created within tests
    }

}