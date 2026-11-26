using CarRental.Models.Enums;

namespace CarRental.Models.DTOs.Payments;

public sealed record RevenueQuery(string? Period, DateOnly? Date, int? Month, int? Year,
    DateOnly? From, DateOnly? To);

public sealed record RevenuePoint(string Label, decimal Revenue);
public sealed record RevenueMethod(string Method, decimal Revenue);

public sealed record RevenueReport(string Period, DateOnly StartDate, DateOnly EndDate,
    DateOnly PreviousStartDate, DateOnly PreviousEndDate, decimal TotalRevenue,
    int SuccessfulPaymentCount, int PaidOrderCount, decimal CashRevenue,
    decimal BankTransferRevenue, decimal PreviousRevenue, decimal? GrowthRate,
    IReadOnlyList<RevenuePoint> RevenueData, IReadOnlyList<RevenueMethod> PaymentMethods);

public enum RevenueBucket { Hour, Day, Month, Year }

public sealed record RevenueGroup(int Bucket, PaymentMethod Method, decimal Amount, int Count);
