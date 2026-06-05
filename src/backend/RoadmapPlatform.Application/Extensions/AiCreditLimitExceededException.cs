using RoadmapPlatform.Application.DTOs.AiCredits;

namespace RoadmapPlatform.Application.Exceptions
{
    public class AiCreditLimitExceededException : Exception
    {
        public AiCreditLimitExceededException(AiCreditStatusDto status)
            : base("Daily AI credit limit reached.")
        {
            Status = status;
        }

        public AiCreditStatusDto Status { get; }
    }
}