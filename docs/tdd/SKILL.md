---
name: complete-tdd-writer
description: Viết TDD hoàn chỉnh cho một STORY-xxx trong Document First bằng User Story, Acceptance Criteria, Business Rule và repository hiện tại; dùng khi người dùng yêu cầu soạn TDD theo story và cần tài liệu đủ để Developer triển khai, QA kiểm thử, Reviewer duyệt.
---

# Complete TDD Writer

## Mục tiêu

Khi người dùng yêu cầu “viết TDD cho STORY-xxx”, tạo một Technical Design Document bám đúng Story và toàn bộ Business Rule chi phối trong đúng project Document First.

TDD hoàn chỉnh phải:

- Phủ mọi Main Flow, Alternative Flow, Exception Flow và Acceptance Criteria.
- Truy vết được mỗi quyết định thiết kế về Story hoặc Business Rule có thật.
- Bám kiến trúc, convention, data model và error contract của repository hiện tại.
- Đủ chi tiết để Developer triển khai và QA thiết kế test mà không phải đoán.
- Không bịa field, endpoint, status, rule, document key hoặc hành vi hệ thống.
- Không chứa TODO, TBD, placeholder hoặc câu hỏi mở khi được tuyên bố là hoàn chỉnh.

Skill này chỉ soạn TDD. Không sửa source code. Chỉ ghi TDD lên Document First khi người dùng yêu cầu lưu hoặc tạo tài liệu.

## Phạm vi kích hoạt

Kích hoạt khi người dùng nói:

- Viết TDD cho STORY-xxx.
- Thiết kế kỹ thuật cho STORY-xxx.
- Dựa vào Story và Business Rule để tạo TDD.
- Hoàn thiện TDD của một Story trong Document First.

Không dùng cho việc chỉ review TDD đã có, triển khai code, viết unit test hoặc system test.

## Nguồn sự thật và mức ưu tiên

Dùng nguồn theo thứ tự:

1. User Story được chỉ định, gồm metadata, conditions, flows, Acceptance Criteria, Non-Functional và Out of Scope.
2. Business Rule được Story tham chiếu hoặc thực sự chi phối flow.
3. TDD, API contract, ADR và tài liệu liên quan đã có trong cùng project.
4. Repository hiện tại: kiến trúc, entity, endpoint, convention, migration, error model và integration.
5. Giả định kỹ thuật tối thiểu khi các nguồn trên không quyết định được.

Nếu context trên Document First khác với entity, field, enum, quan hệ, constraint hoặc hành vi đang có trong code/DB, phải dừng việc soạn TDD và hỏi người dùng hướng xử lý. Không tự thêm, xóa, đổi tên, sửa field/entity, thay đổi schema hoặc tự suy diễn target design. Chỉ tiếp tục sau khi người dùng xác nhận phương án.

## Quy trình lấy context bắt buộc

### 1. Xác định đúng project

- Dùng project người dùng nêu rõ.
- Nếu chỉ có STORY-xxx mà không có project và key có thể trùng, liệt kê project hoặc hỏi đúng một câu để xác định.
- Không tìm Story ở project khác chỉ vì cùng tên hoặc gần nghĩa.
- Không tạo TDD trong project khác với project chứa Story.
- Ghi nhớ projectId đã xác định cho toàn bộ lượt làm việc.

### 2. Đọc Story có cấu trúc

Đọc STORY-xxx bằng công cụ lấy Story có cấu trúc để thu thập:

- Statement và context.
- Preconditions và trigger.
- Main Flow.
- Alternative Flow.
- Exception Flow.
- Acceptance Criteria.
- Non-Functional.
- Out of Scope.
- References tới TDD, Business Rule và tài liệu khác.
- documentKey, approvalState và versionLabel.

Nếu Story không tồn tại hoặc sai loại tài liệu, dừng và báo chính xác; không tự thay bằng Story gần giống.

### 3. Đọc đầy đủ Business Rule

- Lấy mọi Business Rule được Story tham chiếu.
- Duyệt danh sách rule của project khi cần phát hiện rule chi phối nhưng chưa được liên kết.
- Khi duyệt, đi hết phân trang; kết quả search không chứng minh danh sách đã đầy đủ.
- Đọc nội dung đầy đủ của từng rule, gồm When, Then, Except, Notes và effective date.
- Ghi documentKey cho từng rule.
- Không dùng rule từ project khác.
- Nếu reference bị gãy, ghi nhận là unresolved; không tự suy đoán nội dung.

