using DAL;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static Controllers.AccessControl;

[UserAccess(Access.View)]
public class MediasController : Controller
{
    private void InitSessionVariables()
    {
        if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
        if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
        if (Session["Search"] == null) Session["Search"] = false;
        if (Session["SearchString"] == null) Session["SearchString"] = "";
        if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
        if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
        if (Session["SortByTitle"] == null) Session["SortByTitle"] = true;
        if (Session["MediaSortBy"] == null) Session["MediaSortBy"] = MediaSortBy.PublishDate;
        if (Session["SortAscending"] == null) Session["SortAscending"] = false;
        if (Session["SelectedUser"] == null) Session["SelectedUser"] = 0;

        // Paging handling (Phase B)
        if (Session["pageNum"] == null) Session["pageNum"] = 1;
        if (Session["firstPageSize"] == null) Session["firstPageSize"] = 12;
        if (Session["pageSize"] == null) Session["pageSize"] = 3;
        if (Session["EndOfMedias"] == null) Session["EndOfMedias"] = false;

        ValidateSelectedCategory();
    }

    private void ResetMediasPaging()
    {
        Session["pageNum"] = 1;
        Session["EndOfMedias"] = false;
    }

    private void ResetCurrentMediaInfo()
    {
        Session["CurrentMediaId"] = 0;
        Session["CurrentMediaTitle"] = "";
    }

    private void ValidateSelectedCategory()
    {
        if (Session["SelectedCategory"] != null)
        {
            var selectedCategory = (string)Session["SelectedCategory"];
            var Medias = DB.Medias.ToList().Where(c => c.Category == selectedCategory);
            if (Medias.Count() == 0)
                Session["SelectedCategory"] = "";
        }
    }

    // Extraction des données avec pagination (Phase B) + Tri par Likes (Phase C)
    private IEnumerable<Media> _getItems(int index, int nbItems)
    {
        InitSessionVariables();
        bool search = (bool)Session["Search"];
        string searchString = (string)Session["SearchString"];

        IEnumerable<Media> result = null;
        if (Models.User.ConnectedUser.IsAdmin) result = DB.Medias.ToList();
        else result = DB.Medias.ToList().Where(c => c.Shared || Models.User.ConnectedUser.Id == c.OwnerId);

        if (search)
        {
            // Surbrillance Phase B : on recherche dans Titre ET Description
            result = result.Where(c => c.Title.ToLower().Contains(searchString) || c.Description.ToLower().Contains(searchString));

            string SelectedCategory = (string)Session["SelectedCategory"];
            if (SelectedCategory != "") result = result.Where(c => c.Category == SelectedCategory);

            int selectedUser = (int)Session["SelectedUser"];
            if (selectedUser != 0) result = result.Where(c => c.OwnerId == selectedUser);
        }

        if ((bool)Session["SortAscending"])
        {
            switch ((MediaSortBy)Session["MediaSortBy"])
            {
                case MediaSortBy.Title: result = result.OrderBy(c => c.Title); break;
                case MediaSortBy.PublishDate: result = result.OrderBy(c => c.PublishDate); break;
                case MediaSortBy.Likes: result = result.OrderBy(c => DB.Likes.ToList().Count(l => l.MediaId == c.Id)); break;
            }
        }
        else
        {
            switch ((MediaSortBy)Session["MediaSortBy"])
            {
                case MediaSortBy.Title: result = result.OrderByDescending(c => c.Title); break;
                case MediaSortBy.PublishDate: result = result.OrderByDescending(c => c.PublishDate); break;
                case MediaSortBy.Likes: result = result.OrderByDescending(c => DB.Likes.ToList().Count(l => l.MediaId == c.Id)); break;
            }
        }

        int count = result.Count();
        if (count < nbItems + index)
        {
            nbItems = count - index;
            Session["EndOfMedias"] = true;
        }
        if (nbItems < 0) nbItems = 0;

        return result.Skip(index).Take(nbItems);
    }

    public ActionResult SetFirstPageSize(int pageSize)
    {
        Session["firstPageSize"] = pageSize;
        return null;
    }

    public ActionResult getNextMediasPage()
    {
        bool EndOfMedias = (bool)Session["EndOfMedias"];
        if (!EndOfMedias)
        {
            Session["pageNum"] = (int)Session["pageNum"] + 1;
            int pageNum = (int)Session["pageNum"];
            int pageSize = (int)Session["pageSize"];
            int firstPageSize = (int)Session["firstPageSize"];

            int index = pageNum == 1 ? 0 : (pageNum - 2) * pageSize + firstPageSize;
            int nbItems = pageNum == 1 ? firstPageSize : pageSize;

            IEnumerable<Media> mediasPage = _getItems(index, nbItems);
            return PartialView("GetMedias", mediasPage);
        }
        return null;
    }

    public ActionResult EndOfMedias()
    {
        return Json((bool)Session["EndOfMedias"], JsonRequestBehavior.AllowGet);
    }

    public ActionResult GetMedias(bool forceRefresh = false)
    {
        try
        {
            if (forceRefresh || DB.Users.HasChanged || DB.Medias.HasChanged || DB.Likes.HasChanged)
            {
                ResetMediasPaging();
                int firstPageSize = (int)Session["firstPageSize"];
                var items = _getItems(0, firstPageSize);
                return PartialView(items);
            }
            return null;
        }
        catch (System.Exception ex) { return Content("Erreur interne" + ex.Message, "text/html"); }
    }

