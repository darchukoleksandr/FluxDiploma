using ProtoBuf;

namespace Domain.Models
{
    [ProtoContract]
    public class OperationResponse<T>
    {
        [ProtoMember(1)]
        public string Error { get; set; }

        [ProtoMember(2)]
        public T Response { get; set; }

        public bool IsErrorOccured()
        {
            return !string.IsNullOrEmpty(Error);
        }

    }
}
