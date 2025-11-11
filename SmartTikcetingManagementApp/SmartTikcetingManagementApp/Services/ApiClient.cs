using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using static SmartTicketingManagementApp.Pages.Support.IndexModel;

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
        }


    }
}


