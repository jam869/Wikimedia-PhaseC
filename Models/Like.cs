using DAL;

namespace Models
{
    public class Like : Record
    {
        public int UserId { get; set; }
        public int MediaId { get; set; }
    }
}