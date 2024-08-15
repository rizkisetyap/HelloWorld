using NuGet.Packaging.Signing;

namespace HelloWorld.ViewModels
{
    public class ReportVM
    {
        public int code { get; set; }
        public string message { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public int totalrecord { get; set; }
        public IEnumerable<ReportPressence> datalist { get; set; }

    }
    public class ReportPressence
    {
        public long? checkin { get; set; }
        public long? checkout { get; set; }
        public string? totalhours { get; set; }
        public DateTime? CheckinDate { get; set; }
        public DateTime? CheckoutDate { get; set; }

        public ReportPressence()
        {
            if (checkin != null)
            {
                this.CheckinDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(checkin ?? 0).ToLocalTime();
            }
            if (checkout != null)
            {
                this.CheckoutDate = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(checkout ?? 0).ToLocalTime();
            }
        }
    }
    public class HariLiburVM
    {
        public int id { get; set; }
        public DateTime tanggalLibur { get; set; }
        public string keterangan { get; set; }
        public string creater { get; set; }
        public int year { get; set; }
    }

    public class GenerateTimesheetVM
    {
        public string Diperiksa { get; set; } = string.Empty;
        public string Disetujui { get; set; } = string.Empty;
    }
}
