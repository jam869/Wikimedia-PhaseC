using DAL;
namespace Models
{
    public class CommentLike : Record
    {
        public int UserId { get; set; }
        public int CommentId { get; set; }
    }
}