# Bộ database toàn dự án

## Chọn đúng file để cài

- **Demo đầy đủ, cài mới:** `Database/Install_FullProject_Demo.sql`. Mặc định tạo `PRN212_StudentManagement_G4_Demo`; chạy toàn bộ file trong SSMS. Có dữ liệu cơ bản cũ và bộ DEMO-* độc lập. Không ghi đè database có sẵn.
- **Vận hành với dữ liệu thật, cài mới:** `Database/Install_Production.sql`. Mặc định tạo `PRN212_StudentManagement_G4_Production`. Không có tài khoản/sinh viên/môn/lớp/điểm mẫu; có 4 role, 20 permission, matrix và mẫu thang điểm cần duyệt. Xem lại INSERT GradeScale và đặt `@GradePolicyConfirmed=1` sau khi chốt chính sách. Mặc định script dừng trước CREATE DATABASE.
- **Database đã có dữ liệu:** backup/restore thử, migration 001 rồi 002; KHÔNG dùng installer cài mới. Không nhập demo vào database vận hành. Các bổ sung ở lần này là dữ liệu/đóng gói, không đòi migration mới ngoài 001/002.
- `Database/StudentManagement_G4.sql` giữ bộ mẫu tối thiểu để tương thích bộ regression cũ; nếu muốn demo cả nhóm, chọn file FullProject ở trên.

## Kích hoạt đăng nhập

Seed không chứa mật khẩu dùng chung. Các tài khoản mới có dữ liệu credential ngẫu nhiên không có mật khẩu được cung cấp; phải đặt mật khẩu tạm bằng công cụ trước khi dùng. Công cụ dùng đúng PBKDF2-SHA256/210000 vòng, không hiện mật khẩu, buộc đổi lần đầu.

Trên máy quản trị, đặt STUDENT_ADMIN_SQL bằng cơ chế cấu hình bí mật của môi trường rồi chạy:

```powershell
# Demo: reset từng tài khoản active định dùng
dotnet run --project tools/AccountBootstrap -- demo_admin
dotnet run --project tools/AccountBootstrap -- demo_staff_se
dotnet run --project tools/AccountBootstrap -- demo_staff_ba
dotnet run --project tools/AccountBootstrap -- demo_lecturer_se
dotnet run --project tools/AccountBootstrap -- demo_student_a

# Production trống: tạo admin đầu tiên, không tự tạo thêm nếu đã có admin
dotnet run --project tools/AccountBootstrap -- your_admin --create-first-admin
```

Xóa biến STUDENT_ADMIN_SQL sau khi dùng. Công cụ này là công cụ quản trị, không phân phối connection quản trị cho client WPF. Tài khoản inactive không được công cụ reset như tài khoản active. Tạo tài khoản thật và liên kết hồ sơ bằng module quản trị sau khi module đã được tích hợp; nếu module chưa có, DBA cần quy trình nhập đã duyệt, không dùng seed demo thay thế.

## Bộ demo bổ sung

Tất cả mã DEMO-* / username demo_* là giả. Không trộn vào dữ liệu thật.

| Đối tượng | Dữ liệu bổ sung |
|---|---|
| Khoa | DEMO-SE, DEMO-BA |
| Tài khoản | 13: admin, 2 staff, 2 lecturer, 8 student (gồm một inactive) |
| Sinh viên | 8, tất cả liên kết tài khoản |
| Môn học | 6: A(3 tín chỉ), B(2), C(1), BA(4), OFF inactive và NEW chưa có lớp |
| Học kỳ | DEMO-T1, T2 và T3 không có đăng ký/điểm |
| Lớp | 9; có FULL sức chứa 1, CLOSED, CANCELLED và EMPTY |
| Đăng ký/xếp lớp/điểm | 13 đăng ký, 11 xếp lớp, 8 điểm; có điểm chưa công bố, 0 và 10 |
| Hồ sơ | 3 hồ sơ của DEMO-A: hai cùng mã chứng từ để thử cảnh báo và một archived |

Bộ mẫu tối thiểu cũ có 6 sinh viên, một số chưa có tài khoản; các hồ sơ cũ có DocumentNumber NULL được giữ nguyên để thử xử lý dữ liệu lịch sử. Đó là dữ liệu cũ có chủ ý, không tự điền mã chứng từ thật hay thay đổi tài khoản cũ khi seed.

## Kết quả mong đợi để đối chiếu

