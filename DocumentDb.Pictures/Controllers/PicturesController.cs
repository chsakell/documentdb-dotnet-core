namespace DocumentDb.Pictures.Controllers
{
    using System.Net;
    using System.Threading.Tasks;
    //using System.Web.Mvc;
    //using Models;
    using Microsoft.AspNetCore.Mvc;
    using Models;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Http;
    using System.IO;
    using Microsoft.Azure.Documents;
    using Microsoft.Azure.Documents.Client;

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
        public async Task<IActionResult> Index()
        {
            var items = await DocumentDBRepository<PictureItem>.GetItemsAsync(d => d.Approved);
            return View(items);
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
        public async Task<ActionResult> CreateAsync([Bind("Id,Title,Description,Approved,Category")] PictureItem item, ICollection<IFormFile> files)
        {


            if (ModelState.IsValid)
            {
                Document document = await DocumentDBRepository<PictureItem>.CreateItemAsync(item);

                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileStream = new MemoryStream();
                        await file.CopyToAsync(fileStream);

                        //Create the attachment
                        using (fileStream)
                        {
                            Attachment attachment = await DocumentDBRepository<PictureItem>.CreateAttachmentAsync(document.AttachmentsLink, fileStream, null);
                        }
                    }
                }

                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind("Id,Title,Description,Approved,Category")] PictureItem item, [Bind("oldCategory")] string oldCategory)
        {
            if (ModelState.IsValid)
            {
                if (item.Category == oldCategory)
                {
                    await DocumentDBRepository<PictureItem>.UpdateItemAsync(item.Id, item);
                }
                else
                {
                    await DocumentDBRepository<PictureItem>.DeleteItemAsync(item.Id, oldCategory);
                    await DocumentDBRepository<PictureItem>.CreateItemAsync(item);
                }
                return RedirectToAction("Index");
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
            return View(item);
        }

        [ActionName("Delete")]
        public async Task<ActionResult> DeleteAsync(string id, string category)
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

        [ActionName("Details")]
        public async Task<ActionResult> DetailsAsync(string id, string category)
        {
            PictureItem item = await DocumentDBRepository<PictureItem>.GetItemAsync(id, category);
            return View(item);
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