### 4. Đọc context liên quan

Đọc các TDD, API contract, ADR hoặc tài liệu liên quan để giữ nhất quán:

- Tên endpoint và version API.
- Error envelope và errors.code.
- Entity, field, relationship và naming.
- Authentication và authorization.
- Transaction, idempotency và concurrency.
- External integration.
- Convention diagram và cấu trúc TDD của project.

Không sao chép thiết kế cũ nếu nó mâu thuẫn với Story hoặc Business Rule hiện tại.

### 5. Đối chiếu repository

Đọc hướng dẫn repository và code liên quan, ưu tiên tìm bằng tên entity, endpoint, command/query, service, validator và error code.

Xác định:

- Module hiện có và module cần thêm hoặc sửa.
- Controller/endpoint hiện tại.
- Request/response DTO.
- Entity, database configuration, index và migration.
- Service/repository/integration liên quan.
- Authentication/authorization.
- Transaction boundary.
- Error/result convention.
- Test hoặc fixture thể hiện hành vi hiện tại.

Không sửa code trong bước này.

## Ma trận truy vết bắt buộc trước khi viết

Tạo ma trận làm việc cho mọi yêu cầu:

| Nguồn | Yêu cầu | Thành phần chịu trách nhiệm | Thiết kế/API/Data | Error/guard | Diagram | Testable outcome |
|---|---|---|---|---|---|---|

Quy tắc:

- Mỗi Acceptance Criterion có ít nhất một dòng.
- Mỗi bước Main/Alternative/Exception Flow có ít nhất một thành phần xử lý.
- Mỗi Business Rule có guard, action hoặc constraint tương ứng.
- Mỗi Non-Functional có quyết định kỹ thuật hoặc ghi rõ không tác động thiết kế.
- Mỗi Out of Scope phải xuất hiện trong Non-goals.
- Không bắt đầu viết bản cuối nếu còn yêu cầu chưa có thành phần chịu trách nhiệm.

Ma trận là công cụ kiểm tra nội bộ. Chỉ đưa vào TDD khi project yêu cầu; nếu không, thể hiện truy vết trong References và nội dung các section.

## Phân tích thiết kế

Với mỗi flow, xác định:

1. Actor và quyền.
2. Trigger và precondition.
3. Request path, query, header và body.
4. Validation cú pháp.
5. Validation nghiệp vụ.
6. Dữ liệu cần đọc và thứ tự đọc.
7. Dữ liệu live, source, derived và snapshot.
8. Component xử lý.
9. External call nếu có.
10. Transaction begin, write, commit và rollback.
11. Response thành công.
12. Exception, HTTP status và errors.code.
13. Logging, audit, trace và dữ liệu nhạy cảm.
14. Idempotency, concurrency và retry nếu có liên quan.

Validation rẻ và độc lập phải thực hiện trước external call hoặc transaction dài. Không ghi dữ liệu thành công một phần nếu business operation yêu cầu tính nguyên tử.

## Ma trận bao phủ API bắt buộc

Trước khi viết phần Internal API, lập một ma trận riêng cho từng endpoint. Phạm vi “đầy đủ” nghĩa là mọi lớp đầu vào và mọi kết quả có thể đạt tới theo Story, Business Rule, API convention và repository; không chỉ Happy Path.

Ma trận dùng các cột:

| Vị trí | Field/tình huống | Giá trị hoặc lớp tương đương | Điều kiện/Business Rule | Kết quả | HTTP | errors.code | Response body | Side effect |
|---|---|---|---|---|---|---|---|---|

### Bao phủ đầu vào

Với mỗi path parameter, query parameter, header, body field, array item và nested field, kiểm tra các trường hợp có ý nghĩa:

- Field hợp lệ điển hình.
- Bị thiếu khi required và bị thiếu khi optional.
- Null khi nullable và null khi non-nullable.
- Empty string, whitespace-only và chuỗi sau trim.
- Sai kiểu JSON hoặc sai kiểu primitive.
- Sai format: UUID, URL, email, date, datetime, enum hoặc pattern.
- Giá trị nhỏ nhất, lớn nhất, ngay dưới min và ngay trên max.
- Số âm, số không, số thập phân hoặc overflow khi kiểu dữ liệu liên quan.
- Chuỗi quá dài, ký tự Unicode, ký tự đặc biệt và nội dung cần escape khi có liên quan.
- Enum hợp lệ, enum không tồn tại và khác biệt casing.
- Array rỗng, một phần tử, tối đa phần tử, vượt giới hạn và phần tử trùng.
- Object thiếu nested field, nested field null hoặc object dư field bị cấm.
- Hai field hợp lệ riêng lẻ nhưng vi phạm cross-field rule.
- ID tồn tại, không tồn tại, sai loại, đã xóa, inactive, thuộc người dùng khác hoặc ngoài tenant.
- Giá trị trùng dữ liệu hiện có, stale version hoặc concurrent update nếu feature có ràng buộc tương ứng.
- Pagination/filter/sort mặc định, hợp lệ, biên, không có kết quả và không hợp lệ nếu endpoint hỗ trợ.
- File/media sai loại, sai kích thước hoặc lỗi upload nếu endpoint nhận file.

