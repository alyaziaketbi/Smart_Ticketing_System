using System;
using System.Collections.Generic;

namespace SmartTicketingManagementApp.Data.Entities
{
    public partial class user
    {
        public user()
        {
            team_members = new HashSet<team_member>();
            tickets = new HashSet<ticket>();
        }

        public int user_id { get; set; }
        public string? name { get; set; }
        public string? email { get; set; }
        public string? user_role { get; set; }

        public virtual ICollection<team_member> team_members { get; set; }
        public virtual ICollection<ticket> tickets { get; set; }
    }
}
