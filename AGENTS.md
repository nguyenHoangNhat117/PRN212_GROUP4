# Cấu hình AI Agent - Dự án WPF Quản lý sinh viên

## 1. Môi trường công nghệ (Environment)
- Ngôn ngữ & Framework: C#, .NET, WPF (Windows Presentation Foundation)
- Hệ quản trị CSDL: SQL Server
- Công cụ test tự động: `dotnet test` (Unit Test project trong Solution)

## 2. Các lệnh AI được phép tự động chạy (Allowed Commands)
- Chạy toàn bộ Unit Test: `dotnet test`
- Chạy test cho một project cụ thể: `dotnet test ./[TenProjectTest]/[TenProjectTest].csproj`
- Kiểm tra lỗi biên dịch code C#: `dotnet build`

## 3. Danh sách nghiệp vụ và Quy tắc kiểm thử (Business Rules & Backlog)

### F01 – Đăng nhập (Login Validation)
- **Yêu cầu UI:** Nếu thiếu Username hoặc Password, phải hiển thị thông báo lỗi riêng tại ô nhập liệu đó (Validation Error). Nếu sai thông tin, chỉ hiện thông báo chung.
- **Kịch bản Test:** Thử nghiệm đăng nhập với 4 Actor (Student, Lecturer, Academic Staff, Admin). Tài khoản Inactive phải bị chặn. Tài khoản yêu cầu đổi mật khẩu lần đầu không được vào màn hình chính.

### F08 – Đăng ký môn học (Ưu tiên cao - Đồng bộ C# & SQL)
- **Quy tắc:** Bỏ hàm chặn tuyệt đối `AnyAsync()` hiện tại trong C#. Hãy để Procedure SQL làm nơi kiểm tra cuối cùng.
- **Logic xử lý trùng:** 
  - Nếu trạng thái là REGISTERED hoặc COMPLETED -> Từ chối.
  - Nếu trạng thái là DROPPED và chưa có điểm -> Sử dụng lại `CourseEnrollmentID` cũ, chuyển trạng thái về REGISTERED (Không tạo bản ghi mới).
  - Nếu đã có điểm -> Yêu cầu học vụ xử lý.
- **Kịch bản Test:** Chạy test đồng thời (concurrency), test đăng ký mới, đăng ký lại sau hủy.

### F16 – Báo cáo học tập & GPA (Ưu tiên cao)
- **Quy tắc:** Lấy dữ liệu qua `IAcademicGpaProvider` từ nguồn chung của Lực.
- **Kịch bản Test:** Đối chiếu tính nhất quán của GPA giữa màn hình hiển thị, tiến độ học tập, báo cáo xuất ra và file CSV. Test các ca học lại, thiếu điểm, và chưa có kết quả.

### F05 – Hồ sơ sinh viên (Chống trùng nghiệp vụ)
- **Quy tắc:** Kiểm tra trùng dựa trên tổ hợp trường (Sinh viên + Loại hồ sơ + Mã chứng từ). Nếu trùng nội dung nhưng hợp lệ thì đưa ra cảnh báo (Warning UI) chứ không cấm tuyệt đối. Dùng cơ chế Archive, không xóa cứng.

### Kiểm thử hệ thống (F02, F03, F09, F20)
- **F02 (Logout):** Đăng xuất khi đang tải dữ liệu; đảm bảo không còn lưu cache phiên đăng nhập của user trước.
- **F03 (Profile):** Phân quyền sửa (Student chỉ sửa email/sđt của mình). Phát hiện xung đột khi 2 người cùng sửa (Concurrency).
- **F09 (Xem đăng ký):** Phân quyền xem theo đúng Actor (Student chỉ thấy của mình, Lecturer thấy lớp phụ trách).
- **F20 (Đổi mật khẩu):** Kiểm tra hash mật khẩu cũ/mới, xác nhận sai mật khẩu phải bị chặn.

## 4. Quy trình nghiệm thu tự động (Definition of Done)
1. AI sửa code C# / SQL theo yêu cầu.
2. AI tự động gõ lệnh `dotnet build` để đảm bảo dự án không bị lỗi cú pháp.
3. AI tự động gõ lệnh `dotnet test` để chạy các kịch bản test tương ứng.
4. Mọi lỗi (Fail) trả về từ Terminal phải được AI tự động đọc và sửa lại cho đến khi Pass.
