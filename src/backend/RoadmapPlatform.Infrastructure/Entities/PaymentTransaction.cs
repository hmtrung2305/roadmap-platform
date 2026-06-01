using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class PaymentTransaction
{
    public Guid TransactionId { get; set; }

    public Guid InvoiceId { get; set; }

    public string? Gateway { get; set; }

    public string? GatewayTransactionId { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? WebhookPayload { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}
