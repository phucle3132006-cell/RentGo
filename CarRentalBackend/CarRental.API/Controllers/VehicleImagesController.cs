using System.ComponentModel.DataAnnotations;
using CarRental.BLL.Interfaces;
using CarRental.Models.DTOs.VehicleImages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarRental.API.Controllers
{
    [ApiController]
    [Route("api/vehicles/{vehicleId:long}/images")]
    public class VehicleImagesController : ControllerBase
    {
        private readonly IVehicleImageService _vehicleImageService;

        public VehicleImagesController(IVehicleImageService vehicleImageService)
        {
            _vehicleImageService = vehicleImageService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<VehicleImageResponse>>> GetByVehicleId(
            [FromRoute, Range(1, long.MaxValue)] long vehicleId)
        {
            try
            {
                return Ok(await _vehicleImageService.GetByVehicleIdAsync(vehicleId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<VehicleImageResponse>> Upload(
            [FromRoute, Range(1, long.MaxValue)] long vehicleId,
            [FromForm, Required] IFormFile file,
            [FromForm] UploadVehicleImageRequest request)
        {
            if (file.Length == 0)
                return BadRequest(new { message = "File ảnh không được rỗng." });

            try
            {
                await using var stream = file.OpenReadStream();
                var image = await _vehicleImageService.UploadAsync(
                    vehicleId, stream, file.FileName, file.Length, request.IsPrimary);

                return Created(image.ImageUrl, image);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("primary")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> SetPrimary(
            [FromRoute, Range(1, long.MaxValue)] long vehicleId,
            [FromBody] SetPrimaryVehicleImageRequest request)
        {
            if (!await _vehicleImageService.SetPrimaryAsync(vehicleId, request.ImageId))
                return NotFound(new { message = "Không tìm thấy ảnh của xe." });

            return NoContent();
        }

        [HttpDelete("{imageId:long}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Delete(
            [FromRoute, Range(1, long.MaxValue)] long vehicleId,
            [FromRoute, Range(1, long.MaxValue)] long imageId)
        {
            if (!await _vehicleImageService.DeleteAsync(vehicleId, imageId))
                return NotFound(new { message = "Không tìm thấy ảnh của xe." });

            return NoContent();
        }
    }
}
