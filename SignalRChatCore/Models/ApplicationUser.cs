using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChatCore.Models
{
    public class ApplicationUser : IdentityUser
    {
        public DateTime RegisteredDate { get; set; }
        public string DisplayName { get; set; }

        public string Avatar { get; set; }

        public virtual ICollection<Room> Rooms { get; set; }

        public virtual ICollection<Message> Messages { get; set; }
    }
}
