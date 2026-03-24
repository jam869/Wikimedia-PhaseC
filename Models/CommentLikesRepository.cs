using DAL;
using System.Linq;
namespace Models
{
    public class CommentLikesRepository : Repository<CommentLike>
    {
        public void DeleteUserCommentLikes(int userId)
        {
            var items = ToList().Where(l => l.UserId == userId).ToList();
            foreach (var item in items) { Delete(item.Id); }
        }
        public void DeleteCommentLikes(int commentId)
        {
            var items = ToList().Where(l => l.CommentId == commentId).ToList();
            foreach (var item in items) { Delete(item.Id); }
        }
    }
}