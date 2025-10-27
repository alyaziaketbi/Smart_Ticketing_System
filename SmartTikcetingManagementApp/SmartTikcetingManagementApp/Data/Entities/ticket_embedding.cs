using System;
using System.Collections.Generic;

namespace SmartTicketingManagementApp.Data.Entities
{
    public partial class ticket_embedding
    {
        public int id { get; set; }
        public int? ticket_id { get; set; }
        public string? chunk_text { get; set; }

        public virtual ticket? ticket { get; set; }
    }
}
