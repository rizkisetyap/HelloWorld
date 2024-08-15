using HelloWorld.Services;
using HelloWorld.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace HelloWorld.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class TimesheetController : ControllerBase
    {
        private readonly ITimesheetServices _timesheetServices;

        public TimesheetController(ITimesheetServices timesheetServices)
        {
            _timesheetServices = timesheetServices;
        }

        [HttpPost]
        public async Task<IActionResult> UploadTemplate(IFormFile file)

        {
            var result = await _timesheetServices.UploadTemplate(file);
            return Ok(result);


        }
        [HttpPost]
        public async Task<IActionResult> GetReport()
        {
            var result = await _timesheetServices.ReportPressence();
            return Ok(result);
        }
        [HttpGet]
        public IActionResult Test()
        {
            return Ok("Test endpoint");
        }

        [HttpPost]
        public async Task<IActionResult> GenerateTimesheet([FromBody] GenerateTimesheetVM model)
        {
            var result = await _timesheetServices.GenerateTimesheet(model);

            if (result.IsSuccess)
            {
                var msString = JsonConvert.SerializeObject(result.Data);
                var byteArray = JsonConvert.DeserializeObject<byte[]>(msString);
                var memoryStream = new MemoryStream(byteArray);
                memoryStream.Position = 0;
                return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Timesheet");


            }
            return BadRequest(result);
        }
    }
}
