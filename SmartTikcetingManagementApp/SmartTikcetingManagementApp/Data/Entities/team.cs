using System;
using System.Collections.Generic;

namespace SmartTicketingManagementApp.Data.Entities
{
    public partial class team
    {
        public team()
        {
            team_members = new HashSet<team_member>();
            tickets = new HashSet<ticket>();
        }

        public string team_id { get; set; } = null!;
        public string? team_name { get; set; }

        public string? team_description { get; set; }

        public virtual ICollection<team_member> team_members { get; set; }
        public virtual ICollection<ticket> tickets { get; set; }

    }
}
