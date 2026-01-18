using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Hosting;
using BookmarksApp.Models;

namespace DAL
{
    public sealed class DB
    {
        #region singleton setup
        private static readonly DB instance = new DB();
        public static DB Instance { get { return instance; } }
        #endregion

        public static Repository<Bookmark> Bookmarks { get; set; } = new Repository<Bookmark>();

        public static List<string> BookmarkCategories()
        {
            List<string> Categories = new List<string>();
            foreach (Bookmark bookmark in Bookmarks.ToList().OrderBy(b => b.Category))
            {
                if (Categories.IndexOf(bookmark.Category) == -1)
                {
                    Categories.Add(bookmark.Category);
                }
            }
            return Categories;
        }
    }
}