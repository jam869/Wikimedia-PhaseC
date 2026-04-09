using DAL;
using System.Linq;

namespace Models
{
    public class LikesRepository : Repository<Like>
    {
        public void DeleteMediaLikes(int mediaId)
        {
            var likesToDelete = ToList().Where(l => l.MediaId == mediaId).ToList();
            foreach (var like in likesToDelete) { Delete(like.Id); }
        }

        public void DeleteUserLikes(int userId)
        {
            var likesToDelete = ToList().Where(l => l.UserId == userId).ToList();
            foreach (var like in likesToDelete) { Delete(like.Id); }
        }
    }
}