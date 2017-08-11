using DocumentDb.Pictures.Data;
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
        private IDocumentDBRepository<PictureItem> picturesRepository;

        public PictureItemImageViewComponent(IDocumentDBRepository<PictureItem> picturesRepository)
        {
            this.picturesRepository = picturesRepository;
        }

        public async Task<IViewComponentResult> InvokeAsync(PictureItem item)
        {
            string image = string.Empty;
            Document document = await this.picturesRepository.GetDocumentAsync(item.Id, item.Category);

            var attachLink = UriFactory.CreateAttachmentUri("Gallery", "Pictures", document.Id, "wallpaper");
            Attachment attachment = await this.picturesRepository.ReadAttachmentAsync(attachLink.ToString(), item.Category);

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
