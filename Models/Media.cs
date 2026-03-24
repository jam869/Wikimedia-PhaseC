using DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Models
{
    public enum MediaSortBy { Title, PublishDate }

    public class Media : Record
    {
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string YoutubeId { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.Now;

        public int OwnerId { get; set; } = 1;
        public bool Shared { get; set; } = true;
        [JsonIgnore]
        public User Owner => DB.Users.Get(OwnerId).Copy();
    }
}