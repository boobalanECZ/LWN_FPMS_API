namespace DFN_BMS.Models
{
    public class LoginRequest
    {
        public string? Username { get; set; }

        public string? Password { get; set; }

        public bool MobilityWithoutCheck { get; set; }

        public string? DeviceId { get; set; }
    }
}