    public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();
            bool search = (bool)Session["Search"];
            if (search) return PartialView();
            return null;
        }
        catch (System.Exception ex) { return Content("Erreur interne" + ex.Message, "text/html"); }
    }

    public ActionResult GetMediaDetails(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();
            int mediaId = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
            Media Media = DB.Medias.Get(mediaId);

            if (DB.Users.HasChanged || DB.Medias.HasChanged || DB.Likes.HasChanged || forceRefresh)
            {
                return PartialView("GetMediaDetails", Media);
            }
            return null;
        }
        catch (System.Exception ex) { return Content("Erreur interne" + ex.Message, "text/html"); }
    }

    public ActionResult List()
    {
        ResetCurrentMediaInfo();
        return View();
    }

    public ActionResult ToggleSearch()
    {
        if (Session["Search"] == null) Session["Search"] = false;
        Session["Search"] = !(bool)Session["Search"];
        return RedirectToAction("List");
    }

    public ActionResult SetMediaSortBy(MediaSortBy mediaSortBy)
    {
        Session["MediaSortBy"] = mediaSortBy;
        return RedirectToAction("List");
    }

    public ActionResult ToggleMediaSort()
    {
        InitSessionVariables();
        MediaSortBy currentSort = (MediaSortBy)Session["MediaSortBy"];

        if (currentSort == MediaSortBy.Title) Session["MediaSortBy"] = MediaSortBy.PublishDate;
        else if (currentSort == MediaSortBy.PublishDate) Session["MediaSortBy"] = MediaSortBy.Likes;
        else Session["MediaSortBy"] = MediaSortBy.Title;

        return RedirectToAction("List");
    }

    public ActionResult ToggleSort()
    {
        Session["SortAscending"] = !(bool)Session["SortAscending"];
        return RedirectToAction("List");
    }

    public ActionResult SetSearchString(string value)
    {
        Session["SearchString"] = value.ToLower();
        return RedirectToAction("List");
    }

    public ActionResult SetSearchCategory(string value)
    {
        Session["SelectedCategory"] = value;
        return RedirectToAction("List");
    }

    public ActionResult About()
    {
        return View();
    }

    public ActionResult Details(int id)
    {
        Session["CurrentMediaId"] = id;
        Media Media = DB.Medias.Get(id);
        Session["UserCanEditCurrentMedia"] = false;
        if (Media != null)
        {
            Session["CurrentMediaTitle"] = Media.Title;
            Session["UserCanEditCurrentMedia"] = Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin;
            return View(Media);
        }
        return RedirectToAction("List");
    }

    [UserAccess(Access.Write)]
    public ActionResult Create()
    {
        return View(new Media());
    }

    [HttpPost]
    [UserAccess(Access.Write)]
    [ValidateAntiForgeryToken()]
    public ActionResult Create(Media Media, string sharedCB = "off")
    {
        if (Media.IsValid()) // Validation côté serveur (Phase B)
        {
            Media.OwnerId = Models.User.ConnectedUser.Id;
            Media.Shared = sharedCB == "on";
            DB.Medias.Add(Media);
            DB.Events.Add("Create", Media.Title);
            return RedirectToAction("List");
        }
        return View(Media);
    }

    [UserAccess(Access.Write)]
    public ActionResult Edit()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media Media = DB.Medias.Get(id);
            if (Media != null)
            {
                if (Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                    return View(Media);
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    [UserAccess(Access.Write)]
    [HttpPost]
    [ValidateAntiForgeryToken()]
    public ActionResult Edit(Media Media, string sharedCB = "off")
    {
        if (Media.IsValid()) // Validation côté serveur (Phase B)
        {
            int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
            Media storedMedia = DB.Medias.Get(id);
            if (storedMedia != null)
            {
                Media.Id = id;
                Media.Shared = sharedCB == "on";
                Media.OwnerId = storedMedia.OwnerId;
                Media.PublishDate = storedMedia.PublishDate;
                DB.Medias.Update(Media);
            }
            return RedirectToAction("Details/" + id);
        }
        return View(Media);
    }

    [UserAccess(Access.Write)]
    public ActionResult Delete()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media Media = DB.Medias.Get(id);
            if (Media != null)
            {
                if (Media.OwnerId == Models.User.ConnectedUser.Id || Models.User.ConnectedUser.IsAdmin)
                {
                    DB.Likes.DeleteMediaLikes(id);
                    DB.Medias.Delete(id);
                    return RedirectToAction("List");
                }
                return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
            }
        }
        return Redirect("/Accounts/Login?message=Accès illégal! &success=false");
    }

    public JsonResult CheckConflict(string YoutubeId)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(),
                    JsonRequestBehavior.AllowGet);
    }

    [UserAccess(Access.View)]
    public ActionResult ToggleLike(int mediaId)
    {
        int userId = Models.User.ConnectedUser.Id;
        var existingLike = DB.Likes.ToList().FirstOrDefault(l => l.UserId == userId && l.MediaId == mediaId);
        if (existingLike != null) DB.Likes.Delete(existingLike.Id);
        else DB.Likes.Add(new Like { UserId = userId, MediaId = mediaId });
        return null;
    }

    public ActionResult GetMediasUsersList()
    {
        InitSessionVariables();
        var userIds = DB.Medias.ToList().Select(m => m.OwnerId).Distinct();
        var users = DB.Users.ToList().Where(u => userIds.Contains(u.Id)).OrderBy(u => u.Name).ToList();
        return PartialView(users);
    }

    public ActionResult SetSearchUser(int value)
    {
        Session["SelectedUser"] = value;
        return RedirectToAction("List");
    }
}