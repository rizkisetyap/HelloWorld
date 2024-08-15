namespace HelloWorld.ViewModels
{
    public class ApiResponseViewModel
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public object Data { get; set; }
        public string RedirectUrl { get; set; }
    }
}
