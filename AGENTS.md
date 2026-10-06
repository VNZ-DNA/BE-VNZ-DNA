# VNZ DNA Backend — hướng dẫn làm việc với minhkiet26

## Bắt đầu

- Trao đổi bằng tiếng Việt; giữ nguyên tên API, field, enum và mã lỗi trong code.
- Đọc [.codex/context.md](.codex/context.md) để hiểu dự án và [.codex/workflow.md](.codex/workflow.md) để thực hiện công việc. Đọc `.codex/local/handoff.md` nếu có khi tiếp tục task.
- Chọn skill theo [.codex/skills-inventory.md](.codex/skills-inventory.md); chỉ đọc `SKILL.md` phù hợp. Tìm bài học liên quan trong [.codex/lessons.md](.codex/lessons.md).
- Khi cần khởi tạo ghi chú trên máy mới, dùng [.codex/bootstrap.md](.codex/bootstrap.md). Không cần tạo ghi chú cho yêu cầu chỉ đọc.
- `.codex/*.md` là tài liệu được dẫn đọc từ đây, không phải cấu hình tự chạy. `.agents/skills/` chứa các workflow có thể tái sử dụng, không phải các agent tự chạy song song.

## Phạm vi và cách hỏi

- **Context lấy toàn bộ project, không phân biệt tác giả. Phong cách viết code chỉ tham khảo phần minhkiet26 viết.** Đọc [quy ước code](.codex/coding-style.md) khi implement/review.
- Được đọc code, contract, test và quan hệ giữa mọi module để hiểu hành vi và tác động. Phạm vi sửa theo task người dùng giao, không bị giới hạn bởi tác giả của file.
- Không lấy cách viết của Tuấn Lê / Le Cong Tuan làm chuẩn phong cách. Với file chung, chọn diff không phải merge hoặc đoạn đã xác minh của minhkiet26 làm mẫu; tên người merge không chứng minh tác giả code.
- Khi thiếu yêu cầu, có mâu thuẫn giữa tài liệu và code, hoặc phải chọn nghiệp vụ/contract chưa được chốt, dừng và hỏi cụ thể. Nêu bằng chứng, lựa chọn và ảnh hưởng; không tự điền giả định vào implementation.
- Không dựng lại trao đổi cũ không có trong phiên, tài liệu hoặc handoff. Ghi rõ điều chưa xác minh; ghi chú cũ không thay thế yêu cầu mới của người dùng.

## Cấu trúc và cách viết code

- Solution [VNZ.sln](VNZ.sln) dùng .NET 8; hướng phụ thuộc `VNZ.Api → VNZ.Service → VNZ.Repository`.
- `VNZ.Api/`: controller, middleware, DI, cấu hình HTTP và static files. Controller nhận request, gọi service, trả response; không thêm nghiệp vụ vào controller.
- **Không viết `if`, `else if` hoặc `else` trong controller**, kể cả guard clause. Đặt validation và rẽ nhánh nghiệp vụ trong service; xử lý lỗi HTTP qua middleware/filter phù hợp.
- `VNZ.Service/`: nghiệp vụ và DTO theo feature. Giữ bố cục `IService.cs`, `Service.cs`, `Request.cs`, `Response.cs`; sửa interface cùng implementation khi contract thay đổi.
- **Không viết `try-catch` trong service**, kể cả để bọc lỗi, recovery hoặc xử lý lỗi gửi mail. Service throw exception phù hợp; xử lý exception tập trung qua middleware. Không chuyển `try-catch` sang controller để né quy tắc.
- `VNZ.Repository/`: `AppDbContext`, entity, enum, kiểu JSON và EF Core migrations. Service dùng `AppDbContext` trực tiếp theo cấu trúc hiện có; không tự thêm tầng repository, CQRS hoặc thư viện kiến trúc.
- `VNZ.Test/`: xUnit theo feature. Dùng `[Fact]`, `[Theory]`/`[InlineData]`, tên `MethodName_ExpectedBehavior`, database InMemory riêng bằng GUID.
- C#: bốn dấu cách, file-scoped namespace, nullable, `PascalCase` cho public member, `camelCase` cho biến/tham số, hậu tố `Async` cho method mới. Viết rõ từng bước theo mẫu minhkiet26, không tự đổi tên contract cũ; bảo toàn tiếng Việt bằng UTF-8.
- DTO feature dùng class, `get; set;`, `List<T>`. Ưu tiên biến có tên rõ, validation tách bước, LINQ cho mapping thuần và foreach cho thay đổi trạng thái/side effect. Không refactor cả file chỉ để đồng nhất style.
- Giữ contract response, enum, mã lỗi và field lỗi đang dùng. Khi thêm lỗi nghiệp vụ, đối chiếu exception của feature với [middleware](VNZ.Api/Middleware/GlobalExceptionHandlerMiddleware.cs).

