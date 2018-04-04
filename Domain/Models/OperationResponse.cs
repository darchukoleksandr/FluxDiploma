namespace Domain.Models
{
    public class OperationResponse<T>
    {
        public string Error { get; set; }

        public T Response { get; set; }

        public bool IsErrorOccured()
        {
            return !string.IsNullOrEmpty(Error);
        }

    }
}
