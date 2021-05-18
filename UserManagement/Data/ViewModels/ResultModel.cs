namespace Data.ViewModels
{
    public class ResultModel
    {
        public object Data { get; set; }
        public string ErrorMessage { get; set; }
        public bool Succeed { get; set; }
    }

    public class PagingModel
    {
        public object Data { get; set; }
        public int TotalPages { get; set; }
    }


}
