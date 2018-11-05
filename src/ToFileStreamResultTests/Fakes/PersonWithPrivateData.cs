using System.Collections.Generic;

namespace ToFileStreamResultTests.Fakes
{
    public class PersonWithPrivateData
    {
        public static IEnumerable<PersonWithPrivateData> GetFakePeople() => new List<PersonWithPrivateData>
        {
            new PersonWithPrivateData() {FirstName = "Paul", Surname = "Beare", Height = 1.82M, Password = "password123"},
            new PersonWithPrivateData() {FirstName = "Joseph", Surname = "Adjare", Height = 1.88M, Password = "p@55w0rd"},
            new PersonWithPrivateData() {FirstName = "Andy", Surname = "Lovett", Height = 1.84M, Password = "password"},
        };

        public string FirstName { get; set; }
        public string Surname { get; set; }
        private string Password { get; set; }  //Private members shouldn't be output
        internal int PasswordHint { get; set; } //Internal members shouldn't be output
        public decimal Height { get; set; }

    }
}
