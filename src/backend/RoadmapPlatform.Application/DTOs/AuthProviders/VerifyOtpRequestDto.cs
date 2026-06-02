using System.ComponentModel.DataAnnotations;

namespace RoadmapPlatform.Application.DTOs.AuthProviders
{
    public class VerifyOtpRequestDto
    {
        [Required(ErrorMessage = "OTP is required")]
        public string? Otp { get; set; }
    }
}
