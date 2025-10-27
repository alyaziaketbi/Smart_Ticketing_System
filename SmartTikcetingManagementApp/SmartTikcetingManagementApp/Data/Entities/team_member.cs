using System;
using System.Collections.Generic;

namespace SmartTicketingManagementApp.Data.Entities
{
    public partial class team_member
    {
        public int team_member_id { get; set; }
        public string? team_id { get; set; }
        public int? user_id { get; set; }

        public virtual team? team { get; set; }
        public virtual user? user { get; set; }
    }
}
