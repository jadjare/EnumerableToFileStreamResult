using System.Collections.Generic;

namespace ToFileStreamResultTests.Fakes
{
    public class Person
    {
        public static IEnumerable<Person> GetFakePeople() => new List<Person>
        {
            new Person() {FirstName = "Paul", Surname = "Beare", Height = 1.82M},
            new Person() {FirstName = "Joseph", Surname = "Adjare", Height = 1.88M},
            new Person() {FirstName = "Andy", Surname = "Lovett", Height = 1.84M},
        };

        public string FirstName { get; set; }
        public string Surname { get; set; }
        public decimal Height { get; set; }

    }
}
