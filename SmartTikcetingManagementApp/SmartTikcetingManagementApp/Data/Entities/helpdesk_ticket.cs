using System;
using System.Collections.Generic;

namespace SmartTicketingManagementApp.Data.Entities
{
    public partial class helpdesk_ticket
    {
        public int? ticket_id { get; set; }
        public string? title { get; set; }
        public string? user { get; set; }
        public string? status { get; set; }
        public string? priority { get; set; }
        public string? assigned_to { get; set; }
        public DateTime? created_at { get; set; }
        public string? description { get; set; }
        public string? answer { get; set; }
    }
}
