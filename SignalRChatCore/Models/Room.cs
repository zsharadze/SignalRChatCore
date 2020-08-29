using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChatCore.Models
{
    public class Room
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [Required]
        public virtual ApplicationUser UserAccount { get; set; }

        public virtual ICollection<Message> Messages { get; set; }
    }
}