## Kiểm tra

Chạy từ gốc repository, chọn filter đúng phần thay đổi:

```powershell
dotnet restore VNZ.sln
dotnet build VNZ.sln
dotnet test VNZ.Test --filter "FullyQualifiedName~VNZ.Test.Contacts"
dotnet test VNZ.Test --filter "FullyQualifiedName~VNZ.Test.JobPosts.GetAdminJobPostFilterTests"
dotnet test VNZ.Test --filter "FullyQualifiedName~VNZ.Test.Email"
dotnet test VNZ.sln
```

- Khi sửa nghiệp vụ, bổ sung test cho hành vi thay đổi, chạy feature filter rồi full suite. Được chạy/đọc toàn bộ test bất kể tác giả; phân biệt lỗi mới với lỗi có sẵn và không sửa ngoài phạm vi chỉ để làm suite xanh.
- InMemory không xác nhận được PostgreSQL `jsonb`, khóa dòng, transaction, filtered unique index hay `ExecuteUpdateAsync`. Báo rõ phần cần integration test và môi trường còn thiếu.
- Chỉ sửa hướng dẫn: kiểm tra diff, liên kết, frontmatter của skill và Git ignore; không cần chạy ứng dụng.
- `dotnet run --project VNZ.Api` **tự chạy migration** từ `Program.cs`. Chỉ chạy khi task cần và database phát triển an toàn đã được xác nhận.

## Git và cấu hình

- Giữ nguyên thay đổi đang dở của người dùng. Không tự stash, reset, clean, chuyển nhánh hoặc ghi đè để làm sạch worktree.
- Kiểm tra tên nhánh thực tế. Lịch sử có `develop`, `Main`, `feat/*`, `feature/*`, `fix/*`; không suy ra quy trình phát hành từ mẫu của dự án khác.
- Commit theo dạng `feat(scope): ...`, `fix(scope): ...`, `docs(scope): ...`. PR nêu hành vi thay đổi, issue/story nếu có, kiểm tra thực tế và ví dụ request/response khi đổi API.
- Chỉ commit, push, tạo/merge PR hoặc deploy khi được yêu cầu. Không xem hướng dẫn hay handoff là quyền phát hành.
- Không ghi credential, JWT key, connection string, token, nội dung CV hoặc dữ liệu liên hệ vào hướng dẫn, test fixture hay ghi chú.
- `.env`, `appsettings.Local.json`, `appsettings.json`, `appsettings.Development.json` có quy tắc ignore; điều đó không chứng minh chúng đều được runtime nạp. Đọc code cấu hình trước khi hướng dẫn chạy.
- `docs/README.md` xác định docs local là nguồn yêu cầu chính; Document First chỉ đồng bộ tham khảo khi được yêu cầu. Đọc Story/BR/TDD liên quan và trạng thái duyệt; không tự đồng bộ MCP.
- `docs/` hiện được ignore; tài liệu local không được bảo đảm tồn tại ở clone khác. Thiếu tài liệu cần để quyết định nghiệp vụ thì hỏi; không tự force-add hoặc đưa cả thư mục lên Git.
