# Triển khai database Group 4

SQL Server 2019+; .NET 8; ứng dụng WPF chạy trên Windows. Script không tự kết nối database đang vận hành. Hãy xác nhận đúng instance/database trước mỗi lệnh. Mọi thay đổi phải được thử trên bản restore của dữ liệu thật trước.

## Database mới

1. Mở `Database/StudentManagement_G4.sql` bằng SSMS. Chọn tên database mới tại `@DatabaseName`, chạy toàn bộ file. Script từ chối ghi đè database có sẵn. File đã bao gồm migration 002.
2. Dữ liệu và thang điểm đi kèm là DEMO. Không coi dữ liệu này là kết quả học tập chính thức. Chốt quy tắc GPA với nhóm trước khi vận hành.
3. Dùng tool AccountBootstrap để đặt mật khẩu tạm cho tài khoản mẫu cần dùng. Không đoán hash hoặc đưa mật khẩu cố định lên GitHub. Tool chỉ reset tài khoản active có sẵn; không tạo tài khoản/role hay tự kích hoạt tài khoản.

## Database hiện hữu

1. Backup FULL có CHECKSUM bằng tài khoản vận hành; thử RESTORE sang database khác. VERIFYONLY không thay thế restore thử. Đảm bảo có đủ dung lượng và quyền ghi của dịch vụ SQL Server vào thư mục backup.
2. Dừng ứng dụng và các luồng ghi. Restore bản sao để chạy thử nâng cấp trước.
3. Trong SSMS, chọn đúng database ứng dụng. Chạy `Database/Migrations/001_RecordDocumentNumber.sql`, sau đó `002_RuntimeHardening.sql`.
4. Migration 002 yêu cầu schema đã có CourseEnrollments/Enrollments và DocumentNumber. Nó không chuyển đổi tùy ý các schema legacy. Nếu database cũ chưa có các bảng đó, cần khảo sát và viết migration chuyển dữ liệu riêng; không chạy fresh installer để thay thế dữ liệu cũ.
5. Script 002 có transaction, khóa nâng cấp và chạy lại được. Nó từ chối thang điểm chồng/thiếu khoảng, lớp đang vượt sức chứa; không tự sửa điểm hoặc tăng sức chứa. Giải quyết dữ liệu có xác nhận nghiệp vụ rồi chạy lại.
6. Trạng thái đăng ký/lớp bị lệch không được tự đồng bộ: đối chiếu lịch sử, điều chỉnh theo quyết định học vụ. View GPA nay chỉ tính cả đăng ký và xếp lớp đã COMPLETED, điểm đã công bố. Vì vậy GPA có thể khác với database cũ nếu dữ liệu trước đó không nhất quán.
7. Nếu nâng cấp thành công nhưng nghiệm thu không đạt: giữ dừng ghi; khôi phục backup và bản ứng dụng tương ứng. Không chạy script xóa bảng để rollback.

## Kết nối và quyền

Copy `PRN212_GROUP4/appsettings.example.json` thành `appsettings.json` rồi điền instance/database. File được copy vào output khi build. Không commit file có thông tin kết nối bí mật.

Trên Windows trong cùng domain, ưu tiên Integrated Security với Windows user/group riêng. Nếu dùng SQL authentication, tạo login riêng cho ứng dụng và bảo vệ file cấu hình trên máy client. Không dùng sa/db_owner cho ứng dụng. Tài khoản migration/backup tách khỏi tài khoản chạy WPF.

Ứng dụng hiện dùng EF truy vấn/ghi trực tiếp nên tài khoản runtime cần SELECT trên các bảng/view nó dùng, INSERT/UPDATE cho chức năng được triển khai và EXECUTE trên procedure tương ứng. Không cấp ALTER/CONTROL/CREATE DATABASE hoặc DELETE lịch sử. Audit trigger ghi cùng transaction; tài khoản runtime không được tự sửa/xóa AuditLogs hoặc chỉnh GradeScale. Kiểm tra quyền thực tế với login runtime trước triển khai, không chỉ bằng sa. F19 backup/restore cần cơ chế vận hành/tài khoản đặc quyền riêng.

