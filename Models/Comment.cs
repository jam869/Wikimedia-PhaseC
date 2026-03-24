using DAL;
using System;

namespace Models
{
    public class Comment : Record
    {
        public int MediaId { get; set; }
        public int UserId { get; set; }
        public string Text { get; set; }
        public DateTime CreationDate { get; set; }
        public int ParentId { get; set; } = 0; 
    }
}