﻿using SQLite;

namespace ComicWrap.Systems
{
    [Table("comic_pages")]
    public class ComicPageData
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int ComicId { get; set; }

        public string Name { get; set; }
        public string Url { get; set; }

        public bool IsRead { get; set; }
    }
}
