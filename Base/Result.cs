namespace Base
{
    public class Result
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string MachineId { get; set; }
        public string Data { get; set; }

        public static Result Error(string message)
        {
            return new Result
            {
                Code = 0,
                Message = message
            };
        }

        public static Result BuilderStream(int code, string message = "", string machineId = "", string data = "")
        {
            return new Result
            {
                Code = code,
                Message = message,
                MachineId = machineId,
                Data = data
            };
        }
    }
}