Với máy chủ có chứng chỉ hợp lệ: `Encrypt=True;TrustServerCertificate=False`. Local demo với chứng chỉ tự ký có thể tạm dùng TrustServerCertificate=True. Không đưa mật khẩu vào dòng lệnh/log.

WPF kết nối SQL trực tiếp và dùng chung thông tin truy cập không phải ranh giới bảo mật chống client bị sửa. Quyền actor hiện được kiểm tra ở service. Với hệ thống nhiều máy/người dùng ngoài phạm vi tin cậy, chuyển truy cập qua backend API xác thực người dùng hoặc thiết kế danh tính SQL riêng; không coi SESSION_CONTEXT là bằng chứng danh tính không thể giả mạo.

## Hợp đồng tích hợp các module

- F08: `uspRegisterCourse` trả CourseEnrollmentID, không tự xếp lớp.
- F10: `uspAssignStudentToClass` nhận CourseEnrollmentID; Grades liên kết EnrollmentID.
- Hoàn tất/hủy: gọi `uspSetCourseEnrollmentStatus @CourseEnrollmentID, @Status` với COMPLETED/DROPPED. Procedure cập nhật hai bảng cùng transaction; hoàn tất cần xếp lớp và có điểm; hủy có điểm phải chuyển học vụ xử lý. Không dùng procedure để tự sửa kết quả đã hoàn tất.
- View GPA chỉ nhận hai trạng thái COMPLETED và điểm published; giữ chính sách tất cả lượt hoàn tất, trọng số tín chỉ, hai chữ số. Không tự chuyển sang lấy điểm cao nhất/lần cuối.
- Thang điểm active phải phủ đủ 0.00–10.00, không giao nhau. Đổi cả bộ bằng một statement nếu cần; không sửa chính sách trên dữ liệu thật nếu chưa chốt ảnh hưởng lịch sử. Phiên bản chính sách theo khóa/học kỳ chưa được thêm vì nhóm chưa quy định.
- Audit lưu snapshot JSON trước/sau ở AuditLogs.ChangeData. Users chỉ ghi các cột không chứa password/hash/salt. Session actor được service xác thực gắn vào connection; thao tác SQL quản trị có thể có ActorUserID NULL và có databaseLogin. Audit chứa dữ liệu cá nhân, cần hạn chế quyền đọc, backup và chính sách lưu giữ.
- Các bảng có trigger đã tắt EF OUTPUT không INTO. Module mới cũng phải dùng mapping này. Trigger không thay kiểm tra quyền actor tại service.

## Khởi tạo/reset mật khẩu

Mở terminal có tương tác trên máy quản trị, đặt STUDENT_ADMIN_SQL qua cơ chế bảo mật của môi trường, rồi:

```powershell
dotnet run --project tools/AccountBootstrap -- admin01
```

Tool hỏi mật khẩu và xác nhận, không hiện ký tự; dùng PBKDF2-SHA256/210000 vòng giống ứng dụng và buộc đổi lần đầu. Xóa biến môi trường quản trị sau khi dùng. Không chạy trên tài khoản không được phép reset.

## Nghiệm thu

Build solution và tool, chạy SQL-backed tests bằng scripts/Test-BusinessRules.ps1 hoặc STUDENT_TEST_SQL với SQL Server test riêng. Test tự tạo/xóa database tạm, không dùng tài khoản hệ thống đang vận hành.

Trên Windows thử đủ actor, thao tác hồ sơ, đăng ký/xếp lớp/điểm/công bố/GPA/CSV; so số liệu trước/sau nâng cấp; thử backup/restore bằng quy trình vận hành. Các màn hình của thành viên chưa có trong repo vẫn cần tích hợp và test riêng.

Theo dõi dung lượng AuditLogs (snapshot không bị cắt), thời gian query, deadlock và backup. Deadlock có thể xảy ra với các thao tác đồng thời: rollback transaction và cho tải lại/thử lại toàn bộ giao dịch; không báo thành công khi bị rollback.

Khi chỉnh migration 002, chạy `python scripts/Sync-DatabaseInstaller.py` để cập nhật đoạn nhúng trong fresh installer.
