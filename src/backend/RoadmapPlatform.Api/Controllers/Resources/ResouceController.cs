using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.Interfaces.Resources;

namespace RoadmapPlatform.Api.Controllers.Resources
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResourcesController : ControllerBase
    {
        private readonly IResourceService _resourceService;

        public ResourcesController(IResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetResources()
        {
            var resources = await _resourceService.GetResourcesAsync();

            return Ok(resources);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadResource(
            [FromForm] string title,
            [FromForm] string skillName,
            IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please select a valid file.");
            }

            await using var fileStream = file.OpenReadStream();

            var resource = await _resourceService.UploadResourceAsync(
                title,
                skillName,
                file.FileName,
                fileStream,
                file.Length,
                file.ContentType
            );

            return Ok(resource);
        }

        [HttpGet("{id}/content")]
        public async Task<IActionResult> GetResourceContent(Guid id)
        {
            var content = await _resourceService.GetResourceContentAsync(id);

            return Content(content, "text/markdown");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResource(Guid id)
        {
            await _resourceService.DeleteResourceAsync(id);

            return Ok(new
            {
                message = "Resource and chunks deleted successfully."
            });
        }
    }
}
