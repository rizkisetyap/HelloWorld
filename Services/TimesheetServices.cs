using HelloWorld.ViewModels;
using System.Diagnostics;
using OfficeOpenXml;
using System.Globalization;
using Newtonsoft.Json;
using NuGet.Packaging.Signing;
using System.Net.Http;


namespace HelloWorld.Services
{
    public interface ITimesheetServices
    {
        Task<ApiResponseViewModel> UploadTemplate(IFormFile file);
        Task<ApiResponseViewModel> ReportPressence();
        List<DateTime> GenerateDate();
        Task<ApiResponseViewModel> GenerateTimesheet(GenerateTimesheetVM model);
    }
    public class TimesheetServices : ITimesheetServices
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public TimesheetServices(IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        public List<DateTime> GenerateDate()
        {
            var now = DateTime.Now;
            var list_date = new List<DateTime>();
            var DaysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            for (int day = 1; day <= DaysInMonth; day++)
            {
                var date = new DateTime(now.Year, now.Month, day);
                list_date.Add(date);

            }
            return list_date;
        }

        public async Task<ApiResponseViewModel> ReportPressence()
        {
            var response = new ApiResponseViewModel();
            if (!Directory.Exists(Path.Combine(_webHostEnvironment.WebRootPath, "Report")))
            {
                Directory.CreateDirectory(Path.Combine(_webHostEnvironment.WebRootPath, "Report"));
            }
            var outputPath = Path.Combine(_webHostEnvironment.WebRootPath, "Report", "report.json");
            try
            {
                var URL = _configuration["ApiURL"].ToString();
                string command = "curl -X GET http://202.148.18.28:8080/eabsensisdd_api/eabsensi/report/K-1721";
                Process process = new Process();
                process.StartInfo.FileName = @"C:\Program Files\Git\bin\bash.exe";
                process.StartInfo.Arguments = $"-c \"{command}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;


                // Start the process
                process.Start();
                // Read the output and error streams
                string output = process.StandardOutput.ReadToEnd();
                await File.WriteAllTextAsync(outputPath, output);
                response.IsSuccess = true;

            }
            catch (Exception e)
            {

                response.IsSuccess = false;
                response.Message = e?.InnerException?.Message ?? e!.Message;
            }

            return response;
        }

        public async Task<ApiResponseViewModel> UploadTemplate(IFormFile file)
        {
            var response = new ApiResponseViewModel();

            try
            {
                if (file == null) throw (new ArgumentNullException(nameof(file)));
                string fileName = "TemplateTimesheet.xlsx";

                string uploads = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", fileName);
                await using (var fs = new FileStream(uploads, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }
                response.IsSuccess = true;
                response.StatusCode = 200;
            }
            catch (ArgumentNullException e)
            {
                response.IsSuccess = false;
                response.Message = e?.InnerException?.Message ?? e!.Message;
                response.StatusCode = 400;
            }
            catch (Exception e)
            {
                response.IsSuccess = false;
                response.Message = e?.InnerException?.Message ?? e!.Message;
                response.StatusCode = 500;
            }
            return response;
        }

        public async Task<ApiResponseViewModel> GenerateTimesheet(GenerateTimesheetVM model)
        {

            var response = new ApiResponseViewModel();
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            try
            {
                string templatePath = Path.Combine(_webHostEnvironment.WebRootPath, "Uploads", "TemplateTimesheet.xlsx");
                var now = DateTime.Now;
                var report = await this.ReadReport();
                if (report == null) throw new Exception("Failed to read report.json");
                var datas = report!.data.datalist;
                var dates = GenerateDate().Order().ToList();
                var hariLibur = await _GetHariLibursAsync(dates.FirstOrDefault(), dates.LastOrDefault());
                datas = datas
                    .Where(x => x.checkin != null)
                    .Select(x => new ReportPressence
                    {
                        checkin = x.checkin,
                        checkout = x.checkout,
                        totalhours = x.totalhours?.Trim(),
                        CheckinDate = _ParseUnix(x.checkin),
                        CheckoutDate = _ParseUnix(x.checkout)
                    });

                var memoryStream = new MemoryStream();
                using (var package = new ExcelPackage(templatePath))
                {
                    var ws = package.Workbook.Worksheets.First();
                    var cellPeriod = ws.Cells[6, 6];
                    var cellDiperiksa = ws.Cells[49, 8, 49, 11];
                    var cellDisetujui = ws.Cells[49, 12, 49, 13];

                    cellDiperiksa.Value = $"Nama : {model.Diperiksa}";
                    cellDisetujui.Value = $"Nama : {model.Disetujui}";

                    int rowStart = 11;

                    foreach (var date in dates)
                    {
                        var cellTanggal = ws.Cells[rowStart, 2];
                        var cellDepartement = ws.Cells[rowStart, 13];
                        var cellHadir = ws.Cells[rowStart, 6];
                        var cellJamMasuk = ws.Cells[rowStart, 3];
                        var cellJamPulang = ws.Cells[rowStart, 4];
                        var cellKegiatan = ws.Cells[rowStart, 14, rowStart, 17];
                        var cellTotJam = ws.Cells[rowStart, 5];

                        var pressence = datas.FirstOrDefault(x => x.CheckinDate!.Value.Date == date.Date);
                        var tanggalMerah = hariLibur.Where(x => x.tanggalLibur.Date == date.Date).FirstOrDefault();
                        if (pressence != null)
                        {

                            cellHadir.Value = "H";
                            if (pressence.CheckinDate != null)
                            {
                                cellJamMasuk.Value = pressence.CheckinDate.Value.ToString("HH:mm");
                            }
                            if (pressence.CheckoutDate != null)
                            {
                                cellJamPulang.Value = pressence.CheckoutDate.Value.ToString("HH:mm");
                            }
                            if (pressence.totalhours != null)
                            {
                                cellTotJam.Value = pressence.totalhours;
                            }
                        }
                        if (tanggalMerah != null)
                        {
                            var cell = ws.Cells[rowStart, 1, rowStart, 17];
                            cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                            if (tanggalMerah.tanggalLibur.DayOfWeek == DayOfWeek.Sunday)
                            {
                                cellKegiatan.Value = "Minggu";
                            }
                            else if (tanggalMerah.tanggalLibur.DayOfWeek == DayOfWeek.Saturday)
                            {
                                cellKegiatan.Value = "Sabtu";
                            }
                            else
                            {
                                cellKegiatan.Value = tanggalMerah.keterangan.ToString();
                            }
                        }

                        cellTanggal.Value = date.ToString("dd-MMM-yy", new CultureInfo("id-ID"));
                        cellDepartement.Value = "Banking Operations Optimization";

                        // style
                        cellDepartement.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellDepartement.Style.VerticalAlignment = OfficeOpenXml.Style.ExcelVerticalAlignment.Center;

                        cellJamMasuk.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellJamPulang.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellHadir.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellKegiatan.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        cellTotJam.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;




                        rowStart++;
                    }

                    cellPeriod.Value = now.ToString("MMMM yyyy", new CultureInfo("id-ID"));


                    await package.SaveAsAsync(memoryStream);

                }
                var data = memoryStream.ToArray();
                response.IsSuccess = true;
                response.Data = data;

            }
            catch (Exception e)
            {

                response.IsSuccess = false;
                response.Message = e?.InnerException?.Message ?? e!.Message;
            }
            return response;

        }

        protected async Task<ReportVM?> ReadReport()
        {
            try
            {
                string filePath = Path.Combine(_webHostEnvironment.WebRootPath, "Report", "report.json");
                var stringJson = await File.ReadAllTextAsync(filePath);
                var report = JsonConvert.DeserializeObject<ReportVM>(stringJson);

                return report;
            }
            catch (Exception)
            {

                throw;
            }
        }
        private static DateTime? _ParseUnix(long? ts)
        {
            if (ts == null)
            {
                return null;
            }
            else
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ts ?? 0).ToLocalTime();

            }
        }

        private static async Task<List<HariLiburVM>?> _GetHariLibursAsync(DateTime start, DateTime end)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = $"http://103.101.225.233/QuantumX/Holiday/GetDateRange?startDate={start.ToString("yyyy-MM-dd")}&endDate={end.ToString("yyyy-MM-dd")}";
                    var response = await client.GetStringAsync(url);
                    var hariLiburs = JsonConvert.DeserializeObject<List<HariLiburVM>>(response);

                    return hariLiburs;
                }

            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
