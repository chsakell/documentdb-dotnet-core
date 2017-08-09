using DocumentDb.Pictures.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentDb.Pictures.ViewComponents
{
    public class PictureItemImageViewComponent : ViewComponent
    {
        public PictureItemImageViewComponent()
        {
            
        }

        public async Task<IViewComponentResult> InvokeAsync(PictureItem item)
        {
            string image = string.Empty;
            Document document = await DocumentDBRepository<Document>.GetItemAsync(item.Id, item.Category);

            var attachLink = UriFactory.CreateAttachmentUri("Gallery", "Pictures", document.Id, "wallpaper");
            Attachment attachment = await DocumentDBRepository<PictureItem>.ReadAttachmentAsync(attachLink.ToString(), item.Category);

            var file = attachment.GetPropertyValue<byte[]>("file");

            if (file != null)
            {
                string bytes = Convert.ToBase64String(file);
                image = string.Format("data:{0};base64,{1}", attachment.ContentType, bytes);
            }

            return View(new ImageVM() { Id = "img-" + item.Id, Src = image });
        }
    }
}
