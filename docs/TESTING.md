# Kiểm thử F01, F05, F08, F16, F20

## Cấu trúc và cách chạy

- .NET SDK 8; Windows để chạy giao diện WPF. Có thể build chéo WPF và chạy test nghiệp vụ trên Linux.
- `PRN212_GROUP4.Core` biên dịch trực tiếp Models/service đang được WPF sử dụng. Test không sao chép thuật toán nghiệp vụ.
- `PRN212_GROUP4.Tests` dùng xUnit và SQL Server thật. Mỗi ca tạo database riêng `NhatTests_<GUID>` từ script cài đặt, rồi xóa database đó trong cleanup.
- Dùng SQL Server riêng cho kiểm thử, có quyền tạo database. Không dùng connection của hệ thống đang vận hành.
- Phiên AppSession hiện là phiên đơn của ứng dụng desktop: tắt chạy song song giữa các test; ca concurrency tự tạo các yêu cầu đồng thời trong cùng một test.

Chạy từ thư mục gốc repository:

```powershell
# Tự dựng SQL Server 2022 bằng Docker, build/test và dọn container:
./scripts/Test-BusinessRules.ps1

# Hoặc dùng SQL Server kiểm thử có sẵn:
$env:STUDENT_TEST_SQL = 'Server=localhost,14333;Database=master;User ID=sa;Password=<test-password>;Encrypt=True;TrustServerCertificate=True'
dotnet build
dotnet test --no-build --logger trx
```

Thiếu `STUDENT_TEST_SQL` sẽ báo lỗi rõ ràng, không âm thầm bỏ qua test SQL. Hash/mật khẩu mẫu của database gốc không được đoán: bộ test tự đặt mật khẩu chỉ trong database tạm.

## Kịch bản tự động và kết quả mong đợi

| Mã | Tình huống | Kết quả mong đợi |
|---|---|---|
| F01-01 | Username trống; Password trống; cả hai trống; đủ dữ liệu | Lỗi đúng trường, không lẫn với thông báo sai thông tin chung |
| F01-02 | Student, Lecturer, Academic Staff, Admin đăng nhập | Đúng tài khoản/role và tạo phiên |
| F01-03 | Tài khoản inactive hoặc không tồn tại | Cùng thông báo chung; không có phiên |
| F01-04 | Cả 4 actor có MustChangePassword | Không có AppSession trước khi đổi; service bảo vệ từ chối; đổi xong mới có phiên |
| F01-05 | Hủy yêu cầu đổi mật khẩu lần đầu | Không tái sử dụng yêu cầu cũ |
| F05-01 | Trùng StudentID + RecordType + DocumentNumber | Hai bản ghi vẫn được lưu; kết quả lần trùng có Warning |
| F05-02 | Khác từng thành phần của tổ hợp | Không cảnh báo nhầm |
| F05-03 | Mã chứng từ có khoảng trắng/chữ thường | Chuẩn hóa trim/uppercase trước khi kiểm tra |
| F05-04 | Sửa chính hồ sơ; sửa sang tổ hợp trùng hồ sơ khác | Không tự cảnh báo chính mình; có Warning khi trùng hồ sơ khác |
| F05-05 | Hai người thêm cùng tổ hợp đồng thời | Lưu được cả hai; một lần lưu nhận Warning |
| F05-06 | Archive rồi thêm cùng tổ hợp | Giữ lịch sử, ẩn khỏi danh sách active, vẫn cảnh báo trùng với lịch sử |
| F05-07 | Hai người sửa cùng hồ sơ | Lần sửa bằng dữ liệu cũ bị từ chối, giữ kết quả người lưu trước |
| F05-08 | Student gọi service quản lý hồ sơ | Bị từ chối quyền |
| F05-09 | Chạy migration hai lần trên schema cũ | Không lỗi; không xóa hồ sơ cũ; DocumentNumber lịch sử vẫn NULL |
| F08-01 | Đăng ký mới | Có CourseEnrollmentID, đúng một dòng, không có Enrollments tự tạo |
| F08-02 | REGISTERED hoặc COMPLETED | Từ chối |
| F08-03 | DROPPED chưa có điểm | Tái sử dụng ID cũ; chuyển REGISTERED; không thêm dòng |
| F08-04 | DROPPED đã có điểm | Thông báo học vụ xử lý; trạng thái/điểm không đổi |
| F08-05 | Sinh viên inactive, môn inactive, học kỳ không tồn tại | SQL từ chối kể cả gọi service bỏ qua UI |
| F08-06 | Hai yêu cầu đăng ký cùng lúc | Một thành công, một bị từ chối, chỉ một dòng |
| F08-07 | Student đăng ký cho sinh viên khác | Bị từ chối phạm vi |
| F09-01 | Student/Lecturer/Staff xem đăng ký | Chỉ dữ liệu của mình/lớp được giao/khoa được cấp |
| F16-01 | Dữ liệu mẫu và thêm một lượt học lại | Summary, báo cáo và CSV có cùng GPA từ IAcademicGpaProvider |
| F16-02 | Thiếu điểm hoặc điểm chưa công bố | Điểm trống, không tính GPA từ điểm chưa công bố |
| F16-03 | Chưa có kết quả | GPA NULL và CSV trống, không biến thành 0 |
| F16-04 | Lecturer xem báo cáo | Không lộ lớp khác hoặc GPA tổng hợp ngoài phạm vi |
| F16-05 | Student xem summary người khác hoặc bị thu hồi F14 | Bị từ chối |
| F20-01 | Sai mật khẩu hiện tại hoặc xác nhận mới sai | Từ chối, hash lưu trong SQL không đổi |
| F20-02 | Mô phỏng SaveChanges thất bại | Hash cũ còn nguyên, mật khẩu cũ đăng nhập được, mật khẩu mới bị từ chối |
| F20-03 | Đổi thành công | Mật khẩu cũ bị từ chối, mật khẩu mới đăng nhập được |
| F20-04 | Lưu đổi lần đầu thất bại | MustChangePassword vẫn true, mật khẩu cũ còn nguyên, không tạo phiên |
| F02-01 | Logout khi truy vấn quyền đang chạy | Truy vấn cũ bị từ chối khi hoàn tất, phiên được xóa |
| F02-02 | Logout rồi đăng nhập người khác | Không dùng lại danh tính/phạm vi người trước |
| F03-01 | Student sửa hồ sơ | Chỉ hồ sơ mình và email/điện thoại; sửa tên bị từ chối |
| F03-02 | Hai bản sao cùng sửa profile | RowVer cũ bị từ chối, giữ dữ liệu người lưu trước |

