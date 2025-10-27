using System;
using System.Collections.Generic;

namespace SmartTicketingManagementApp.Data.Entities
{
    public partial class ticket
    {
        public ticket()
        {
            ticket_embeddings = new HashSet<ticket_embedding>();
        }

        public int ticket_id { get; set; }
        public int? requester_id { get; set; }
        public string? subject { get; set; }
        public string? body { get; set; }
        public string? answer { get; set; }
        public string? suggested_answer { get; set; }
        public string? type { get; set; }
        public string? priority { get; set; }
        public string? assigned_team_id { get; set; }
        public int? assigned_team_user_id { get; set; }
        public string? suggested_assigned_team_id { get; set; }
        public string? status { get; set; }
        public DateTime? created_at { get; set; }
        public string? tag_1 { get; set; }
        public string? tag_2 { get; set; }
        public string? tag_3 { get; set; }
        public string? tag_4 { get; set; }
        public string? tag_5 { get; set; }
        public string? tag_6 { get; set; }
        public string? tag_7 { get; set; }
        public string? tag_8 { get; set; }

        public virtual team? assigned_team { get; set; }
        public virtual user? requester { get; set; }
        public virtual ICollection<ticket_embedding> ticket_embeddings { get; set; }
    }
}
