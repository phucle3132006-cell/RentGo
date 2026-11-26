
using CarRental.BLL.Interfaces;
using CarRental.Models.DTOs.VehicleModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers
{
    [ApiController]
    [Route("api/vehicle-models")]
    public class VehicleModelsController : ControllerBase
    {
        private readonly IVehicleModelService _service;

        public VehicleModelsController(IVehicleModelService service)
        {
            _service = service;
        }

        // GET: api/vehicle-models
        // Khách chưa đăng nhập cũng có thể xem danh sách mẫu xe.
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(result);
        }

        // GET: api/vehicle-models/1
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            var result = await _service.GetByIdAsync(id);

            if (result == null)
                return NotFound(new
                {
                    message = "Không tìm thấy mẫu xe."
                });

            return Ok(result);
        }

        // POST: api/vehicle-models
        // Chỉ ADMIN được thêm mẫu xe.
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Create(
            [FromBody] CreateVehicleModelRequest request)
        {
            try
            {
                var result = await _service.CreateAsync(request);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Id },
                    result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể thêm mẫu xe do ràng buộc dữ liệu."
                });
            }
        }

        // PUT: api/vehicle-models/1
        // Chỉ ADMIN được sửa mẫu xe.
        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateVehicleModelRequest request)
        {
            try
            {
                var updated = await _service.UpdateAsync(id, request);

                if (!updated)
                    return NotFound(new
                    {
                        message = "Không tìm thấy mẫu xe."
                    });

                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể cập nhật mẫu xe do ràng buộc dữ liệu."
                });
            }
        }

        // DELETE: api/vehicle-models/1
        // Chỉ ADMIN được xóa mẫu xe.
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var deleted = await _service.DeleteAsync(id);

                if (!deleted)
                    return NotFound(new
                    {
                        message = "Không tìm thấy mẫu xe."
                    });

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (DbUpdateException)
            {
                return Conflict(new
                {
                    message = "Không thể xóa mẫu xe do ràng buộc dữ liệu."
                });
            }
        }
    }
}
