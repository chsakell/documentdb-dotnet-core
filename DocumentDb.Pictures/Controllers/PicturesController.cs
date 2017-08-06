namespace DocumentDb.Pictures.Controllers
{
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using System.IO;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;
    using System;
    using Sakura.AspNetCore;

    public class PicturesController : Controller
    {
        private List<string> Categories;

        public PicturesController()
        {
            this.Categories = new List<string>()
            {
                "3D & Abstract", "Animals & Birds", "Anime", "Beach","Bikes", "Cars","Celebrations", "Celebrities","Christmas", "Creative Graphics","Cute", "Digital Universe","Dreamy & Fantasy", "Flowers","Games", "Inspirational","Love", "Military",
                "Music", "Movies","Nature", "Others","Photography", "Sports","Technology", "Travel & World","Vector & Designs"
            };
        }

        [ActionName("Index")]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 8)
        {

            var items = await DocumentDBRepository<PictureItem>.GetItemsAsync();
            return View(items.ToPagedList(pageSize, page));
        }


#pragma warning disable 1998
        [ActionName("Create")]
        public async Task<IActionResult> CreateAsync()
        {
            FillCategories();
            return View();
        }
#pragma warning restore 1998

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Id,Title,Category")] PictureItem item, IFormFile file)
        {
            if (ModelState.IsValid & file != null)
            {
                RequestOptions options = new RequestOptions { PreTriggerInclude = new List<string> { "createDate" } };
                Document document = await DocumentDBRepository<PictureItem>.CreateItemAsync(item, options);

                if (file != null)
                {
                    var attachment = new Attachment { ContentType = file.ContentType, Id = "wallpaper", MediaLink = string.Empty };
                    var input = new byte[file.OpenReadStream().Length];
                    file.OpenReadStream().Read(input, 0, input.Length);
                    attachment.SetPropertyValue("file", input);
                    ResourceResponse<Attachment> createdAttachment = await DocumentDBRepository<PictureItem>.CreateAttachmentAsync(document.AttachmentsLink, attachment, new RequestOptions() { PartitionKey = new PartitionKey(item.Category) });
                }

                return RedirectToAction("Index");
            }

            FillCategories();
            ViewBag.FileRequired = true;
            return View();
        }

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id, string category)
        {
            Document document = await DocumentDBRepository<Document>.GetItemAsync(id, category);
            PictureItem item = await DocumentDBRepository<PictureItem>.GetItemAsync(id, category);

            var attachLink = UriFactory.CreateAttachmentUri("Gallery", "Pictures", document.Id, "wallpaper");
            Attachment attachment = await DocumentDBRepository<PictureItem>.ReadAttachmentAsync(attachLink.ToString(), item.Category);

            var file = attachment.GetPropertyValue<byte[]>("file");

            if (file != null)
            {
                string bytes = Convert.ToBase64String(file);
                ViewBag.Image = string.Format("data:{0};base64,{1}", attachment.ContentType, bytes);
            }
            else
            {
                ViewBag.Image = string.Empty;
            }

            return View(item);
        }

        [ActionName("Edit")]
        public async Task<ActionResult> EditAsync(string id, string category)
        {
            if (id == null)
            {
                return BadRequest();
            }

            PictureItem item = await DocumentDBRepository<PictureItem>.GetItemAsync(id, category);
            if (item == null)
            {
                return NotFound();
            }

            FillCategories(category);

            Document document = await DocumentDBRepository<Document>.GetItemAsync(id, category);

            var attachLink = UriFactory.CreateAttachmentUri("Gallery", "Pictures", document.Id, "wallpaper");

            Attachment attachment = await DocumentDBRepository<PictureItem>.ReadAttachmentAsync(attachLink.ToString(), item.Category);

            if (attachment != null)
            {
                var file = attachment.GetPropertyValue<byte[]>("file");

                if (file != null)
                {
                    string bytes = Convert.ToBase64String(file);
                    ViewBag.Image = string.Format("data:{0};base64,{1}", attachment.ContentType, bytes);
                }
            }
            else
            {
                ViewBag.Image = string.Empty;
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync(PictureItem item, [Bind("oldCategory")] string oldCategory, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                Document document = null;

                if (item.Category == oldCategory)
                {
                    document = await DocumentDBRepository<PictureItem>.UpdateItemAsync(item.Id, item);
                }
                else
                {
                    await DocumentDBRepository<PictureItem>.DeleteItemAsync(item.Id, oldCategory);
                    document = await DocumentDBRepository<PictureItem>.CreateItemAsync(item);
                    
                }

                if (file != null)
                {
                    var attachLink = UriFactory.CreateAttachmentUri("Gallery", "Pictures", document.Id, "wallpaper");
                    Attachment attachment = await DocumentDBRepository<PictureItem>.ReadAttachmentAsync(attachLink.ToString(), item.Category);

                    //var attachment = new Attachment { ContentType = file.ContentType, Id = "wallpaper", MediaLink = string.Empty };
                    var input = new byte[file.OpenReadStream().Length];
                    file.OpenReadStream().Read(input, 0, input.Length);
                    attachment.SetPropertyValue("file", input);
                    ResourceResponse<Attachment> createdAttachment = await DocumentDBRepository<PictureItem>.ReplaceAttachmentAsync(attachment, new RequestOptions() { PartitionKey = new PartitionKey(item.Category) });
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
            await DocumentDBRepository<PictureItem>.DeleteItemAsync(id, category);
            return RedirectToAction("Index");
        }

        [ActionName("DeleteAll")]
        public ActionResult DeleteAll()
        {
            Categories.Add("All");
            FillCategories("All");
            return View();
        }

        [HttpPost]
        [ActionName("DeleteAll")]
        public async Task<ActionResult> DeleteAllAsync(string category)
        {
            if (category != "All")
            {
                var response = await DocumentDBRepository<PictureItem>.ExecuteStoredProcedureAsync("bulkDelete", "SELECT * FROM c", category);
            }
            else
            {
                foreach(string cat in Categories)
                {
                    await DocumentDBRepository<PictureItem>.ExecuteStoredProcedureAsync("bulkDelete", "SELECT * FROM c", cat);
                }
            }

            return RedirectToAction("Index");
        }

        private void FillCategories(string selectedCategory = null)
        {
            List<SelectListItem> items = new List<SelectListItem>();

            foreach (var category in Categories)
            {
                if (!string.IsNullOrEmpty(selectedCategory) && category == selectedCategory)
                {
                    items.Add(new SelectListItem { Text = category, Value = category, Selected = true });
                }
                else
                {
                    items.Add(new SelectListItem { Text = category, Value = category });
                }
            }

            ViewBag.Category = items;
        }
    }
}