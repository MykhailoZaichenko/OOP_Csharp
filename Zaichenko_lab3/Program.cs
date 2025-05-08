using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public enum Size
{
    Pocket,
    Standard,
    Big
}

public interface IDateAndCopy
{
    DateTime Date { get; }
    object DeepCopy();
}

public class Person : IDateAndCopy 
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime BirthDate { get; set; }

    public DateTime Date => this.BirthDate;

    public Person(string firstName, string lastName, DateTime birthDate)
    {
        this.FirstName = firstName;
        this.LastName = lastName;
        this.BirthDate = birthDate;
    }

    public Person() : this("DefaultFirstName", "DefaultLastName", new DateTime(2001, 1, 1)) { }

    public override string ToString() => $"{this.FirstName} {this.LastName}, born on {this.BirthDate.ToShortDateString()}";

    public virtual string ToShortString() => $"{this.FirstName} {this.LastName}";

    public override bool Equals(object obj)
    {
        if (obj is Person other)
            return this.FirstName == other.FirstName && this.LastName == other.LastName && this.BirthDate == other.BirthDate;
        return false;
    }

    public static bool operator ==(Person a, Person b) => Equals(a, b);
    public static bool operator !=(Person a, Person b) => !Equals(a, b);

    public override int GetHashCode() => HashCode.Combine(this.FirstName, this.LastName, this.BirthDate);

    public virtual object DeepCopy() => new Person(this.FirstName, this.LastName, this.BirthDate);
}

public class Book : IDateAndCopy
{
    public Person Author { get; set; }
    public string Title { get; set; }
    public DateTime PublicationDate { get; set; }
    public Size Format { get; set; }

    public DateTime Date => this.PublicationDate;

    public Book(Person author, string title, DateTime publicationDate, Size format)
    {
        this.Author = author;
        this.Title = title;
        this.PublicationDate = publicationDate;
        this.Format = format;
    }

    public Book() : this(new Person(), "DefaultTitle", new DateTime(2001, 1, 1), Size.Standard) { }


    public override string ToString() => $"Author: {this.Author}, Title: {this.Title}, Format: {this.Format}, Published: {this.PublicationDate.ToShortDateString()}";

    public virtual object DeepCopy() => new Book((Person)this.Author.DeepCopy(), this.Title, this.PublicationDate, this.Format);
}

public class Organization
{
    protected string Name;
    protected string Address;
    protected int RegistrationYear;

    public Organization(string name, string address, int year)
    {
        this.Name = name;
        this.Address = address;
        this.RegistrationYear = year;
    }

    public Organization() : this("DefaultOrganization", "DefaultAddress", 1999) { }

    public string OrgName { get => this.Name; set => this.Name = value; }
    public string OrgAddress { get => this.Address; set => this.Address = value; }

    public int OrgYear
    {
        get => this.RegistrationYear;
        set
        {
            if (value > DateTime.Now.Year)
                throw new ArgumentOutOfRangeException("Year cannot be in the future");
            this.RegistrationYear = value;
        }
    }

    public virtual object DeepCopy() => new Organization(this.Name, this.Address, this.RegistrationYear);

    public override bool Equals(object obj)
    {
        if (obj is Organization o)
            return this.Name == o.Name && this.Address == o.Address && this.RegistrationYear == o.RegistrationYear;
        return false;
    }

    public static bool operator ==(Organization a, Organization b) => Equals(a, b);
    public static bool operator !=(Organization a, Organization b) => !Equals(a, b);

    public override int GetHashCode() => HashCode.Combine(this.Name, this.Address, this.RegistrationYear);

    public override string ToString() => $"Name: {this.Name}, Address: {this.Address}, Year: {this.RegistrationYear}";
}

public class Publisher : Organization, IDateAndCopy, IEnumerable
{
    private DateTime licenseExpiryDate;
    private List<Book> books = new List<Book>();
    private List<Person> employees = new List<Person>();

    public Publisher(string name, string address, DateTime licenseDate, int year)
        : base(name, address, year)
    {
        this.licenseExpiryDate = licenseDate;
    }

    public Publisher()
        : this(
            "DefaultPublisher",
            "DefaultAddress",
            DateTime.Today,
            2000
        )
    { }


    public DateTime LicenseExpiryDate { get => this.licenseExpiryDate; set => this.licenseExpiryDate = value; }
    public List<Book> Books => this.books;
    public List<Person> Employees => this.employees;

    public double PocketBookPercentage => this.books.Count == 0 ? 0 : (double)this.books.Count(b => b.Format == Size.Pocket) / this.books.Count * 100;

    public void AddBooks(params Book[] newBooks) => this.books.AddRange(newBooks);

    public void AddEmployee(params Person[] people) => this.employees.AddRange(people);

