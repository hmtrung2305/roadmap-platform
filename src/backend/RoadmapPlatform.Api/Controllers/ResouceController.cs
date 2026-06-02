using Microsoft.AspNetCore.Mvc;
using RoadmapPlatform.Application.Interfaces.Resources;

namespace RoadmapPlatform.Api.Controllers
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
                file.Length
            );

            return Ok(resource);
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