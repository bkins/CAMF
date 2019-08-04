
class Person
{
    public string Ssn { get; set; }
    public string FirstName { get; set; }
    public string MiddleName { get; set; }
    public string LastName { get; set; }
    public string AddressLineOne { get; set; }
    public string City { get; set; }
    public Person FakeData { get; set; }

    public Person(Person someFakeData)
    {
        Initialize();
        FakeData = someFakeData;
    }

    public Person()
    {
        Initialize();
    }

    private void Initialize()
    {
        Ssn = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        MiddleName = string.Empty;
        AddressLineOne = string.Empty;
        City = string.Empty;
    }

    public override string ToString()
    {
        //TODO:  Add tabs dynamically based on the column they are separating.
        return $"{Ssn}\t{LastName}\t{FirstName}\t{MiddleName}\t{AddressLineOne}\t\t\t{City}";
    }
}
