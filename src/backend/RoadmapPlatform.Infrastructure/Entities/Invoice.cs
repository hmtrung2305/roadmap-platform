using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Invoice
{
    public Guid InvoiceId { get; set; }

    public Guid UserId { get; set; }

    public decimal TotalAmount { get; set; }

    public string Currency { get; set; } = null!;

    public string Status { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();

    public virtual User User { get; set; } = null!;
}
