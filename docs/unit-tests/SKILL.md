---
name: complete-unit-test-writer
description: Viết tài liệu Unit Test Markdown hoàn chỉnh theo đúng cấu trúc UT-xxx-yy, bám sát STORY, TDD, Business Rule và code hiện có. Dùng khi cần soạn từng test case ở mức hàm, service hoặc validator; sau mỗi file phải phân tích các trường hợp còn thiếu và chờ người dùng duyệt trước khi viết file tiếp theo.
---

# Complete Unit Test Writer

## Mục đích

Tạo đúng một tài liệu Unit Test hoàn chỉnh cho một hành vi có thể quan sát được của unit đang kiểm thử. Tài liệu phải đủ rõ để lập trình viên có thể triển khai test tự động mà không cần đoán thêm hoặc sửa lại nội dung.

Luôn lấy STORY và TDD làm nguồn yêu cầu chính. Chỉ dùng Business Rule, code, Entity, enum, database mapping và các test hiện có để xác minh và cụ thể hóa nội dung đã được tài liệu quy định; không được dùng chúng để tự ý thay đổi yêu cầu.

Không dùng skill này để viết System Test, End-to-End Test, triển khai code test hoặc sửa STORY/TDD.

## Nguyên tắc bắt buộc

1. Mỗi file chỉ mô tả một test case và một kết quả chính.
2. Mỗi lần chỉ tạo một file Unit Test, trừ khi người dùng yêu cầu rõ số lượng lớn hơn.
3. Phải đọc đầy đủ STORY và TDD được tham chiếu trước khi viết.
4. Phải đọc các Business Rule mà STORY hoặc TDD tham chiếu nếu test case liên quan đến rule đó.
5. Không tự thêm field, trạng thái, enum, validation, lỗi, side effect, API, dependency hoặc hành vi không tồn tại trong STORY/TDD.
6. Không dùng placeholder như TBD, TODO, dấu ba chấm hoặc câu chung chung cần người khác hoàn thiện sau.
7. Không ghi Content Hash hoặc contentHash vào tài liệu.
8. Không tự lưu tài liệu lên Document First. Yêu cầu viết Unit Test thông thường chỉ tạo file Markdown cục bộ trong thư mục docs/unit-tests. Chỉ lưu lên Document First khi người dùng yêu cầu rõ.
9. Sau khi hoàn thành một file, phải báo các trường hợp còn thiếu của cùng STORY/TDD và dừng lại để người dùng duyệt. Không được tự viết file tiếp theo.

## Thứ tự nguồn sự thật

Sử dụng nguồn theo thứ tự sau:

1. Nội dung và Acceptance Criteria của STORY.
2. Contract, flow, validation, data model và error handling trong TDD.
3. Business Rule được STORY hoặc TDD tham chiếu.
4. Entity, enum, DbContext, interface, service, validator và code hiện có.
5. Quy ước của các Unit Test đã có trong cùng project.

Code được dùng để xác định tên class, method, kiểu dữ liệu và dependency thực tế. Code không được âm thầm ghi đè yêu cầu trong STORY/TDD.

## Quy tắc dừng khi có mâu thuẫn

Nếu phát hiện bất kỳ điểm khác nhau nào giữa STORY, TDD, Business Rule và code/database có thể làm thay đổi input, output, validation, entity, field, enum, relationship, trạng thái, lỗi hoặc side effect của test:

1. Dừng viết Unit Test.
2. Nêu chính xác từng nguồn đang quy định khác nhau như thế nào.
3. Hỏi người dùng chọn cách xử lý.
4. Không tự thêm, xóa, đổi tên hoặc sửa contract để làm cho các nguồn khớp nhau.

Cũng phải dừng nếu thiếu thông tin thiết yếu khiến Expected output không thể xác định chính xác, ví dụ thiếu loại exception, mã lỗi, quy tắc làm tròn, thứ tự ưu tiên hoặc trạng thái đầu ra.

## Quy trình thực hiện

### Bước 1: Xác định phạm vi

Xác định chính xác:

- Project đang làm.
- STORY cần kiểm thử.
- TDD hiện thực hóa STORY đó.
- Unit under test: class và method cụ thể.
- Test case hiện tại thuộc nhánh nào: Happy, Branch, Boundary, Error, Quirk hoặc Determinism.
- Owner của tài liệu Unit Test.

Nếu người dùng chỉ cung cấp STORY mà chưa chỉ rõ TDD, phải tìm TDD tham chiếu STORY đó. Nếu có nhiều TDD phù hợp và không thể xác định duy nhất, phải hỏi người dùng.