Chỉ loại bỏ trường hợp không thể xảy ra hoặc không liên quan đến kiểu field/Story. Khi loại bỏ một lớp quan trọng, ghi rõ lý do.

### Bao phủ đầu ra

Với mỗi kết quả có thể đạt tới, mô tả đầy đủ:

- Success response cho từng Main Flow và Alternative Flow.
- Response khi danh sách có dữ liệu và danh sách rỗng.
- Response khi optional field có giá trị và khi bằng null.
- Response cho created, accepted, no-content hoặc partial result nếu contract có dùng.
- Validation error theo từng field hoặc từng nhóm field có cùng contract.
- Authentication failure và authorization failure.
- Not found, ownership/tenant mismatch, inactive, deleted và wrong-type.
- Business Rule violation.
- Duplicate/conflict, idempotency replay và concurrent modification nếu áp dụng.
- Rate limit hoặc quota nếu thuộc scope.
- External timeout, transport error, provider rejection và malformed provider response.
- Database/persistence/transaction failure.
- Unexpected internal error theo error envelope chuẩn.

Mỗi response phải chỉ rõ HTTP status, errors.code, toàn bộ body, header đặc biệt nếu có, field nullable/conditional và database side effect. Không dùng dấu ba chấm, “các field còn lại”, “tương tự trên” hoặc response rút gọn.

### Quy tắc gom trường hợp

- Viết một ví dụ riêng cho mỗi hành vi hoặc response contract khác nhau.
- Chỉ được gom nhiều input khi chúng trả cùng HTTP status, errors.code, response shape và side effect; phải liệt kê rõ các input được gom.
- Không tạo hàng loạt ví dụ vô nghĩa cho mọi chuỗi ký tự có thể có; dùng lớp tương đương và giá trị biên để chứng minh bao phủ.
- Mọi dòng trong ma trận phải xuất hiện trong API Examples hoặc được dẫn tới một example đại diện có cùng contract.
- Mọi response trong Examples phải có entry tương ứng trong Error Codes hoặc success contract.

## Cấu trúc TDD bắt buộc

Giữ nguyên tên và thứ tự heading sau.

# TDD-{KEY}

## Document Info

Gồm:

- Feature.
- Author.
- Reviewer.
- Status: Draft khi tạo mới.
- Version: v1.0 khi project không quy định khác.
- Updated At theo YYYY-MM-DD.

Không tự nhận một người cụ thể là Author hoặc Reviewer nếu nguồn không xác nhận.

## Context & Goals

### Problem

Chỉ mô tả vấn đề nghiệp vụ, actor, trạng thái hiện tại và tác động. Không đưa giải pháp kỹ thuật vào Problem.

### Goals

Liệt kê kết quả kỹ thuật đo được và truy được về Acceptance Criteria hoặc Business Rule.

### Non-goals

Chuyển toàn bộ Out of Scope của Story thành Non-goals và bổ sung giới hạn kỹ thuật có căn cứ.

## Architecture

Mô tả component, trách nhiệm, ranh giới module, luồng dữ liệu và dependency. Nêu module hiện có cần sửa và module mới cần tạo.

Bắt buộc có Mermaid flowchart thể hiện Client, API, Auth, application/service, domain/repository, database và external service nếu có.

Sau diagram ghi Notes về:

- Ownership và source of truth.
- Dữ liệu live so với snapshot.
- Transaction boundary.
- Idempotency/concurrency.
- Điều kiện không được gọi external service hoặc không được ghi database.

## Sequence Diagram

Tạo Mermaid sequenceDiagram trả lời “ai gọi ai và message gì”.

Phải bao phủ authentication/authorization, parse/validation, dependency read/write, toàn bộ flow, external call, transaction và response. Không dùng bước chung chung như Process request khi có thể ghi rõ hành động.

