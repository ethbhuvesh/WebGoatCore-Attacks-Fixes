using WebGoatCore.Models;
using WebGoatCore.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;

namespace WebGoatCore.Controllers
{
    [Route("[controller]/[action]")]
    public class BlogController : Controller
    {
        private readonly BlogEntryRepository _blogEntryRepository;
        private readonly BlogResponseRepository _blogResponseRepository;

        HtmlEncoder _htmlencoder;

        public BlogController(BlogEntryRepository blogEntryRepository, BlogResponseRepository blogResponseRepository, NorthwindContext context, HtmlEncoder htmlencoder)
        {
            _blogEntryRepository = blogEntryRepository;
            _blogResponseRepository = blogResponseRepository;

            _htmlencoder = htmlencoder;
        }

        public IActionResult Index()
        {
            List<BlogEntry> blogs = _blogEntryRepository.GetTopBlogEntries();
            foreach (BlogEntry blog in blogs)
            {
                foreach (BlogResponse blogcont in blog.Responses)
                {
                    blogcont.Contents = _htmlencoder.Encode(blogcont.Contents);
                }
            }

            return View(blogs);
        }

        [HttpGet("{entryId}")]
        public IActionResult Reply(int entryId)
        {
            return View(_blogEntryRepository.GetBlogEntry(entryId));
        }

        [HttpPost("{entryId}")]
        public IActionResult Reply(int entryId, string contents)
        {
            var userName = User.Identity.Name ?? "Anonymous";
            var response = new BlogResponse()
            {
                Author = userName,
                Contents = contents,
                BlogEntryId = entryId,
                ResponseDate = DateTime.Now
            };
            _blogResponseRepository.CreateBlogResponse(response);

            return RedirectToAction("Index");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Create(string title, string contents)
        {
            var blogEntry = _blogEntryRepository.CreateBlogEntry(title, contents, User.Identity.Name!);
            return View(blogEntry);
        }

    }
}