## Quy tắc GPA dùng chung

Giữ chính sách SQL đã có: tất cả lượt học có Enrollments.COMPLETED và điểm đã công bố; trọng số tín chỉ; làm tròn 2 chữ số. Một lượt học lại cũng là một lượt hoàn tất được tính. Không tự đổi sang lấy lần cuối hoặc điểm cao nhất khi nhóm chưa duyệt.

`SqlViewGpaProvider` là adapter nguồn chung. `AcademicReportService` và `AcademicSummaryService` đều nhận `IAcademicGpaProvider`; nguồn của Lực có thể được thay qua interface. Summary kiểm tra F14 và phạm vi của Student/Staff. Lecturer chỉ nhận kết quả lớp phụ trách trong F16, không nhận GPA toàn bộ của sinh viên.

Các truy vấn báo cáo và summary dùng giao dịch Serializable để dữ liệu điểm/GPA nhất quán trong một lần đọc. CSV dùng cùng formatter được test; thao tác xuất kiểm tra lại quyền/lấy dữ liệu mới rồi cập nhật Rows trước khi ghi file.

Repository hiện chưa có màn hình F14/F15 hoàn chỉnh của Lực. Service summary là hợp đồng dùng chung để các màn hình đó tích hợp, không phải bằng chứng rằng các màn hình chưa tồn tại đã được kiểm thử UI.

## Nâng cấp database đang có

1. Chạy `Database/Migrations/001_RecordDocumentNumber.sql` trên đúng database ứng dụng trước khi dùng bản mới.
2. Migration chỉ thêm cột nullable và index không unique; không xóa/đổi lịch sử.
3. Hồ sơ mới hoặc chỉnh sửa phải nhập mã chứng từ thực tế. Hồ sơ cũ vẫn đọc/archive được khi mã còn NULL.
4. Database mới chạy `Database/StudentManagement_G4.sql`; không chạy lại script fresh-install để nâng cấp database hiện hữu.
5. Cảnh báo trùng bao gồm hồ sơ archived. Warning hiện sau khi lưu thành công, không phải lỗi và không làm mất bản ghi.

## Kiểm tra giao diện trên Windows trước khi merge

Các test tự động chạy service/SQL và build XAML; không thay thế việc mở cửa sổ WPF:

1. Login: bỏ trống từng ô, xác nhận lỗi hiện đúng ô; thử đủ 4 actor; tài khoản đổi lần đầu không nhìn thấy MainWindow trước khi đổi.
2. Add/Edit Record: nhập mã chứng từ; lưu hai hồ sơ cùng tổ hợp; hộp thoại có biểu tượng Warning, lưu thành công và có cả hai dòng; kiểm tra Cancel/Archive.
3. Enrollment: hủy một đăng ký không có điểm trong bộ dữ liệu thử, đăng ký lại từ UI; ID giữ nguyên. Thử bản ghi đã có điểm và xác nhận thông báo học vụ.
4. Reports: lọc theo sinh viên/học kỳ/lớp; đối chiếu các cột điểm/GPA với CSV và nguồn chung; thử kết quả rỗng và hủy SaveFileDialog.
5. Password: sai hiện tại, sai xác nhận, đúng; logout và login lại bằng mật khẩu mới.
6. Logout trong lúc đang tải hồ sơ: không có cửa sổ lỗi của phiên cũ bật lại; tài khoản kế tiếp chỉ thấy dữ liệu trong quyền của mình.

Ghi kết quả UI thực tế và người kiểm tra vào PR; không đánh dấu các bước này đã chạy chỉ vì dotnet build/test pass.
