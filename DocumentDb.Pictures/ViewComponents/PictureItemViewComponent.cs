using DocumentDb.Pictures.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.ViewComponents
{
    public class PictureItemViewComponent : ViewComponent
    {
        public PictureItemViewComponent()
        {

        }

        public async Task<IViewComponentResult> InvokeAsync(PictureItem item)
        {
            return View(item);
        }
    }
}
