
using CarRental.BLL.Interfaces;
using CarRental.Models.DTOs.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.API.Controllers
{
    [ApiController]
    [Route("api/vehicles")]
    public class VehiclesController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehiclesController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        // GET: api/vehicles
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll([FromQuery] VehicleSearch search, CancellationToken cancellationToken)
        {
            var vehicles = await _vehicleService.GetAllAsync(search, cancellationToken);
            return Ok(vehicles);
        }

        // GET: api/vehicles/1
        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(long id)
        {
            var vehicle = await _vehicleService.GetByIdAsync(id);

            if (vehicle == null)
                return NotFound(new
                {
                    message = "Khong tim thay xe."
                });

            return Ok(vehicle);
        }

        // POST: api/vehicles
        [HttpPost]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Create(
            [FromBody] CreateVehicleRequest request)
        {
            try
            {
                var vehicle = await _vehicleService.CreateAsync(request);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = vehicle.Id },
                    vehicle);
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
                    message = "Khong the luu xe. Kiem tra rang buoc du lieu."
                });
            }
        }

        // PUT: api/vehicles/1
        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Update(
            long id,
            [FromBody] UpdateVehicleRequest request)
        {
            try
            {
                bool updated = await _vehicleService.UpdateAsync(id, request);

                if (!updated)
                    return NotFound(new
                    {
                        message = "Khong tim thay xe."
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
                    message = "Khong the cap nhat xe."
                });
            }
        }

        // DELETE: api/vehicles/1
        [HttpDelete("{id:long}")]
        [Authorize(Roles = "ADMIN")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                bool deleted = await _vehicleService.DeleteAsync(id);

                if (!deleted)
                    return NotFound(new
                    {
                        message = "Khong tim thay xe."
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
                    message = "Khong the xoa xe vi du lieu dang duoc su dung."
                });
            }
        }
    }
}
