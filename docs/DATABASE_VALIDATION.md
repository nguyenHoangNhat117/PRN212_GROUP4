# Kiểm chứng thay đổi database

Base: 439c542. Nhánh: fix/database-runtime-hardening.

- Build solution .NET SDK 8.0.427: PASS, 0 warnings/errors.
- Build tools/AccountBootstrap: PASS, 0 warnings/errors.
- SQL Server 2022 trên container riêng; database tạm riêng cho từng ca.
- dotnet test --no-build --nologo --logger trx: 65 passed, 0 failed, 0 skipped, 47 giây.
- Giữ đủ 55 ca cũ; thêm 10 ca gồm migration chạy lại/giữ dữ liệu, overlap/gap thang điểm, giảm sĩ số, ghi xếp lớp trực tiếp vượt sức chứa, GPA khi trạng thái lệch, hủy và đăng ký lại giữ ID, hủy có điểm phải rollback, hoàn tất cần điểm và cập nhật hai bảng, audit có actor/JSON và rollback không để lại audit.
- Nâng cấp database từ script của base 439c542, chạy migration 001 và 002 rồi chạy lại 002: PASS; giữ 4 bản ghi điểm và 11 đăng ký.
- Không chạy WPF tương tác trên Linux; chưa áp dụng migration lên database đang vận hành của người dùng.
- Backup/restore, chứng chỉ TLS và quyền tài khoản runtime cần được nghiệm thu trên môi trường đích.
