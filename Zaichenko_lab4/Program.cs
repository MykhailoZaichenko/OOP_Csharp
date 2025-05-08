using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

public class Organization : IComparable<Organization>, IComparer<Organization>
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int RegistrationYear { get; set; }

    public Organization(string name, string address, int year)
    {
        this.Name = name;
        this.Address = address;
        this.RegistrationYear = year;
    }

    public Organization() : this("DefaultOrg", "DefaultAddress", 2000) { }

    public int CompareTo(Organization other) => this.Name.CompareTo(other?.Name);
    public int Compare(Organization x, Organization y) => x.RegistrationYear.CompareTo(y.RegistrationYear);

    public override string ToString() => $"Organization name: {this.Name}, Address: {this.Address}, Year: {this.RegistrationYear}";

    public override bool Equals(object obj) => obj is Organization o && o.Name == this.Name && o.Address == this.Address && o.RegistrationYear == this.RegistrationYear;
    public override int GetHashCode() => HashCode.Combine(this.Name, this.Address, this.RegistrationYear);

    public static bool operator ==(Organization a, Organization b) => Equals(a, b);
    public static bool operator !=(Organization a, Organization b) => !Equals(a, b);
}

public class OrganizationAddressComparer : IComparer<Organization>
{
    public int Compare(Organization x, Organization y) => x.Address.CompareTo(y.Address);
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
        set { this.Name = value.Name; this.Address = value.Address; this.RegistrationYear = value.RegistrationYear; }
    }

    public override string ToString() =>
        $"{base.ToString()}, License Expiry: {this.licenseExpiryDate.ToShortDateString()},\nBooks:\n{string.Join("\n", this.books)},\nEmployees:{string.Join("\n", this.employees)}\n";

    public string ToShortString() =>
        $"{base.ToString()}, License Expiry: {this.licenseExpiryDate.ToShortDateString()}, Total Books: {this.books.Count}, Employees: {this.employees.Count}";

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

public class PublisherCollection
{
    private List<Publisher> publishers = new List<Publisher>();

    public void AddDefaults()
    {
        var author = new Person("Ivan", "Ivanenko", new DateTime(1980, 1, 1));
        var employee = new Person("Taras", "Shevchenko", new DateTime(1990, 2, 2));

        var book1 = new Book(author, "Intro to C#", new DateTime(2020, 1, 1), Size.Standard);
        var book2 = new Book(employee, "Advanced C#", new DateTime(2021, 2, 2), Size.Big);

        var p1 = new Publisher("Alpha", "Kyiv", DateTime.Today, 2010);
        p1.AddBooks(book1);
        p1.AddEmployee(author);

        var p2 = new Publisher("Beta", "Lviv", DateTime.Today, 2012);
        p2.AddBooks(book2);
        p2.AddEmployee(employee);

        publishers.Add(p1);
        publishers.Add(p2);
    }

    public void AddPublishers(params Publisher[] pubs)
    {
        foreach (var p in pubs)
        {
            var book = new Book(new Person("Oksana", "Petryk", new DateTime(1995, 3, 3)), $"Book of {p.Name}", DateTime.Today, Size.Pocket);
            var employee = new Person("Andrii", "Zaychenko", new DateTime(1993, 4, 4));
            p.AddBooks(book);
            p.AddEmployee(employee);
            publishers.Add(p);
        }
    }

    public override string ToString() => string.Join("\n", publishers.Select(p => p.ToString()));
    public string ToShortString() => string.Join("\n", publishers.Select(p => p.ToShortString()));

    public void SortByName() => publishers.Sort();
    public void SortByYear() => publishers.Sort(new Organization());
    public void SortByAddress() => publishers.Sort(new OrganizationAddressComparer());
}

public class TestCollections
{
    private List<Organization> orgList;
    private List<string> stringList;
    private Dictionary<Organization, Publisher> orgDict;
    private Dictionary<string, Publisher> stringDict;

    public static Publisher GeneratePublisher(int i)
    {
        return new Publisher($"Publisher{i}", $"Address{i}", DateTime.Today, 2000 + i);
    }

    public TestCollections(int count)
    {
        orgList = new List<Organization>(count);
        stringList = new List<string>(count);
        orgDict = new Dictionary<Organization, Publisher>(count);
        stringDict = new Dictionary<string, Publisher>(count);

        for (int i = 0; i < count; i++)
        {
            Publisher pub = GeneratePublisher(i);
            Organization org = new Organization(pub.Name, pub.Address, pub.RegistrationYear);
            orgList.Add(org);
            stringList.Add(org.ToString());
            orgDict.Add(org, pub);
            stringDict.Add(org.ToString(), pub);
        }
    }

    public void MeasureSearchTimes()
    {
        var items = new[]
        {
            orgList[0],
            orgList[orgList.Count / 2],
            orgList[^1],
            new Organization("NotExist", "Nowhere", 1999)
        };

        foreach (var org in items)
        {
            string orgStr = org.ToString();
            var sw = new Stopwatch();

            sw.Start(); orgList.Contains(org); sw.Stop();
            Console.WriteLine($"List<Organization>: {sw.ElapsedTicks} ticks");

            sw.Restart(); stringList.Contains(orgStr); sw.Stop();
            Console.WriteLine($"List<string>: {sw.ElapsedTicks} ticks");

            sw.Restart(); orgDict.ContainsKey(org); sw.Stop();
            Console.WriteLine($"Dictionary<Org,Pub> by key: {sw.ElapsedTicks} ticks");

            sw.Restart(); stringDict.ContainsKey(orgStr); sw.Stop();
            Console.WriteLine($"Dictionary<string,Pub> by key: {sw.ElapsedTicks} ticks");

            sw.Restart(); orgDict.ContainsValue(new Publisher(org.Name, org.Address, DateTime.Today, org.RegistrationYear)); sw.Stop();
            Console.WriteLine($"Dictionary<Org,Pub> by value: {sw.ElapsedTicks} ticks\n");
        }
    }
}

class Program
{
    static void Main()
    {
        var collection = new PublisherCollection();
        collection.AddDefaults();
        collection.AddPublishers(
            new Publisher("Gamma", "Odesa", DateTime.Today, 2005),
            new Publisher("Delta", "Kharkiv", DateTime.Today, 2008)
        );

        Console.WriteLine("Original Collection:\n" + collection);
        collection.SortByName(); Console.WriteLine("\nSorted by Name:\n" + collection);
        collection.SortByYear(); Console.WriteLine("\nSorted by Year:\n" + collection);
        collection.SortByAddress(); Console.WriteLine("\nSorted by Address:\n" + collection);

        Console.WriteLine("\n=== Testing Collections ===");
        var test = new TestCollections(1000);
        test.MeasureSearchTimes();
    }
}