    public Organization OrganizationInfo
    {
        get => new Organization(this.Name, this.Address, this.RegistrationYear);
        set { this.Name = value.OrgName; this.Address = value.OrgAddress; this.RegistrationYear = value.OrgYear; }
    }

    public override string ToString() =>
        $"{base.ToString()}, License Expiry: {this.licenseExpiryDate.ToShortDateString()},\nBooks:\n{string.Join("\n", this.books)},\nEmployees:\n{string.Join("\n", this.employees)}";

    public string ToShortString() =>
        $"{base.ToString()}, License Expiry: {this.licenseExpiryDate.ToShortDateString()}, Total Books: {this.books.Count}";

    public object DeepCopy()
    {
        var copy = new Publisher(this.Name, this.Address, this.licenseExpiryDate, this.RegistrationYear);
        copy.books = this.books.Select(b => (Book)b.DeepCopy()).ToList();
        copy.employees = this.employees.Select(e => (Person)e.DeepCopy()).ToList();
        return copy;
    }

    public DateTime Date => this.licenseExpiryDate;

    public IEnumerator GetEnumerator()
    {
        foreach (var book in this.books)
            if (!this.employees.Contains(book.Author))
                yield return book;
    }

    public IEnumerable<Book> BooksAfterYear(int year)
    {
        foreach (var book in this.books)
            if (book.PublicationDate.Year > year)
                yield return book;
    }

    public IEnumerable<Book> BooksByAuthor(string name)
    {
        foreach (var book in this.books)
            if ($"{book.Author.FirstName} {book.Author.LastName}" == name)
                yield return book;
    }

    public IEnumerable<Book> BooksByEmployeeAuthors()
    {
        foreach (var book in this.books)
            if (this.employees.Contains(book.Author))
                yield return book;
    }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("1 -----------------\n");
        Organization org1 = new Organization("Name", "Address", 2001);
        Organization org2 = new Organization("Name", "Address", 2001);
        Console.WriteLine($"Org1: {org1.ToString()}\nOrg2:{org2.ToString()}");
        Console.WriteLine($"Link equals: {ReferenceEquals(org1, org2)}");
        Console.WriteLine($"Obj equals: {org1.Equals(org2)}");
        Console.WriteLine($"Hash Codes: {org1.GetHashCode()}, {org2.GetHashCode()}");

        Console.WriteLine("\n2 -----------------\n");
        try
        {
            org1.OrgYear = DateTime.Now.Year + 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }

        Console.WriteLine("\n3 -----------------\n");
        var pub = new Publisher("Litera", "Kyiv", new DateTime(2030, 12, 31), 2020);
        var author1 = new Person("Ivan", "Ivanenko", new DateTime(1980, 1, 1));
        var author2 = new Person("Taras", "Shevchenko", new DateTime(1990, 2, 2));
        var book1 = new Book(author1, "Intro to C#", new DateTime(2020, 1, 1), Size.Standard);
        var book2 = new Book(author2, "Advanced C#", new DateTime(2021, 2, 2), Size.Big);
        var book3 = new Book(author2, "C#", new DateTime(2022, 3, 3), Size.Pocket);
        pub.AddBooks(book1, book2, book3);
        pub.AddEmployee(author2);
        Console.WriteLine(pub.ToString());

        Console.WriteLine("\n4 -----------------\n");
        Console.WriteLine("OrganizationInfo:");
        Console.WriteLine(pub.OrganizationInfo);

        Console.WriteLine("\n5 -----------------\n");
        var pubCopy = (Publisher)pub.DeepCopy();
        var info = pub.OrganizationInfo;
        info.OrgName = "ChangedName";
        pub.OrganizationInfo = info;
        pub.Books[0].Title = "ChangedTitle";
        Console.WriteLine("Original:");
        Console.WriteLine(pub.ToString());
        Console.WriteLine("\nCopy:");
        Console.WriteLine(pubCopy.ToString());

        Console.WriteLine("\n6 -----------------\n");
        Console.WriteLine("Books published after 2020:");
        foreach (var book in pub.BooksAfterYear(2020))
            Console.WriteLine(book);

        Console.WriteLine("\n7 -----------------\n");
        Console.WriteLine("Books by author Taras Shevchenko:");
        foreach (var book in pub.BooksByAuthor("Ivan Ivanenko"))
            Console.WriteLine(book);

        Console.WriteLine("\n8 -----------------\n");
        Console.WriteLine("Books by authors NOT employees:");
        foreach (var book in pub)
            Console.WriteLine(book);

        Console.WriteLine("\n9 -----------------\n");
        Console.WriteLine("Books by authors who are employees:");
        foreach (var book in pub.BooksByEmployeeAuthors())
            Console.WriteLine(book);
    }
}
