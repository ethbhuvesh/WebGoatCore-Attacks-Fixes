using Microsoft.EntityFrameworkCore;
using WebGoatCore.Models;

namespace WebGoatCore.Data
{
    public class BlogResponseRepository
    {
        private readonly NorthwindContext _context;

        public BlogResponseRepository(NorthwindContext context)
        {
            _context = context;
        }

        public void CreateBlogResponse(BlogResponse response)
        {
            //TODO: should put this in a try/catch
            // Use EntityFramework FromSQLRaw for faster query
            string Author = response.Author;
            int BlogEntryId = response.BlogEntryId;
            var ResponseDate = response.ResponseDate;
            string Contents = response.Contents;

            var responseBack = _context.BlogResponses.FromSqlInterpolated(
                $"INSERT INTO BlogResponses (Author, BlogEntryId, ResponseDate, Contents) VALUES ( {Author}, {BlogEntryId}, {ResponseDate}, {Contents} ); SELECT * FROM BlogResponses WHERE changes() = 1 AND Id = last_insert_rowid();").ToListAsync();
        }
    }
}