Nếu chưa xác định được owner từ yêu cầu hoặc quy ước hiện có của project, phải hỏi người dùng trước khi tạo file để tài liệu không cần sửa lại sau.

### Bước 2: Thu thập ngữ cảnh

Đọc nội dung đầy đủ của STORY và TDD, không chỉ dùng tiêu đề, kết quả tìm kiếm hoặc bản tóm tắt. Tối thiểu phải kiểm tra:

- Main Flow, Alternative Flow và Exception Flow.
- Acceptance Criteria.
- Business Rule references.
- API hoặc Internal API contract.
- Validation và error handling.
- Data Model, enum, nullability và relationship.
- State transition và side effect.
- Non-Functional Requirement có thể kiểm thử ở mức unit.

Sau đó kiểm tra code liên quan để dùng đúng tên class, method, parameter, return type, dependency và model. Đọc các Unit Test hiện có để tránh trùng test case và giữ đúng cách đặt tên.

### Bước 3: Lập ma trận coverage nội bộ

Trước khi viết, lập danh sách tất cả trường hợp có thể cần Unit Test từ STORY và TDD:

- Mỗi Acceptance Criterion.
- Mỗi điều kiện rẽ nhánh trong flow hoặc pseudocode.
- Mỗi validation và Business Rule.
- Giá trị biên và lớp tương đương.
- Null, empty, zero, số âm, duplicate hoặc enum không hợp lệ nếu contract cho phép nhận các giá trị này.
- Not found, inactive, deleted, conflict hoặc forbidden nếu được quy định.
- Dependency trả lỗi, timeout hoặc dữ liệu bất thường nếu TDD quy định cách xử lý.
- Mỗi state transition và terminal state.
- Mỗi side effect: ghi database, gọi repository, gửi message, gửi email, cập nhật timestamp hoặc không được thực hiện hành động nào.

Đánh dấu test case nào đã có file, test case nào đang viết và test case nào còn thiếu. Không tạo hai file kiểm thử cùng một hành vi với cùng input và expected output.

### Bước 4: Chọn đúng một test case

Ưu tiên theo thứ tự:

1. P0: bảo mật, mất dữ liệu, sai quyền, vi phạm dữ liệu nghiêm trọng.
2. P1: main flow hoặc Business Rule cốt lõi.
3. P2: nhánh thay thế, validation và giá trị biên quan trọng.
4. P3: trường hợp hiếm hoặc hành vi phụ ít ảnh hưởng.

Nếu người dùng đã chỉ định test case thì phải viết đúng trường hợp đó, không tự đổi sang test khác.

### Bước 5: Đặt tên file và Test ID

Dùng định dạng:

- File: UT-<mã TDD>-<số thứ tự hai chữ số>.md
- Test ID trong bảng phải trùng tuyệt đối với tên file bỏ phần mở rộng.
- Ví dụ: TDD-005 dùng UT-005-01.md, UT-005-02.md, UT-005-03.md.

Số thứ tự phải là số tiếp theo chưa được sử dụng trong thư mục. Không ghi đè file đã có trừ khi người dùng yêu cầu sửa file đó.

### Bước 6: Viết tài liệu

Tài liệu phải dùng đúng cấu trúc ở mục Mẫu đầu ra. Trong một ô có nhiều dòng, dùng thẻ HTML <br> để bảng Markdown không bị vỡ.

### Bước 7: Tự kiểm tra

Đọc lại toàn bộ file và đối chiếu checklist hoàn thành trước khi bàn giao.

### Bước 8: Báo coverage còn thiếu và chờ duyệt

Sau khi tạo xong một file:

1. Nói rõ đường dẫn file vừa tạo.
2. Nêu test case vừa phủ, loại test, priority và STORY/TDD/BR được truy vết.
3. Liệt kê các test case còn thiếu của cùng STORY và TDD, nhóm theo Happy, Branch, Boundary, Error, Quirk hoặc Determinism.
4. Với mỗi test còn thiếu, nêu Test ID dự kiến, hành vi cần kiểm tra và priority.
5. Hỏi người dùng có duyệt để viết test tiếp theo hay không.
6. Dừng công việc. Không tạo file tiếp theo trước khi nhận được xác nhận.

Nếu không còn trường hợp cần test, phải nói rõ coverage đã đầy đủ theo STORY/TDD hiện tại và không đề xuất tạo thêm file vô nghĩa.

## Hướng dẫn chi tiết từng cột

### Test ID

