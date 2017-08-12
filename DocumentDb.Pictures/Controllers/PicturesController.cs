namespace DocumentDb.Pictures.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using System;
    using Sakura.AspNetCore;
    using DocumentDb.Pictures.Data;
    using System.Linq;
    using AutoMapper;

    public class PicturesController : Controller
    {
        private IDocumentDBRepository<GalleryDBRepository> galleryRepository;

        public PicturesController(IDocumentDBRepository<GalleryDBRepository> galleryRepository)
        {
            this.galleryRepository = galleryRepository;
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 8, string filter = null)
        {
            await this.galleryRepository.InitAsync("Pictures");
            IEnumerable<PictureItem> items;

            if (string.IsNullOrEmpty(filter))
                items = await this.galleryRepository.GetItemsAsync<PictureItem>();
            else
            {
                items = await this.galleryRepository
                    .GetItemsAsync<PictureItem>(picture => picture.Title.ToLower().Contains(filter.Trim().ToLower()));
                ViewBag.Message = "We found " + (items as ICollection<PictureItem>).Count + " pictures for term " + filter.Trim();
            }
            return View(items.ToPagedList(pageSize, page));
        }

        [ActionName("Create")]
        public async Task<IActionResult> CreateAsync()
        {
            await FillCategoriesAsync();
            return View();
        }

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Id,Title,Category")] PictureItem item, IFormFile file)
        {
            if (ModelState.IsValid & file != null)
            {
                await this.galleryRepository.InitAsync("Pictures");

                RequestOptions options = new RequestOptions { PreTriggerInclude = new List<string> { "createDate" } };

                Document document = await this.galleryRepository.CreateItemAsync<PictureItem>(item, options);

                if (file != null)
                {
                    var attachment = new Attachment { ContentType = file.ContentType, Id = "wallpaper", MediaLink = string.Empty };
                    var input = new byte[file.OpenReadStream().Length];
                    file.OpenReadStream().Read(input, 0, input.Length);
                    attachment.SetPropertyValue("file", input);
                    ResourceResponse<Attachment> createdAttachment = await this.galleryRepository.CreateAttachmentAsync(document.AttachmentsLink, attachment, new RequestOptions() { PartitionKey = new PartitionKey(item.Category) });
                }

                return RedirectToAction("Index");
            }

            await FillCategoriesAsync();

            ViewBag.FileRequired = true;
            return View();
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id, string category)
        {
            if (id == null)
            {
                return BadRequest();
            }

            await this.galleryRepository.InitAsync("Pictures");

            Document document = await this.galleryRepository.GetDocumentAsync(id, category);

            //PictureItem item = await this.galleryRepository.GetItemAsync<PictureItem>(id, category);
            PictureItem item = Mapper.Map<PictureItem>(document);

            if (item == null)
            {
                return NotFound();
            }

            await FillCategoriesAsync(category);

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync(PictureItem item, [Bind("oldCategory")] string oldCategory, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                await this.galleryRepository.InitAsync("Pictures");

                Document document = null;

                if (item.Category == oldCategory)
                {
                    document = await this.galleryRepository.UpdateItemAsync(item.Id, item);

                    if (file != null)
                    {
                        var attachLink = UriFactory.CreateAttachmentUri("Gallery", "Pictures", document.Id, "wallpaper");
                        Attachment attachment = await this.galleryRepository.ReadAttachmentAsync(attachLink.ToString(), item.Category);

                        var input = new byte[file.OpenReadStream().Length];
                        file.OpenReadStream().Read(input, 0, input.Length);
                        attachment.SetPropertyValue("file", input);
                        ResourceResponse<Attachment> createdAttachment = await this.galleryRepository.ReplaceAttachmentAsync(attachment, new RequestOptions() { PartitionKey = new PartitionKey(item.Category) });
                    }
                }
                else
                {
                    await this.galleryRepository.DeleteItemAsync(item.Id, oldCategory);

                    document = await this.galleryRepository.CreateItemAsync(item);

                    if (file != null)
                    {
                        var attachment = new Attachment { ContentType = file.ContentType, Id = "wallpaper", MediaLink = string.Empty };
                        var input = new byte[file.OpenReadStream().Length];
                        file.OpenReadStream().Read(input, 0, input.Length);
                        attachment.SetPropertyValue("file", input);
                        ResourceResponse<Attachment> createdAttachment = await this.galleryRepository.CreateAttachmentAsync(document.AttachmentsLink, attachment, new RequestOptions() { PartitionKey = new PartitionKey(item.Category) });
                    }
                }

                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmedAsync([Bind("Id, Category")] string id, string category)
        {
            await this.galleryRepository.InitAsync("Pictures");

            await this.galleryRepository.DeleteItemAsync(id, category);

            return RedirectToAction("Index");
        }

        [ActionName("DeleteAll")]
        public async Task<ActionResult> DeleteAllAsync()
        {
            await FillCategoriesAsync("All", true);
            return View();
        }

        [HttpPost]
        [ActionName("DeleteAll")]
        public async Task<ActionResult> DeleteAllAsync(string category)
        {
            await this.galleryRepository.InitAsync("Categories");

            var categories = await this.galleryRepository.GetItemsAsync<CategoryItem>();

            await this.galleryRepository.InitAsync("Pictures");

            if (category != "All")
            {
                var response = await this.galleryRepository.ExecuteStoredProcedureAsync("bulkDelete", "SELECT * FROM c", categories.Where(cat => cat.Title.ToLower() == category.ToLower()).First().Title);
            }
            else
            {

                foreach (var cat in categories)
                {
                    await this.galleryRepository.ExecuteStoredProcedureAsync("bulkDelete", "SELECT * FROM c", cat.Title);
                }
            }

            if (category != "All")
            {
                await FillCategoriesAsync("All");
                ViewBag.CategoryRemoved = category;

                return View();
            }
            else
                return RedirectToAction("Index");
        }

        private async Task FillCategoriesAsync(string selectedCategory = null, bool toUpperCase = false)
        {
            IEnumerable<CategoryItem> categoryItems = null;

            await this.galleryRepository.InitAsync("Categories");

            List<SelectListItem> items = new List<SelectListItem>();

            if (!toUpperCase)
                categoryItems = await this.galleryRepository.GetItemsAsync<CategoryItem>();
            else
                categoryItems = this.galleryRepository.CreateDocumentQuery<CategoryItem>("SELECT c.id, udf.toUpperCase(c.title) as Title FROM Categories c", new FeedOptions() { EnableCrossPartitionQuery = true });

            if (!string.IsNullOrEmpty(selectedCategory) && !categoryItems.Any(item => item.Title == selectedCategory))
            {
                items.Add(new SelectListItem { Text = selectedCategory, Value = selectedCategory, Selected = true });
            }

            foreach (var category in categoryItems)
            {
                if (!string.IsNullOrEmpty(selectedCategory) && category.Title == selectedCategory)
                {
                    items.Add(new SelectListItem { Text = category.Title, Value = category.Title, Selected = true });
                }
                else
                {
                    items.Add(new SelectListItem { Text = category.Title, Value = category.Title });
                }
            }

            if (string.IsNullOrEmpty(selectedCategory))
            {
                items.First().Selected = true;
            }

            ViewBag.Category = items;
        }
    }
}