using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using VNZ.Repository;
using VNZ.Repository.Entity;
using VNZ.Repository.Entity.Enum;
using VNZ.Service.ContactService;
using VNZ.Service.Exceptions;
using Xunit;
using ContactService = VNZ.Service.ContactService.Service;
using MailService = VNZ.Service.MailService;

namespace VNZ.Test.Contacts;

public class GetContactListFilterTests
{
    [Fact]
    public async Task GetContactListAsync_AppliesContactStatusAndReadStateIndependently()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ContactFilterTestDbContext(options);

        var expected = CreateContact("An unread", isRead: false, ContactStatus.NotContacted, 4);
        var readNotContacted = CreateContact("An read", isRead: true, ContactStatus.NotContacted, 3);
        var unreadContacted = CreateContact("An contacted", isRead: false, ContactStatus.Contacted, 2);
        var deleted = CreateContact("An deleted", isRead: false, ContactStatus.NotContacted, 1);
        deleted.IsDelete = true;

        dbContext.ContactInquiries.AddRange(expected, readNotContacted, unreadContacted, deleted);
        await dbContext.SaveChangesAsync();

        var service = CreateService(dbContext);

        var response = await service.GetContactListAsync(new Request.GetContactListRequest
        {
            Search = " an ",
            Status = ["Chưa liên hệ", "Đã liên hệ"],
            IsRead = ["false"],
            PageSize = 15
        });

        Assert.Equal(15, response.PageSize);
        Assert.Equal(2, response.TotalItems);
        Assert.Equal([expected.Id, unreadContacted.Id], response.Items.Select(item => item.Id));
        Assert.DoesNotContain(response.Items, item => item.Id == deleted.Id);
    }

    [Theory]
    [InlineData("unknown", "isRead")]
    [InlineData("1", "isRead")]
    public async Task GetContactListAsync_RejectsInvalidReadState(string value, string field)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ContactFilterTestDbContext(options);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ContactException>(() => service.GetContactListAsync(
            new Request.GetContactListRequest { IsRead = [value] }));

        Assert.Equal("CONTACT_QUERY_INVALID", exception.Code);
        Assert.Equal([field], exception.Fields);
    }

    [Fact]
    public async Task GetContactListAsync_RejectsInvalidContactStatus()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new ContactFilterTestDbContext(options);
        var service = CreateService(dbContext);

        var exception = await Assert.ThrowsAsync<ContactException>(() => service.GetContactListAsync(
            new Request.GetContactListRequest { Status = ["NotContacted"] }));

        Assert.Equal("CONTACT_QUERY_INVALID", exception.Code);
        Assert.Equal(["status"], exception.Fields);
    }

    private static ContactService CreateService(AppDbContext dbContext)
    {
        return new ContactService(
            dbContext,
            new StubMailService(),
            new HttpContextAccessor());
    }

    private static ContactInquiry CreateContact(
        string fullName,
        bool isRead,
        ContactStatus contactStatus,
        int createdAtOffset)
    {
        return new ContactInquiry
        {
            Id = Guid.NewGuid(),
            InquiryTopic = ContactInquiryTopic.TechnologyConsulting,
            FullName = fullName,
            Email = $"{Guid.NewGuid():N}@example.test",
            ConsentToDataProcessing = true,
            IsRead = isRead,
            ContactStatus = contactStatus,
            CreatedAt = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero)
                .AddMinutes(createdAtOffset)
        };
    }

    private sealed class StubMailService : MailService.IService
    {
        public Task SendAsync(MailService.MailContent content) => Task.CompletedTask;

        public Task<MailService.MailDeliveryResult> SendInterviewInvitationAsync(
            MailService.InterviewInvitationMailContent content) =>
            Task.FromResult(new MailService.MailDeliveryResult { IsSuccess = true });

        public Task<MailService.MailDeliveryResult> SendRejectionEmailAsync(
            MailService.RejectionEmailMailContent content) =>
            Task.FromResult(new MailService.MailDeliveryResult { IsSuccess = true });
    }

    private sealed class ContactFilterTestDbContext : AppDbContext
    {
        public ContactFilterTestDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobPost>().Ignore(jobPost => jobPost.Skills);
            modelBuilder.Entity<JobApplication>().Ignore(application => application.JobPostSnapshot);
            modelBuilder.Entity<Product>().Ignore(product => product.Content);
            modelBuilder.Entity<Product>().Ignore(product => product.Images);
        }
    }
}
