using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using static SmartTicketingManagementApp.Pages.Support.IndexModel;
using static System.Net.WebRequestMethods;

namespace SmartTicketingManagementApp.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;

        public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<bool> CreateTicketAsync(object request)
        {
            var response = await _httpClient.PostAsJsonAsync("/tickets", request);
            return response.IsSuccessStatusCode;
        }

        public class AssignResponse
        {
            public int ticket_id { get; set; }
            public string assigned_team_id { get; set; } = string.Empty;
            public string assigned_team_name { get; set; } = string.Empty;
            public string reasoning { get; set; } = string.Empty;
            public bool persisted { get; set; }
            public string message { get; set; } = string.Empty;
        }

        public async Task<AssignResponse?> AssignTicketAsync(int ticketId, int topK = 5)
        {
            var body = new { ticket_id = ticketId, top_k = topK };
            var response = await _httpClient.PostAsJsonAsync("/assign", body);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<AssignResponse>();
        }
        public class SolutionResponse
        {
            public int ticket_id { get; set; }
            public string? solution { get; set; }
            public List<SourceItem>? sources { get; set; }
            public bool persisted { get; set; }
            public string? message { get; set; }
        }

        public class SourceItem
        {
            public int ticket_id { get; set; }
            public string? title { get; set; }
            public double? score { get; set; }
        }

        public async Task<SolutionResponse?> SuggestSolutionAsync(int ticketId, int topK = 1)
        {
            var body = new { ticket_id = ticketId, top_k = topK };
            var response = await _httpClient.PostAsJsonAsync("/solution", body);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SolutionResponse>();
        }

        public async Task<SimilarResponse?> FindSimilarAsync(int ticketId, int topK = 5)
        {
            var body = new { ticket_id = ticketId, top_k = topK };
            var response = await _httpClient.PostAsJsonAsync("/similar", body);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SimilarResponse>();
        }

        public class SimilarResponse
        {
            public List<SimilarTicket>? Results { get; set; }
        }

        public class SimilarTicket
        {
            public int Ticket_Id { get; set; }
            public double Score { get; set; }
            public string? Title { get; set; }
            public string? Answer { get; set; }
            public string? Assigned_Team_Name { get; set; }
            public string? Body { get; set; }
        }

        public class NotificationRequest
        {
            public int ticket_id { get; set; }
            public string recipient { get; set; } = string.Empty;
            public string user_name { get; set; } = string.Empty;
        }

        // 4 notification types
        public enum NotificationType
        {
            TicketAssignedUser,
            TicketAssignedTeam,
            TicketResolved,
            TicketCanceled
        }

        public async Task<bool> SendNotificationAsync(NotificationType type, NotificationRequest request)
        {
            var endpoint = type switch
            {
                NotificationType.TicketAssignedUser => "/notify/ticket-assigned/user",
                NotificationType.TicketAssignedTeam => "/notify/ticket-assigned/team",
                NotificationType.TicketResolved => "/notify/ticket-resolved",
                NotificationType.TicketCanceled => "/notify/ticket-canceled",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);

            if (response.IsSuccessStatusCode)
                return true;

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Failed to send notification {Type} for ticket {TicketId}. " +
                "StatusCode={StatusCode}, Body={Body}",
                type, request.ticket_id, response.StatusCode, body);

            return false;
        }

        public Task<bool> NotifyTicketAssignedUserAsync(int ticketId, string email, string userName) =>
            SendNotificationAsync(NotificationType.TicketAssignedUser,
                new NotificationRequest { ticket_id = ticketId, recipient = email, user_name = userName });

        public Task<bool> NotifyTicketAssignedTeamAsync(int ticketId, string email, string userName) =>
            SendNotificationAsync(NotificationType.TicketAssignedTeam,
                new NotificationRequest { ticket_id = ticketId, recipient = email, user_name = userName });

        public Task<bool> NotifyTicketResolvedAsync(int ticketId, string email, string userName) =>
            SendNotificationAsync(NotificationType.TicketResolved,
                new NotificationRequest { ticket_id = ticketId, recipient = email, user_name = userName });

        public Task<bool> NotifyTicketCanceledAsync(int ticketId, string email, string userName) =>
            SendNotificationAsync(NotificationType.TicketCanceled,
                new NotificationRequest { ticket_id = ticketId, recipient = email, user_name = userName });
    }
}