| Tình huống | Kết quả |
|---|---|
| demo_staff_se / demo_staff_ba | Chỉ hồ sơ khoa DEMO tương ứng, không thấy khoa bên kia hoặc khoa cơ bản cũ |
| demo_lecturer_se / ba | Chỉ lớp được phân công, không thấy lớp của người kia |
| demo_student_a | Chỉ DEMO-A; hoàn tất A-T1=8.60, B-T1=4.00, học lại A-T2=6.80 |
| GPA DEMO-A | T1=2.80, T2=2.50, tích lũy=2.69 với 8 tín chỉ được tính; chính sách hiện tại tính tất cả lượt hoàn tất |
| DEMO-B | A-T1=7.99 chưa công bố; GPA không có dữ liệu; có B-T1 đăng ký chưa xếp lớp |
| DEMO-C | A-T1 đang học chưa điểm; B-T1 DROPPED chưa điểm có thể đăng ký lại giữ ID; C-T1 DROPPED có điểm, đăng ký lại phải báo học vụ |
| DEMO-BA | BA-T1=8.49, GPA=3.50; Staff SE không được truy cập |
| DEMO-ZERO / DEMO-TEN | Điểm A-T1=0/10 đã công bố; GPA=0.00/4.00 (0 là kết quả thật, khác NULL) |
| DEMO-FULL | DEMO-TEN chiếm một chỗ duy nhất; DEMO-A có đăng ký C-T2 chờ xếp, xếp vào FULL phải bị từ chối |
| DEMO-CLOSED / CANCELLED | Không nhận xếp lớp mới; bộ lọc báo cáo lớp không có dữ liệu trả trạng thái rỗng |
| demo_reset | Student chưa có đăng ký, dùng thử reset/vô hiệu hóa và không có kết quả |
| demo_student_inactive | Không đăng nhập được, sinh viên cũng INACTIVE |
| DEMO-A hồ sơ | Có hai hồ sơ cùng tổ hợp để cảnh báo; archived giữ lịch sử |

DEMO-C có đăng ký đã hủy nhưng còn điểm là tình huống lịch sử cố ý để kiểm thử chặn đăng ký lại; không đưa nó vào GPA. Seed trực tiếp chuẩn bị fixture trong một transaction, không phải hướng dẫn bỏ qua procedure khi vận hành.

## Seed lại và phát hiện lỗi

`Seeds/FullProject_Demo.sql` chỉ chạy trên database đã có extended property G4.DemoSeedAllowed=1 do installer demo tạo. Có transaction và khóa seed. Chạy lại có marker v1 thì không thêm trùng, không reset mật khẩu, không ghi đè dữ liệu đã thao tác. Nếu mã DEMO đã tồn tại nhưng marker chưa có thì script dừng để tránh trộn dữ liệu.

Muốn trở về trạng thái demo ban đầu: cài database demo mới bằng tên khác hoặc restore backup demo. Không tự xóa từng bảng đang có khóa ngoại/lịch sử. Không thêm marker demo vào production chỉ để chạy seed.

Chạy `Database/Operations/Check_Readiness.sql` trong đúng database. Script chỉ đọc; ERROR sẽ THROW, WARN yêu cầu xem xét. Ví dụ WARN cho tài khoản cần đổi mật khẩu hoặc hồ sơ lịch sử thiếu chứng từ là bình thường ở demo. Script không kiểm chứng mật khẩu chưa biết, chất lượng dữ liệu nghiệp vụ, UI hay toàn bộ quyền môi trường.

`Operations/Provision_RuntimeRole.sql` tạo role quyền tối thiểu cho code hiện tại: DBA tự tạo/map user và thêm vào role. Không có quyền xóa lịch sử, sửa audit/thang điểm hoặc DML trực tiếp đăng ký/xếp lớp; module mới dùng procedure và được kiểm tra quyền actor trong service. Role này không thay phân quyền từng người dùng; WPF hiện kết nối SQL trực tiếp. F19 backup/restore dùng cơ chế/tài khoản quản trị riêng.

## Những phần vẫn cần hoàn tất khi đưa lên máy thật

- Chốt chính sách GPA/học lại/lịch đăng ký và phạm vi học vụ; không lấy ngày T1/T2 demo làm lịch thật.
- ScheduleNote chỉ là ghi chú; muốn chặn trùng lịch phải triển khai mô hình lịch và quy tắc đã thống nhất.
- Kiểm thử các màn hình của Thương/Lực khi được tích hợp; dữ liệu/SQL không tự tạo các màn hình còn thiếu.
- Dùng connection runtime riêng, TLS phù hợp, chạy kiểm tra quyền; thử backup/restore, đối chiếu báo cáo/CSV trên Windows trước khi nhập dữ liệu thật.

## Đồng bộ file SQL

Nguồn schema: StudentManagement_G4.sql + migration 002. Nguồn seed: Seeds/FullProject_Demo.sql.
Chạy `python scripts/Sync-DatabaseInstaller.py` rồi `python scripts/Build-DatabasePackages.py` khi sửa nguồn. Không sửa trực tiếp các file Install_* được sinh.
