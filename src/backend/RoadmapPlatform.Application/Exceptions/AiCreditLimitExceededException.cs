using RoadmapPlatform.Application.DTOs.AiCredits;

namespace RoadmapPlatform.Application.Exceptions
{
    public class AiCreditLimitExceededException : Exception
    {
        public AiCreditLimitExceededException(AiCreditStatusDto creditStatus)
            : base("Daily AI credit limit reached.")
        {
            CreditStatus = creditStatus;
        }

        public AiCreditStatusDto CreditStatus { get; }
    }
}
