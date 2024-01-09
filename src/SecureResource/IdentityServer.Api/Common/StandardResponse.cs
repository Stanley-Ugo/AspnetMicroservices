namespace IdentityServer.Api.Common
{
    public class StandardResponse<T>
    {
        public bool Status { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }

        public static StandardResponse<T> GenerateResponse(string code = "200", string message = "success", dynamic data = null, bool status = true)
        {
            return new StandardResponse<T>
            {
                Status = status,
                Code = code,
                Message = message,
                Data = (T)data
            };
        }
    }
}