## Activity Diagram

Tạo Mermaid flowchart TD trả lời “xử lý theo bước nào và rẽ nhánh ở đâu”. Phải thể hiện guard, validation order, alternative path, failure path, external call, transaction và kết thúc. Nội dung phải khớp Sequence Diagram.

## State Diagram

Tạo Mermaid stateDiagram-v2 trả lời “entity thay đổi trạng thái thế nào”. Mỗi transition có event hoặc guard. Nếu Story không có stateful entity, vẫn giữ section và ghi rõ không phát sinh state transition, kèm lý do.

## Data Model

Mô tả table/entity, field, type, required/nullable, default, key, relationship, constraint, index, ownership, snapshot, lineage, migration và compatibility. Dùng Mermaid erDiagram khi có từ hai entity liên quan. Field mới phải được đánh dấu là proposed change.

## Internal API

## API contract chuẩn dùng toàn hệ thống

Mọi API trong TDD phải sử dụng envelope `ApiResponse` theo hợp đồng bên dưới. Response model không generic và có các property `IsSuccess`, `Message`, `Data`, `Errors`, `TraceId`, `TimestampUtc`. Controller gọi `ResponseBuilder.SuccessResponse(...)` hoặc `ResponseBuilder.ErrorResponse(...)`; không tự dựng envelope tại từng controller. JSON trả về dùng camelCase.

Response thành công:

    {
      "success": true,
      "message": "Đăng nhập thành công.",
      "data": {
        "id": "7b8d50fa-9868-4fb7-b8d4-9c8d77d9b87c"
      },
      "errors": null,
      "traceId": null,
      "timestampUtc": "2026-09-22T10:00:00Z"
    }

Response lỗi:

    {
      "success": false,
      "message": "Không tìm thấy bài đăng tuyển.",
      "data": null,
      "errors": {
        "code": "JOB_POST_NOT_FOUND",
        "fields": []
      },
      "traceId": "00-1bd9b83f1d0610560f0e52aed8196506-2bf630f9ce1a8a1a-00",
      "timestampUtc": "2026-09-22T10:00:00Z"
    }

Quy ước bắt buộc:

- message là thông báo tiếng Việt ngắn gọn, có thể hiển thị cho người dùng; không đưa exception, stack trace, SQL hay dữ liệu nhạy cảm vào đây.
- errors.code là mã máy ổn định, viết UPPER_SNAKE_CASE; frontend và test phải dựa vào mã này, không dựa vào nội dung message.
- errors là object hoặc null. Khi lỗi thuộc validation/business/API contract, object chứa code ổn định UPPER_SNAKE_CASE và fields là mảng tên field camelCase; dùng fields rỗng khi lỗi không gắn với field. Lỗi không có mã/field bổ sung có thể trả errors=null.
- traceId do server sinh để tra cứu log; response thành công có thể để null.
- timestampUtc luôn có trong response; ResponseBuilder gán `DateTime.UtcNow`, JSON serialize theo UTC ISO-8601.
- Không thêm messageCode ở root. Mã lỗi, nếu có, chỉ nằm trong errors.code.
- `SuccessResponse(data, message, traceId)` đặt `IsSuccess=true`, gán `Data`/`Message`/`TraceId`, để `Errors=null` và gán `TimestampUtc=DateTime.UtcNow`.
- `ErrorResponse(errors, message, traceId)` đặt `IsSuccess=false`, gán `Errors`/`Message`/`TraceId`, để `Data=null` và gán `TimestampUtc=DateTime.UtcNow`.
- Mọi ví dụ response trong TDD phải thể hiện đủ sáu field success, message, data, errors, traceId, timestampUtc.

### Quy ước enum trong API

Trong domain, service, validator và unit test, enum phải luôn được xử lý bằng enum C# kiểu hóa, ví dụ JobPostStatus.Open; không được so sánh bằng label tiếng Việt. Label chỉ là dữ liệu hiển thị cho UI.

Mọi field enum xuất hiện trong success response phải trả duy nhất label tiếng Việt dưới dạng string, lấy đúng từ Display(Name) của enum trong code. Không trả giá trị số, tên enum C# hoặc đồng thời cả raw value và label. Ví dụ: JobPostStatus.Open trả "Đang tuyển"; EmploymentType.FullTime trả "Toàn thời gian". TDD phải ghi type string, tập nhãn hợp lệ và nguồn Display(Name) cho từng field enum trong response; API test phải xác nhận đúng label tiếng Việt.

