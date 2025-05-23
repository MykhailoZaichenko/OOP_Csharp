﻿using System;
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
    protected string Name;
    protected string Address;
    protected int RegistrationYear;

    public Organization(string name, string address, int year)
    {
        this.Name = name;
        this.Address = address;
        this.RegistrationYear = year;
    }

    public Organization() : this("DefaultOrg", "DefaultAddress", 2000) { }

    public string OrgName { get => this.Name; set => this.Name = value; }
    public string OrgAddress { get => this.Address; set => this.Address = value; }

    public int OrgRegistrationYear
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

    public override string ToString() => $"Organization name: {this.Name}, Address: {this.Address}, Year: {this.RegistrationYear}";

    // CompareTo method for sorting by name with null check
    public int CompareTo(Organization other) => this.Name.CompareTo(other?.Name);
    // CompareTo method for sorting by registration year
    public int Compare(Organization x, Organization y) => x.RegistrationYear.CompareTo(y.RegistrationYear);
}

// Helping class for sorting by address
public class OrganizationAddressComparer : IComparer<Organization>
{
    public int Compare(Organization x, Organization y) => x.OrgAddress.CompareTo(y.OrgAddress);
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
        get => new Organization(this.OrgName, this.OrgAddress, this.OrgRegistrationYear);
        set { this.OrgName = value.OrgName; this.OrgAddress = value.OrgAddress; this.OrgRegistrationYear = value.OrgRegistrationYear; }
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
            var book = new Book(new Person("Oksana", "Petryk", new DateTime(1995, 3, 3)), $"Book of {p.OrgName}", DateTime.Today, Size.Pocket);
            var employee = new Person("Andrii", "Zaychenko", new DateTime(1993, 4, 4));
            p.AddBooks(book);
            p.AddEmployee(employee);
            publishers.Add(p);
        }
    }

    public override string ToString()
    {
        string result = "";
        foreach (var publisher in publishers)
        {
            result += publisher.ToString() + "\n";
        }
        return result;
    }

    public string ToShortString()
    {
        string result = "";
        foreach (var publisher in publishers)
        {
            result += publisher.ToShortString() + "\n";
        }
        return result;
    }

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

    public List<Organization> GetOrganizations() => orgList;

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
            Organization org = new Organization(pub.OrgName, pub.OrgAddress, pub.OrgRegistrationYear);
            orgList.Add(org);
            stringList.Add(org.ToString());
            orgDict.Add(org, pub);
            stringDict.Add(org.ToString(), pub);
        }
    }

    public void MeasureSearchTimesFor(Organization org)
    {
        string orgStr = org.ToString();
        var sw = new Stopwatch();

        sw.Start(); bool inList = orgList.Contains(org); sw.Stop();
        Console.WriteLine($"List<Organization> — {(inList ? "found" : "not found")}: {sw.ElapsedTicks} ticks ({sw.Elapsed.TotalMilliseconds} ms)");

        sw.Restart(); bool inStrList = stringList.Contains(orgStr); sw.Stop();
        Console.WriteLine($"List<string> — {(inStrList ? "found" : "not found")}: {sw.ElapsedTicks} ticks ({sw.Elapsed.TotalMilliseconds} ms)");

        sw.Restart(); bool inDictKey = orgDict.ContainsKey(org); sw.Stop();
        Console.WriteLine($"Dictionary<Organization, Publisher> (by key) — {(inDictKey ? "found" : "not found")}: {sw.ElapsedTicks} ticks ({sw.Elapsed.TotalMilliseconds} ms)");

        sw.Restart(); bool inStrDictKey = stringDict.ContainsKey(orgStr); sw.Stop();
        Console.WriteLine($"Dictionary<string, Publisher> (by key) — {(inStrDictKey ? "found" : "not found")}: {sw.ElapsedTicks} ticks ({sw.Elapsed.TotalMilliseconds} ms)");

        sw.Restart(); bool inDictValue = orgDict.ContainsValue(
            new Publisher(org.OrgName, org.OrgAddress, DateTime.Today, org.OrgRegistrationYear)); sw.Stop();
        Console.WriteLine($"Dictionary<Organization, Publisher> (by value) — {(inDictValue ? "found" : "not found")}: {sw.ElapsedTicks} ticks ({sw.Elapsed.TotalMilliseconds} ms)");
    }

}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Creating PublisherCollection and adding elements ===\n");
        var collection = new PublisherCollection();

        collection.AddDefaults();
        collection.AddPublishers(
            new Publisher("Gamma", "Odesa", DateTime.Today, 2005),
            new Publisher("Delta", "Kharkiv", DateTime.Today, 2008)
        );

        // 1. Створити об'єкт типу PublisherCollection. Додати до колекції кілька різних елементів типу Publisher з різними значеннями полів і вивести об'єкт PublisherCollection.
        Console.WriteLine("Original PublisherCollection:\n");
        Console.WriteLine(collection.ToString());

        // 2. Для створеного об'єкта PublisherCollection викликати методи, які виконують сортування списку List <Publisher> за різними критеріями, і після кожного сортування вивести дані об'єкта. Виконати сортування^
            // за назвою організації;
            // за роком реєстрації;
            // за адресою реєстрації.
        Console.WriteLine("\n=== Sorted by Name ===");
        collection.SortByName();
        Console.WriteLine(collection.ToShortString());

        Console.WriteLine("\n=== Sorted by Registration Year ===");
        collection.SortByYear();
        Console.WriteLine(collection.ToShortString());

        Console.WriteLine("\n=== Sorted by Address ===");
        collection.SortByAddress();
        Console.WriteLine(collection.ToShortString());

        // 3. Створити об'єкт типу TestCollections. Викликати метод для пошуку в колекціях першого, центрального, останнього і елемента, що не входить в колекції. Вивести значення часу пошуку для всіх чотирьох випадків з поясненнями про те, для якої колекції і для якого елементу отримано значення.
        Console.WriteLine("\n=== Collection Performance Testing ===\n");
        var test = new TestCollections(1000);

        var orgList = test.GetOrganizations();
        var testItems = new[]
        {
            new { Label = "First element", Org = orgList[0] },
            new { Label = "Middle element", Org = orgList[orgList.Count / 2] },
            new { Label = "Last element", Org = orgList[^1] },
            new { Label = "Non-existing element", Org = new Organization("NotExist", "Nowhere", 1999) }
        };

        foreach (var item in testItems)
        {
            Console.WriteLine($" Searching: {item.Label}");
            test.MeasureSearchTimesFor(item.Org);
            Console.WriteLine();
        }
    }
}

