using BookmarksApp.Models;
using DAL;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Mvc;

namespace BookmarksApp.Controllers
{
    public class BookmarksController : Controller
    {
        // A controller exist within the process of a request
        // After the response is provided its instance is deleted

        private void InitSessionVariables()
        {
            // Session is a dictionary that hold keys values specific to a session
            // Each user of this web application have their own Session
            // A Session has a default time out of 20 minutes, after time out it is cleared

            if (Session["CurrentBookmarkId"] == null) Session["CurrentBookmarkId"] = 0;
            if (Session["CurrentBookmarkTitle"] == null) Session["CurrentBookmarkTitle"] = "";
            if (Session["Search"] == null) Session["Search"] = false;
            if (Session["SearchString"] == null) Session["SearchString"] = "";
            if (Session["SearchCategory"] == null) Session["SearchCategory"] = "*";
            if (Session["GroupByTitles"] == null) Session["GroupByTitles"] = true;
            if (Session["SortAscending"] == null) Session["SortAscending"] = true;
        }
        private void ResetCurrentBookmarkInfo()
        {
            Session["CurrentBookmarkId"] = 0;
            Session["CurrentBookmarkTitle"] = "";
        }

        // This action produce a partial view of Bookmarks
        // It is meant to be called by an AJAX request (from client script)
        public ActionResult GetBookmarks(bool forceRefresh = false)
        {
            ViewBag.BookmarkCategories = DB.BookmarkCategories();

            IEnumerable<Bookmark> result = null;
            if (forceRefresh || DB.Bookmarks.HasChanged)
            {
                // forceRefresh is true when a related view is produce
                // DB.Bookmarks.HasChanged is true when a change has been applied on any Bookmark

                InitSessionVariables();
                string searchString = (string)Session["SearchString"];
                string searchCategory = (string)Session["SearchCategory"];

                if ((bool)Session["Search"] && (bool)Session["GroupByTitles"])
                {
                    if ((bool)Session["SortAscending"])
                        result = DB.Bookmarks.ToList().Where(c => c.Title.ToLower().Contains(searchString)).OrderBy(c => c.Title);
                    else
                        result = DB.Bookmarks.ToList().Where(c => c.Title.ToLower().Contains(searchString)).OrderByDescending(c => c.Title);

                    if (searchCategory != "*")
                        result = result.Where(c => c.Category.ToLower() == searchCategory.ToLower());
                }
                else
                {
                    if ((bool)Session["GroupByTitles"])
                    {
                        if ((bool)Session["SortAscending"])
                            result = DB.Bookmarks.ToList().OrderBy(c => c.Title);
                        else
                            result = DB.Bookmarks.ToList().OrderByDescending(c => c.Title);
                    }
                    else
                        result = DB.Bookmarks.ToList().OrderBy(c => c.Title);
                }
                return PartialView(result);
            }
            return null;
        }

        public ActionResult List()
        {
            ViewBag.BookmarkCategories = DB.BookmarkCategories();
            ResetCurrentBookmarkInfo();
            return View();
        }
        public ActionResult About()
        {
            return View();
        }

        public ActionResult Details(int id)
        {
            // Keep in Session the current id wich will be referred in
            // Edit and Delete action
            Session["CurrentBookmarkId"] = id;
            Bookmark Bookmark = DB.Bookmarks.Get(id);
            if (Bookmark != null)
            {
                Session["CurrentBookmarkTitle"] = Bookmark.Title;
                return View(Bookmark);
            }
            return RedirectToAction("List");
        }
        public ActionResult Create()
        {
            ViewBag.BookmarkCategories = DB.BookmarkCategories();
            return View(new Bookmark());
        }

        [HttpPost]
        /* Install anti forgery token verification attribute.
         * the goal is to prevent submission of data from a page 
         * that has not been produced by this application*/
        [ValidateAntiForgeryToken()]
        public ActionResult Create(Bookmark Bookmark)
        {
            DB.Bookmarks.Add(Bookmark);
            return RedirectToAction("List");
        }

        public ActionResult Edit()
        {
            // Note that id is not provided has a parameter.
            // It use the Session["CurrentBookmarkId"] set within
            // Details(int id) action
            // This way we prevent from malicious requests that could
            // modify or delete programatically the all the Bookmarks

            int id = Session["CurrentBookmarkId"] != null ? (int)Session["CurrentBookmarkId"] : 0;
            if (id != 0)
            {
                ViewBag.BookmarkCategories = DB.BookmarkCategories();
                Bookmark Bookmark = DB.Bookmarks.Get(id);
                if (Bookmark != null)
                    return View(Bookmark);
            }
            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        public ActionResult Edit(Bookmark Bookmark)
        {
            // Has explained earlier, id of Bookmark is stored server side an not provided in form data
            // passed in the method in order to prever from malicious requests

            int id = Session["CurrentBookmarkId"] != null ? (int)Session["CurrentBookmarkId"] : 0;

            // Make sure that the Bookmark of id really exist
            Bookmark storedBookmark = DB.Bookmarks.Get(id);
            if (storedBookmark != null)
            {
                Bookmark.Id = id; // patch the Id
                DB.Bookmarks.Update(Bookmark);
            }
            return RedirectToAction("Details/" + id);
        }

        // This action is ment to be called by an AJAX request
        // Return true if there is a name conflict
        // Look into validation.js for more details
        // and also into Views/Bookmarks/BookmarkForm.cshtml
        public JsonResult CheckTitleConflict(string Title)
        {
            int id = Session["CurrentBookmarkId"] != null ? (int)Session["CurrentBookmarkId"] : 0;
            // Response json value true if name is used in other Bookmarks than the current Bookmark
            return Json(DB.Bookmarks.ToList().Where(c => c.Title == Title && c.Id != id).Any(),
                        JsonRequestBehavior.AllowGet /* must have for CORS verification by client browser */);
        }

        public ActionResult Delete()
        {
            int id = Session["CurrentBookmarkId"] != null ? (int)Session["CurrentBookmarkId"] : 0;
            if (id != 0)
            {
                DB.Bookmarks.Delete(id);
            }
            return RedirectToAction("List");
        }
        
        public ActionResult ToggleSort()
        {
            Session["SortAscending"] = !(bool)Session["SortAscending"];
            return RedirectToAction("List");
        }

        public ActionResult GroupByTitles()
        {
            Session["GroupByTitles"] = true;
            return RedirectToAction("List");
        }

        public ActionResult GroupByCategories()
        {
            Session["GroupByTitles"] = false;
            return RedirectToAction("List");
        }

        public ActionResult ToggleSearch()
        {
            if (Session["Search"] == null) Session["Search"] = false;
            Session["Search"] = !(bool)Session["Search"];
            return RedirectToAction("List");
        }

        public ActionResult SetSearchString(string value)
        {
            Session["SearchString"] = value.ToLower();
            return RedirectToAction("List");
        }

        public ActionResult SetSearchCategory(string value)
        {
            Session["SearchCategory"] = value.ToLower();
            return RedirectToAction("List");
        }
    }
}