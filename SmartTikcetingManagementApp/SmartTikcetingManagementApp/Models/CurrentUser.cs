namespace SmartTicketingManagementApp.Models
{
    public sealed class CurrentUser
    {
        public int UserId { get; init; }
        public string Name { get; init; } = "";
        public string Email { get; init; } = "";
        public string Role { get; init; } = "Requester"; // "Agent" or "Requester"
    }

    public static class SessionKeys
    {
        public const string UserId = "u:id";
        public const string UserName = "u:name";
        public const string UserEmail = "u:email";
        public const string UserRole = "u:role";
    }

}
