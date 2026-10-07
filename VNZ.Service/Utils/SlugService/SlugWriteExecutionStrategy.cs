using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using VNZ.Repository;

namespace VNZ.Service.Utils.SlugService;

public sealed class SlugWriteExecutionStrategy : ExecutionStrategy
{
    private const int MaximumRetryCount = 20;
    private readonly string _slugIndexName;
    private readonly Func<Exception> _createRetryLimitException;

    public SlugWriteExecutionStrategy(
        AppDbContext dbContext,
        string slugIndexName,
        Func<Exception> createRetryLimitException)
        : base(dbContext, MaximumRetryCount, TimeSpan.Zero)
    {
        ArgumentNullException.ThrowIfNull(createRetryLimitException);

        if (slugIndexName != "IX_News_Article_Slug"
            && slugIndexName != "IX_Job_Post_Slug")
        {
            throw new ArgumentException(
                "Chỉ hỗ trợ unique index slug của NewsArticle hoặc JobPost.",
                nameof(slugIndexName));
        }

        _slugIndexName = slugIndexName;
        _createRetryLimitException = createRetryLimitException;
    }

    protected override bool ShouldRetryOn(Exception exception)
    {
        // EF Core đã gỡ lớp DbUpdateException trước khi gọi phương thức này.
        if (exception is not PostgresException postgresException)
        {
            return false;
        }

        if (postgresException.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        return string.Equals(
            postgresException.ConstraintName,
            _slugIndexName,
            StringComparison.Ordinal);
    }

    protected override TimeSpan? GetNextDelay(Exception lastException)
    {
        // Tối đa 20 lần thử lại, không tính lần ghi đầu tiên.
        var nextDelay = base.GetNextDelay(lastException);

        if (nextDelay == null)
        {
            throw _createRetryLimitException();
        }

        return nextDelay;
    }
}