Quy ước label chỉ áp dụng cho response. Khi endpoint nhận enum trong request, frontend gửi tên enum C# chính xác, phân biệt hoa/thường, ví dụ "Open", "FullTime" hoặc "Senior"; không gửi label tiếng Việt và không gửi giá trị số. API phải parse sang enum C# trước khi gọi domain/service, từ chối tên không tồn tại bằng errors.code phù hợp. TDD phải nêu rõ tập tên enum hợp lệ cho từng request field.

Quy ước HTTP và mã lỗi:

- Tất cả endpoint thuộc khu vực quản trị dùng tiền tố `/api/v1/admin`. Endpoint xác thực công khai dùng `/api/v1/auth`; không đặt endpoint quản trị dưới `/api/admin` hoặc tiền tố khác.

| Tình huống | HTTP | errors.code |
|---|---:|---|
| Request thiếu/sai dữ liệu hoặc vi phạm validation | 400 | RESOURCE_VALIDATION_FAILED |
| Chưa xác thực, token thiếu/không hợp lệ/hết hạn | 401 | AUTH_UNAUTHENTICATED |
| Đăng nhập sai thông tin | 401 | AUTH_INVALID_CREDENTIALS |
| Tài khoản bị vô hiệu hóa | 401 | AUTH_ACCOUNT_INACTIVE |
| Đã xác thực nhưng không có quyền | 403 | AUTH_FORBIDDEN |
| Không tìm thấy tài nguyên | 404 | RESOURCE_NOT_FOUND |
| Xung đột dữ liệu hoặc quy tắc nghiệp vụ không cho phép thao tác | 409 | RESOURCE_CONFLICT |
| Lỗi không dự kiến | 500 | RESOURCE_OPERATION_FAILED |

RESOURCE trong bảng là quy ước đặt tên, phải thay bằng tên cụ thể khi viết TDD, ví dụ JOB_POST, NEWS_ARTICLE, MEMBER hoặc DASHBOARD; không được để nguyên chuỗi RESOURCE trong API contract cuối cùng. API tạo mới trả 201, đọc/cập nhật/truy vấn thành công trả 200; chỉ dùng 204 khi response không cần body và vẫn phải được Story/TDD quy định rõ.

Khi implement, controller gọi `ResponseBuilder` để tạo success/error response; middleware dùng cùng chuẩn response builder khi xử lý exception. Validator/service cung cấp thông tin lỗi để response có errors.code và errors.fields nhất quán. HTTP status được thiết lập riêng trên response. TDD phải nêu chính xác HTTP status, mã lỗi, field lỗi và envelope cho từng Exception Flow.

### Endpoints

Với mỗi endpoint ghi:

- Method, path, actor, permission và mục đích.
- Toàn bộ path parameter, query parameter, header và request body.
- Với từng field: type, required/optional, nullable, default, trim/normalization, format, enum, min/max, array limit và nguồn giá trị.
- Unknown-field policy: ignore hay reject.
- Cross-field rule và validation order.
- Toàn bộ success response field, type, nullable, source và điều kiện xuất hiện; field enum trong response phải là label tiếng Việt theo quy ước enum.
- Dependency, external call, transaction, side effect, idempotency và concurrency.
- Danh sách HTTP status và errors.code có thể trả về.

### Examples

Tạo ví dụ theo ma trận bao phủ API, gồm:

1. Mỗi Happy Path khác response hoặc side effect.
2. Mỗi Alternative Flow.
3. Mỗi Exception Flow.
4. Mỗi lớp validation đầu vào có errors.code hoặc response khác nhau.
5. Authentication và authorization.
6. Not found, ownership, inactive, deleted và wrong-type khi áp dụng.
7. Mỗi Business Rule violation.
8. Empty result, null optional field và pagination boundary khi áp dụng.
9. Duplicate, conflict, idempotency và concurrency khi áp dụng.
10. External, upload, persistence và transaction failure khi áp dụng.

Mỗi ví dụ phải có:

- Tiêu đề nêu đúng điều kiện.
- Request đầy đủ: method, URL, header cần thiết, query và body.
- Response đầy đủ: HTTP status, header đặc biệt và toàn bộ JSON body.
- Expected side effect: record tạo/sửa, không ghi dữ liệu hoặc rollback.
- Business Rule/Acceptance Criterion liên quan.

UUID, timestamp, enum, casing, nullability và envelope phải nhất quán với Fields và repository.

### Error Codes

