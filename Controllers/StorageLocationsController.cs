using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CtrlEat.DTOs;
using CtrlEat.Services;

namespace CtrlEat.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class StorageLocationsController : ControllerBase
    {
        private readonly StorageLocationService _service;

        public StorageLocationsController(StorageLocationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet]
        [ProducesResponseType(typeof(List<StorageLocationResponse>), 200)]
        public ActionResult<List<StorageLocationResponse>> GetStorageLocations(
            [FromQuery] string name = null,
            [FromQuery] string type = null)
        {
            var results = _service.SearchStorageLocations(new GetStorageLocationsRequest
            {
                Name = name,
                Type = type
            });
            return Ok(results);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(StorageLocationResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<StorageLocationResponse> GetStorageLocation([FromRoute] int id)
        {
            try
            {
                return Ok(_service.GetStorageLocation(id));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost]
        [ProducesResponseType(typeof(StorageLocationResponse), 201)]
        [ProducesResponseType(400)]
        public ActionResult<StorageLocationResponse> CreateStorageLocation(
            [FromBody] CreateStorageLocationRequest request)
        {
            try
            {
                var location = _service.CreateStorageLocation(request);
                return CreatedAtAction(nameof(GetStorageLocation), new { id = location.Id }, location);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(StorageLocationResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public ActionResult<StorageLocationResponse> UpdateStorageLocation(
            [FromRoute] int id,
            [FromBody] CreateStorageLocationRequest request)
        {
            try
            {
                return Ok(_service.UpdateStorageLocation(id, request));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult DeleteStorageLocation([FromRoute] int id)
        {
            try
            {
                _service.DeleteStorageLocation(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }
    }
}
