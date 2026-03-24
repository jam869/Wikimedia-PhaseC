using DAL;
using System.Linq;

namespace Models
{
    public class CommentsRepository : Repository<Comment>
    {
        public void DeleteMediaComments(int mediaId)
        {
            var items = ToList().Where(c => c.MediaId == mediaId).ToList();
            foreach (var item in items) { Delete(item.Id); }
        }
        public void DeleteUserComments(int userId)
        {
            var items = ToList().Where(c => c.UserId == userId).ToList();
            foreach (var item in items) { Delete(item.Id); }
        }
    }
}