Mã duy nhất theo định dạng UT-xxx-yy. Phải trùng với tiêu đề cấp một và tên file.

### Module

Tên khu vực nghiệp vụ và mục tiêu đang kiểm thử. Viết đủ cụ thể để phân biệt với module khác, ví dụ PRICING - Tính số lượng khả dụng Combo. Dùng thuật ngữ có trong STORY/TDD, không tự đặt tên nghiệp vụ mới.

### Unit under test

Tên class và method chính xác trong code hoặc TDD, theo dạng ClassName.MethodName. Chỉ ghi một unit chính. Dependency được mock phải mô tả trong Precondition / Mock setup, không đưa vào cột này.

### Loại

Chọn đúng một giá trị:

- Happy: luồng hợp lệ chính.
- Branch: một nhánh điều kiện hoặc luồng thay thế hợp lệ.
- Boundary: giá trị tại hoặc sát giới hạn.
- Error: input không hợp lệ, exception hoặc dependency failure.
- Quirk: hành vi đặc thù có chủ đích, dễ bị hiểu nhầm hoặc dễ regression.
- Determinism: kiểm tra tính ổn định của thời gian, thứ tự, random, làm tròn hoặc kết quả lặp lại.

### Suite

Chọn đúng một giá trị:

- SMOKE: hành vi cốt lõi phải chạy nhanh và luôn được kiểm tra.
- REGRESSION: nhánh quan trọng cần chạy thường xuyên để ngăn lỗi quay lại.
- FULL: trường hợp sâu, hiếm hoặc tốn nhiều setup hơn nhưng vẫn thuộc unit test.

### Priority

Chọn P0, P1, P2 hoặc P3 theo mức ảnh hưởng đã quy định ở Bước 4. Không tăng priority chỉ vì test dễ viết.

### Precondition / Mock setup

Mô tả toàn bộ trạng thái ban đầu và hành vi dependency cần để tái tạo test:

- Dữ liệu mẫu với ID nhất quán.
- Field ảnh hưởng đến nhánh logic và giá trị cụ thể.
- Trạng thái entity, enum, nullability và relationship liên quan.
- Giá trị repository hoặc service mock trả về.
- Thời gian cố định nếu logic phụ thuộc thời gian.
- Dependency nào phải được gọi, không được gọi hoặc ném exception.

Không liệt kê field không liên quan. Không dùng mô tả mơ hồ như dữ liệu hợp lệ hoặc mock bình thường nếu tính hợp lệ chưa được cụ thể hóa.

### Input

Ghi lời gọi method chính xác cùng tham số cụ thể. Với object phức tạp, ghi các field ảnh hưởng đến hành vi. ID và giá trị phải khớp Precondition / Mock setup.

### Expected output

Ghi kết quả quan sát được chính xác, bao gồm phần liên quan trong các mục sau:

- Giá trị trả về và từng field quan trọng.
- Exception type, error code và message nếu contract quy định.
- Dữ liệu được thêm, sửa hoặc không thay đổi.
- State transition.
- Số lần gọi dependency và tham số gọi.
- Dependency không được gọi.
- Side effect được tạo hoặc không được tạo.

Không ghi chung chung như thành công, trả đúng dữ liệu hoặc báo lỗi. Không tự đặt error code hoặc message chưa được STORY/TDD quy định.

### Trace to (requirement / BR)

Liệt kê document key tồn tại thật và trực tiếp quy định hành vi đang test. Tối thiểu phải có STORY và TDD. Thêm BR khi test đang xác minh rule đó. Mỗi key đặt trên một dòng bằng <br>.

Không tham chiếu tài liệu chỉ vì cùng module nhưng không quy định hành vi của test case.

### Rationale

Giải thích ngắn gọn vì sao test này cần thiết và công thức, điều kiện hoặc rủi ro nào đang được bảo vệ. Nêu logic cần chứng minh, không lặp nguyên văn Expected output.

### Owner

Dùng owner do người dùng hoặc project xác định. Nếu chưa có owner, phải hỏi trước khi viết; không ghi placeholder và không tự gán tên người.

### Trạng thái

File mới dùng Draft. Chỉ dùng trạng thái khác nếu người dùng yêu cầu hoặc workflow của project quy định rõ.

## TEST_LINKS

Liệt kê đúng các document key đã xuất hiện trong cột Trace to, mỗi key một bullet. Tối thiểu gồm STORY và TDD; thêm BR nếu có.

Không thêm link thừa và không bỏ sót key đã dùng trong Trace to.

