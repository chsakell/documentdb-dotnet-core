namespace DocumentDb.Pictures.Controllers
{
    using System.Net;
    using System.Threading.Tasks;
    //using System.Web.Mvc;
    //using Models;
    using Microsoft.AspNetCore.Mvc;
    using Models;

    public class PicturesController : Controller
    {
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
            return View();
        }
#pragma warning restore 1998

        [HttpPost]
        [ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAsync([Bind("Id,Title,Description,Approved,Category")] PictureItem item)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<PictureItem>.CreateItemAsync(item);
                return RedirectToAction("Index");
            }

            return View(item);
        }

        [HttpPost]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditAsync([Bind("Id,Title,Description,Approved,Category")] PictureItem item)
        {
            if (ModelState.IsValid)
            {
                await DocumentDBRepository<PictureItem>.UpdateItemAsync(item.Id, item);
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
    }
}