Registry đầy đủ cho mọi lỗi trong Story, diagram, ma trận API và examples. Mỗi entry gồm code, HTTP status, điều kiện, field liên quan, client-visible detail, response schema, retryability và tác động dữ liệu. Mỗi Exception Flow và mỗi error response phải ánh xạ tới một entry.

## External API

Luôn giữ heading.

### Endpoints

Nếu không có external API, ghi rõ feature không gọi hệ thống bên thứ ba. Nếu có, ghi provider, operation, auth, timeout, retry, idempotency và data classification.

### Fields

Ghi field request/response với type, required, nullable, format, range, default, source và mapping nội bộ.

### Error Handling

Mô tả timeout, transport error, provider error, malformed response, retry exhaustion, mapping sang lỗi nội bộ và transaction behavior.

### Quirks

Chỉ ghi hành vi bất thường có thật và cách phòng vệ. Không dùng Quirks để chứa giả định chưa xác nhận.

## References

### User Stories

Ghi STORY-xxx, title, version/approval state đã dùng.

### Business Rules

Ghi từng BR key, title, version/approval state đã dùng.

### Use Cases

Ghi document key có thật hoặc “Không có tài liệu Use Case riêng”.

### Others

Ghi TDD, ADR, API spec, migration, source module hoặc external contract đã dùng. Chỉ dùng document key có thật.

## Change Log

Tạo entry ban đầu:

#### v1.0 (YYYY-MM-DD)

- Change: Tạo TDD cho STORY-xxx.
- Author: giá trị đã xác nhận hoặc Chưa chỉ định.

## Kiểm tra nhất quán bắt buộc

Trước khi xuất, kiểm tra:

- Mỗi Acceptance Criterion được thiết kế và kiểm thử được.
- Mỗi Main/Alternative/Exception Flow có trong text và diagram phù hợp.
- Mỗi Business Rule có guard/action/constraint tương ứng.
- Non-goals khớp Out of Scope.
- Non-Functional có quyết định kỹ thuật.
- Endpoint, field, enum, nullability và HTTP status thống nhất.
- Mọi input field đã được kiểm tra theo required/null/empty/type/format/boundary/cross-field phù hợp.
- Mọi kết quả có thể đạt tới đã có response đầy đủ và side effect rõ ràng.
- Mỗi dòng trong ma trận bao phủ API có example hoặc example đại diện được dẫn chiếu.
- Examples khớp Fields và Error Codes.
- Sequence, Activity, State và ER không mâu thuẫn.
- Data Model khớp repository hoặc được đánh dấu proposed change.
- Transaction, rollback và external failure nhất quán.
- References chỉ chứa key có thật và đúng project.
- Không có TODO, TBD, placeholder, dấu ba chấm thay nội dung hoặc “same as above”.
- Không tuyên bố hoàn chỉnh nếu còn unresolved reference ảnh hưởng contract.

## Điều kiện hoàn tất

Chỉ gọi TDD là hoàn chỉnh khi:

- STORY-xxx đọc được đầy đủ.
- Tất cả Business Rule liên quan đọc được đầy đủ.
- Không còn xung đột chưa giải quyết ảnh hưởng API, data model hoặc flow.
- Tất cả hàng trong ma trận truy vết đã được phủ.
- Mọi diagram hợp lệ về cú pháp và nhất quán về nội dung.
- Mọi contract có dữ liệu cụ thể, không dùng placeholder.
- Không còn input class hoặc reachable response nào thuộc scope nhưng chưa được mô tả.
- TDD vượt qua checklist nhất quán.

Nếu thiếu thông tin không ảnh hưởng contract, dùng thiết kế an toàn và ghi rõ căn cứ. Nếu thiếu thông tin ảnh hưởng contract, không bịa và không tuyên bố hoàn chỉnh; báo đúng phần cần BA hoặc Architect xác nhận.

## Đầu ra và lưu tài liệu

Khi người dùng chỉ yêu cầu viết:

- Tạo file Markdown UTF-8 theo tên STORY-xxx-TDD-{slug}.md hoặc trả Markdown nếu họ yêu cầu xem trước.
- Nội dung chỉ gồm TDD, không chèn lời giải thích vào file.

Khi người dùng yêu cầu lưu lên Document First:

- Tạo TDD trong cùng project với Story.
- Liên kết References tới STORY-xxx và các Business Rule đã dùng.
- Không ghi sang project khác.
- Trả documentKey và trạng thái lưu sau khi thành công.
