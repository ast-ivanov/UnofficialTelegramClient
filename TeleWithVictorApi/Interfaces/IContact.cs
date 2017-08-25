namespace TeleWithVictorApi.Interfaces
{
    public interface IContact
    {
        string FirstName { get; set; }
        string LastName { get; set; }
        string PhoneNumber { get; set; }

        void FillValues(string firstName, string lastName, string phone);
    }

    public interface IContactWithId : IContact
    {
        int Id { get; set; }
        void FillValues(string firstName, string lastName, string phone, int id);
    }
}