## Mẫu đầu ra bắt buộc

    # UT-xxx-yy

    ## Unit Test

    | Test ID | Module | Unit under test | Loại | Suite | Priority | Precondition / Mock setup | Input | Expected output | Trace to (requirement / BR) | Rationale | Owner | Trạng thái |
    | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
    | UT-xxx-yy | <Module> | <Class.Method> | <Loại> | <Suite> | <Priority> | <Dữ liệu và mock cụ thể> | <Lời gọi method cụ thể> | <Kết quả, state và side effect chính xác> | STORY-xxx<br>TDD-xxx<br>BR-xxx | <Lý do kiểm thử> | <Owner> | Draft |

    ## TEST_LINKS

    - STORY-xxx
    - TDD-xxx
    - BR-xxx

Chỉ giữ dòng BR-xxx khi test thực sự tham chiếu Business Rule.

## Quy tắc thiết kế test case

### Một hành vi trên một file

Không gộp nhiều scenario bằng từ hoặc trong cùng một hàng. Nếu input A và input B đi qua hai nhánh khác nhau, tạo hai test case riêng sau khi người dùng duyệt.

### Giá trị biên

Chỉ tạo boundary test cho giới hạn tồn tại trong STORY/TDD hoặc kiểu dữ liệu thực tế. Với một giới hạn N, cân nhắc N-1, N và N+1; mỗi giá trị chỉ cần file riêng khi kết quả hoặc nhánh xử lý khác nhau.

### Exception và lỗi

Expected output phải phân biệt rõ lỗi validation do input, lỗi nghiệp vụ và lỗi dependency. Chỉ khẳng định exception type, HTTP mapping, error code hoặc message nếu nguồn tài liệu đã quy định.

### Mock và interaction

Chỉ mock dependency bên ngoài unit đang test. Không mock chính logic cần kiểm tra. Nếu kết quả phụ thuộc lời gọi repository/service, ghi rõ dữ liệu mock trả về và verify số lần gọi khi điều đó có ý nghĩa với contract.

### Thời gian và tính xác định

Nếu logic dùng thời gian hiện tại, dùng một thời điểm cố định từ time provider hoặc clock abstraction đã tồn tại. Không dùng các cụm như hôm nay hoặc thời gian hiện tại trong test data vì kết quả sẽ thay đổi.

### Dữ liệu nhất quán

Một ID, enum, số lượng, trạng thái hoặc timestamp phải giữ nguyên ý nghĩa từ Precondition đến Input và Expected output. Công thức trong Rationale phải cho ra đúng giá trị trong Expected output.

## Checklist hoàn thành

Chỉ bàn giao file khi tất cả câu dưới đây đều đúng:

- Đã đọc đầy đủ STORY và TDD liên quan.
- Đã đọc BR liên quan nếu có.
- Không có mâu thuẫn chưa được người dùng quyết định.
- Test ID trùng tên file và tiêu đề.
- Unit under test là class và method cụ thể.
- Test chỉ phủ một hành vi.
- Setup đủ để tái tạo test và không chứa dữ liệu thừa gây nhiễu.
- Input cụ thể và khớp setup.
- Expected output có thể assertion được, không mơ hồ.
- Error, state transition, interaction và side effect liên quan đã được nêu.
- Trace to chỉ chứa reference tồn tại thật.
- TEST_LINKS khớp hoàn toàn với Trace to.
- Không có placeholder hoặc Content Hash.
- Owner đã được xác định, không phải giá trị tạm.
- Nội dung không tự thêm yêu cầu ngoài STORY/TDD/BR.
- Đã đối chiếu các file Unit Test hiện có để tránh trùng.
- Đã chuẩn bị danh sách coverage còn thiếu để báo người dùng.
- Chưa tạo test kế tiếp khi chưa có phê duyệt.

## Mẫu bàn giao bắt buộc sau mỗi file

Khi hoàn thành, phản hồi theo nội dung sau:

    Đã tạo: <đường dẫn file>
    Đã phủ: <tên scenario>, <Loại>, <Priority>
    Truy vết: <STORY>, <TDD>, <BR nếu có>

    Các test còn thiếu của STORY/TDD này:
    - <Test ID dự kiến> — <Loại/Priority> — <hành vi cần kiểm tra>
    - <Test ID dự kiến> — <Loại/Priority> — <hành vi cần kiểm tra>

    Bạn duyệt cho tôi viết <Test ID kế tiếp> không?

Nếu danh sách trống, thay phần đó bằng: Không còn trường hợp Unit Test nào bị thiếu theo STORY/TDD hiện tại.
