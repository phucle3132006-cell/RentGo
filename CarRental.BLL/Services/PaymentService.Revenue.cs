using System.Globalization;
using CarRental.Models.DTOs.Payments;
using CarRental.Models.Enums;

namespace CarRental.BLL.Services;

public sealed partial class PaymentService
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    public async Task<RevenueReport> GetRevenueAsync(RevenueQuery query, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(VietnamOffset).DateTime);
        var period = query.Period?.Trim().ToLowerInvariant() ?? "day";
        var (start, end, previous, bucket) = GetRevenueWindow(query, period, today);
        var (groups, paidOrders, previousRevenue) = await payments.ReadRevenueAsync(
            ToRevenueUtc(previous), ToRevenueUtc(start), ToRevenueUtc(end), bucket, cancellationToken);

        var byBucket = groups.GroupBy(group => group.Bucket)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Amount));
        var methods = groups.GroupBy(group => group.Method)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.Amount));
        // Keep cash and transfer visible even if one or both have no receipts in this period.
        methods.TryAdd(PaymentMethod.CASH, 0m);
        methods.TryAdd(PaymentMethod.BANK_TRANSFER, 0m);
        var total = methods.Values.Sum();

        return new(period, start, end.AddDays(-1), previous, start.AddDays(-1), total,
            groups.Sum(group => group.Count), paidOrders, methods[PaymentMethod.CASH],
            methods[PaymentMethod.BANK_TRANSFER], previousRevenue,
            previousRevenue == 0 ? null : Math.Round((total - previousRevenue) / previousRevenue * 100m, 2),
            BuildRevenuePoints(start, end, bucket, byBucket),
            methods.OrderBy(item => item.Key).Select(item => new RevenueMethod(item.Key.ToString(), item.Value)).ToList());
    }

    private static DateTime ToRevenueUtc(DateOnly day) =>
        new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), VietnamOffset).UtcDateTime;

    private static (DateOnly Start, DateOnly End, DateOnly Previous, RevenueBucket Bucket) GetRevenueWindow(
        RevenueQuery query, string period, DateOnly today)
    {
        // End is the next local midnight. The previous window ends at the current window's start.
        var year = query.Year ?? today.Year;
        if ((period is "month" or "year") && (year is < 3 or > 9998))
            throw new ArgumentException("Năm phải nằm trong khoảng từ 3 đến 9998.");

        switch (period)
        {
            case "day":
                var day = query.Date ?? today;
                ValidateRevenueDates(day, day);
                return (day, day.AddDays(1), day.AddDays(-1), RevenueBucket.Hour);
            case "month":
                var month = query.Month ?? today.Month;
                if (month is < 1 or > 12) throw new ArgumentException("Tháng phải nằm trong khoảng từ 1 đến 12.");
                var monthStart = new DateOnly(year, month, 1);
                return (monthStart, monthStart.AddMonths(1), monthStart.AddMonths(-1), RevenueBucket.Day);
            case "year":
                var yearStart = new DateOnly(year, 1, 1);
                return (yearStart, yearStart.AddYears(1), yearStart.AddYears(-1), RevenueBucket.Month);
            case "custom":
                if (query.From is not { } from || query.To is not { } to)
                    throw new ArgumentException("Vui lòng nhập cả ngày bắt đầu và ngày kết thúc.");
                ValidateRevenueDates(from, to);
                var days = to.DayNumber - from.DayNumber + 1;
                return (from, to.AddDays(1), from.AddDays(-days),
                    days <= 62 ? RevenueBucket.Day : days <= 1096 ? RevenueBucket.Month : RevenueBucket.Year);
            default:
                throw new ArgumentException("Kỳ thống kê phải là day, month, year hoặc custom.");
        }
    }

    private static void ValidateRevenueDates(DateOnly from, DateOnly to)
    {
        if (from > to) throw new ArgumentException("Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.");
        var days = to.DayNumber - from.DayNumber + 1;
        if (to == DateOnly.MaxValue || from.DayNumber <= days)
            throw new ArgumentException("Khoảng ngày vượt giới hạn lịch được hỗ trợ khi tính kỳ trước.");
    }

    private static List<RevenuePoint> BuildRevenuePoints(DateOnly start, DateOnly end,
        RevenueBucket bucket, IReadOnlyDictionary<int, decimal> amounts)
    {
        var points = new List<RevenuePoint>();
        var cursor = bucket switch
        {
            RevenueBucket.Month => new DateOnly(start.Year, start.Month, 1),
            RevenueBucket.Year => new DateOnly(start.Year, 1, 1),
            _ => start
        };
        if (bucket == RevenueBucket.Hour)
        {
            for (var hour = 0; hour < 24; hour++)
                points.Add(new($"{hour:00}:00", amounts.GetValueOrDefault(hour)));
            return points;
        }

        var format = bucket switch { RevenueBucket.Day => "dd/MM/yyyy", RevenueBucket.Month => "MM/yyyy", _ => "yyyy" };
        for (var index = 0; cursor < end; index++)
        {
            points.Add(new(cursor.ToString(format, CultureInfo.InvariantCulture), amounts.GetValueOrDefault(index)));
            if (cursor.Year == 9999 && (bucket == RevenueBucket.Year ||
                (bucket == RevenueBucket.Month && cursor.Month == 12))) break;
            cursor = bucket switch
            {
                RevenueBucket.Day => cursor.AddDays(1),
                RevenueBucket.Month => cursor.AddMonths(1),
                _ => cursor.AddYears(1)
            };
        }
        return points;
    }
}
