// ЛР3: Варіант 9. Автор: Михайло Зайченко
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Lab3_Variant9
{
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

        public DateTime Date => BirthDate;

        public Person(string firstName, string lastName, DateTime birthDate)
        {
            FirstName = firstName;
            LastName = lastName;
            BirthDate = birthDate;
        }

        public Person() : this("DefaultFirstName", "DefaultLastName", new DateTime(2001, 1, 1)) { }

        public override string ToString() => $"{FirstName} {LastName}, born on {BirthDate.ToShortDateString()}";
        public virtual string ToShortString() => $"{FirstName} {LastName}";

        public override bool Equals(object obj)
        {
            if (obj is Person other)
                return FirstName == other.FirstName && LastName == other.LastName && BirthDate == other.BirthDate;
            return false;
        }

        public static bool operator ==(Person a, Person b) => Equals(a, b);
        public static bool operator !=(Person a, Person b) => !Equals(a, b);

        public override int GetHashCode() => HashCode.Combine(FirstName, LastName, BirthDate);

        public virtual object DeepCopy() => new Person(FirstName, LastName, BirthDate);
    }

    public enum Size { Pocket, Standard, Big }

    public class Book : IDateAndCopy
    {
        public Person Author { get; set; }
        public string Title { get; set; }
        public DateTime PublicationDate { get; set; }
        public Size Format { get; set; }

        public DateTime Date => PublicationDate;

        public Book(Person author, string title, DateTime publicationDate, Size format)
        {
            Author = author;
            Title = title;
            PublicationDate = publicationDate;
            Format = format;
        }

        public Book() : this(new Person(), "DefaultTitle", new DateTime(2001, 1, 1), Size.Standard) { }

        public override string ToString() => $"Author: {Author}, Title: {Title}, Format: {Format}, Published: {PublicationDate.ToShortDateString()}";

        public virtual object DeepCopy() => new Book((Person)Author.DeepCopy(), Title, PublicationDate, Format);
    }

    public class Organization
    {
        protected string Name;
        protected string Address;
        protected int RegistrationYear;

        public Organization(string name, string address, int year)
        {
            Name = name;
            Address = address;
            RegistrationYear = year;
        }

        public Organization() : this("DefaultOrg", "DefaultAddr", 2000) { }

        public string OrgName { get => Name; set => Name = value; }
        public string OrgAddress { get => Address; set => Address = value; }

        public int OrgYear
        {
            get => RegistrationYear;
            set
            {
                if (value > DateTime.Now.Year)
                    throw new ArgumentOutOfRangeException("Year cannot be in the future");
                RegistrationYear = value;
            }
        }

        public virtual object DeepCopy() => new Organization(Name, Address, RegistrationYear);

        public override bool Equals(object obj)
        {
            if (obj is Organization o)
                return Name == o.Name && Address == o.Address && RegistrationYear == o.RegistrationYear;
            return false;
        }

        public static bool operator ==(Organization a, Organization b) => Equals(a, b);
        public static bool operator !=(Organization a, Organization b) => !Equals(a, b);

        public override int GetHashCode() => HashCode.Combine(Name, Address, RegistrationYear);

        public override string ToString() => $"Name: {Name}, Address: {Address}, Year: {RegistrationYear}";
    }

    public class Publisher : Organization, IDateAndCopy, IEnumerable
    {
        private DateTime licenseExpiryDate;
        private List<Book> books = new List<Book>();
        private List<Person> employees = new List<Person>();

        public Publisher(string name, string address, DateTime licenseDate, int year)
            : base(name, address, year)
        {
            licenseExpiryDate = licenseDate;
        }

        public Publisher() : this("DefaultPublisher", "DefaultAddress", DateTime.Today, 2000) { }

        public DateTime LicenseExpiryDate { get => licenseExpiryDate; set => licenseExpiryDate = value; }
        public List<Book> Books => books;
        public List<Person> Employees => employees;

        public double PocketBookPercentage => books.Count == 0 ? 0 : (double)books.Count(b => b.Format == Size.Pocket) / books.Count * 100;

        public void AddBooks(params Book[] newBooks) => books.AddRange(newBooks);
        public void AddEmployee(params Person[] people) => employees.AddRange(people);

        public Organization OrganizationInfo
        {
            get => new Organization(Name, Address, RegistrationYear);
            set { Name = value.OrgName; Address = value.OrgAddress; RegistrationYear = value.OrgYear; }
        }

        public override string ToString() =>
            $"{base.ToString()}, License Expiry: {licenseExpiryDate.ToShortDateString()},\nBooks:\n{string.Join("\n", books)},\nEmployees:\n{string.Join("\n", employees)}";

        public string ToShortString() =>
            $"{base.ToString()}, License Expiry: {licenseExpiryDate.ToShortDateString()}, Total Books: {books.Count}";

        public object DeepCopy()
        {
            var copy = new Publisher(Name, Address, licenseExpiryDate, RegistrationYear);
            copy.books = books.Select(b => (Book)b.DeepCopy()).ToList();
            copy.employees = employees.Select(e => (Person)e.DeepCopy()).ToList();
            return copy;
        }

        public DateTime Date => licenseExpiryDate;

        public IEnumerator GetEnumerator()
        {
            foreach (var book in books)
                if (!employees.Contains(book.Author)) yield return book;
        }

        public IEnumerable<Book> BooksAfterYear(int year)
        {
            foreach (var book in books)
                if (book.PublicationDate.Year > year) yield return book;
        }

        public IEnumerable<Book> BooksByAuthor(string name)
        {
            foreach (var book in books)
                if ($"{book.Author.FirstName} {book.Author.LastName}" == name) yield return book;
        }

        public IEnumerable<Book> BooksByEmployeeAuthors()
        {
            foreach (var book in books)
                if (employees.Contains(book.Author)) yield return book;
        }
    }

    class Program
    {
        static void Main()
        {
            Organization org1 = new Organization("Name", "Addr", 2001);
            Organization org2 = new Organization("Name", "Addr", 2001);
            Console.WriteLine($"Reference Equals: {ReferenceEquals(org1, org2)}\nEquals: {org1.Equals(org2)}");
            Console.WriteLine($"Hash Codes: {org1.GetHashCode()}, {org2.GetHashCode()}");

            try
            {
                org1.OrgYear = DateTime.Now.Year + 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
            }

            var pub = new Publisher("Litera", "Kyiv", new DateTime(2030, 12, 31), 2020);
            var author1 = new Person("Ivan", "Ivanenko", new DateTime(1980, 1, 1));
            var author2 = new Person("Oksana", "Petryk", new DateTime(1990, 2, 2));
            var book1 = new Book(author1, "Intro to C#", new DateTime(2020, 1, 1), Size.Standard);
            var book2 = new Book(author2, "Advanced C#", new DateTime(2021, 2, 2), Size.Big);
            pub.AddBooks(book1, book2);
            pub.AddEmployee(author2);
            Console.WriteLine(pub.ToString());

            Console.WriteLine("\nOrganizationInfo:");
            Console.WriteLine(pub.OrganizationInfo);

            var pubCopy = (Publisher)pub.DeepCopy();
            pub.OrganizationInfo.OrgName = "ChangedName";
            pub.Books[0].Title = "ChangedTitle";

            Console.WriteLine("\nOriginal:");
            Console.WriteLine(pub.ToString());
            Console.WriteLine("\nCopy:");
            Console.WriteLine(pubCopy.ToString());

            Console.WriteLine("\nBooks published after 2020:");
            foreach (var book in pub.BooksAfterYear(2020))
                Console.WriteLine(book);

            Console.WriteLine("\nBooks by author Oksana Petryk:");
            foreach (var book in pub.BooksByAuthor("Oksana Petryk"))
                Console.WriteLine(book);

            Console.WriteLine("\nBooks by authors who are employees:");
            foreach (var book in pub.BooksByEmployeeAuthors())
                Console.WriteLine(book);

            Console.WriteLine("\nBooks by authors NOT employees:");
            foreach (var book in pub)
                Console.WriteLine(book);
        }
    }
}
