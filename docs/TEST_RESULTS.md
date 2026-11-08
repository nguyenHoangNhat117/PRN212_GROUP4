# Kết quả kiểm thử

- Base: `3bcaae6` (main trước thay đổi).
- Nhánh đề xuất: `fix/nhat-business-rules-tests`.
- .NET SDK: 8.0.427; test runtime net8.0.
- SQL Server: 2022 Developer, 16.0.4305.5, database tạm riêng cho từng ca.
- `dotnet build --nologo`: PASS, 0 warnings, 0 errors.
- `dotnet test --no-build --nologo --logger trx`: 55 passed, 0 failed, 0 skipped; test duration 47 giây.
- `git diff --check`: PASS.
- Không chạy UI WPF tương tác trên Linux. Checklist Windows trong TESTING.md còn chờ người duyệt thực hiện.
- PowerShell runner được cung cấp để tái chạy; phiên này đã chạy build/test trực tiếp với SQL Server Docker.

| Test | Kết quả |
|---|---|
| `PRN212_GROUP4.Tests.AuthenticationTests.All_four_actors_can_sign_in(username: "admin01", role: "ADMIN")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.All_four_actors_can_sign_in(username: "lecturer01", role: "LECTURER")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.All_four_actors_can_sign_in(username: "staff01", role: "ACADEMIC_STAFF")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.All_four_actors_can_sign_in(username: "student01", role: "STUDENT")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.Cancelled_first_change_cannot_be_reused` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.Failed_save_keeps_old_hash_and_old_login_working` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.First_change_save_failure_keeps_password_flag_and_session_blocked` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.First_login_cannot_create_session_until_password_changes(username: "admin01")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.First_login_cannot_create_session_until_password_changes(username: "lecturer01")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.First_login_cannot_create_session_until_password_changes(username: "staff01")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.First_login_cannot_create_session_until_password_changes(username: "student01")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.Inactive_and_unknown_accounts_use_the_same_generic_error` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.Invalid_password_change_preserves_hash(current: "Initial!123", password: "Changed!456", confirmation: "mismatch")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.Invalid_password_change_preserves_hash(current: "wrong", password: "Changed!456", confirmation: "Changed!456")` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.SQL_rejected_password_write_preserves_the_stored_hash` | PASS |
| `PRN212_GROUP4.Tests.AuthenticationTests.Successful_change_accepts_new_password_and_rejects_old_one` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Concurrent_reactivation_reuses_one_id_without_creating_a_new_row` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Concurrent_requests_create_only_one_registration` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Database_rejects_invalid_data_even_when_UI_is_bypassed(setup: "", course: 2, semester: 9999, error: 51122)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Database_rejects_invalid_data_even_when_UI_is_bypassed(setup: "UPDATE dbo.Courses SET IsActive=0 WHERE CourseID=2", course: 2, semester: 2, error: 51121)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Database_rejects_invalid_data_even_when_UI_is_bypassed(setup: "UPDATE dbo.Students SET StudentStatus='INACTIVE' W"···, course: 2, semester: 2, error: 51120)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Dropped_with_grades_requires_academic_review` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Dropped_without_grades_reuses_the_old_id` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Enrollment_queries_respect_actor_scope(username: "lecturer01", ownId: 0)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Enrollment_queries_respect_actor_scope(username: "staff01", ownId: 0)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Enrollment_queries_respect_actor_scope(username: "student01", ownId: 1)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Existing_active_or_completed_registration_is_rejected(student: 1, course: 1, semester: 1)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Existing_active_or_completed_registration_is_rejected(student: 6, course: 1, semester: 2)` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.New_registration_returns_id_without_assigning_a_class` | PASS |
| `PRN212_GROUP4.Tests.EnrollmentTests.Student_cannot_register_another_student` | PASS |
| `PRN212_GROUP4.Tests.LoginValidationTests.Each_missing_field_has_its_own_message(username: "  ", password: "password", usernameMissing: True, passwordMissing: False)` | PASS |
| `PRN212_GROUP4.Tests.LoginValidationTests.Each_missing_field_has_its_own_message(username: "", password: "", usernameMissing: True, passwordMissing: True)` | PASS |
| `PRN212_GROUP4.Tests.LoginValidationTests.Each_missing_field_has_its_own_message(username: "student01", password: "", usernameMissing: False, passwordMissing: True)` | PASS |
| `PRN212_GROUP4.Tests.LoginValidationTests.Each_missing_field_has_its_own_message(username: "student01", password: "password", usernameMissing: False, passwordMissing: False)` | PASS |
| `PRN212_GROUP4.Tests.ProfileAndSessionTests.Logout_during_authorization_query_invalidates_the_pending_request` | PASS |
| `PRN212_GROUP4.Tests.ProfileAndSessionTests.Logout_rejects_protected_calls_and_next_user_gets_only_own_data` | PASS |
| `PRN212_GROUP4.Tests.ProfileAndSessionTests.Stale_profile_update_is_rejected` | PASS |
| `PRN212_GROUP4.Tests.ProfileAndSessionTests.Student_can_update_only_own_contact_fields` | PASS |
| `PRN212_GROUP4.Tests.ReportTests.Lecturer_reports_do_not_expose_unassigned_classes_or_global_GPA` | PASS |
| `PRN212_GROUP4.Tests.ReportTests.Missing_and_unpublished_grades_stay_blank_in_report_and_csv` | PASS |
| `PRN212_GROUP4.Tests.ReportTests.No_results_produce_null_GPA_instead_of_zero` | PASS |
| `PRN212_GROUP4.Tests.ReportTests.Shared_summary_enforces_student_ownership_and_F14_permission` | PASS |
| `PRN212_GROUP4.Tests.ReportTests.Shared_summary_report_and_csv_share_GPA_including_repeat_attempts(repeat: False)` | PASS |
| `PRN212_GROUP4.Tests.ReportTests.Shared_summary_report_and_csv_share_GPA_including_repeat_attempts(repeat: True)` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Additive_upgrade_is_repeatable_and_preserves_existing_records` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Archive_preserves_history_and_still_participates_in_warning` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Concurrent_duplicate_records_are_both_saved_with_a_warning` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Duplicate_key_warns_but_still_persists_both_records` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Every_key_component_is_used(number: "DOC-001", student: 1, type: "Discipline")` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Every_key_component_is_used(number: "DOC-001", student: 2, type: "Academic")` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Every_key_component_is_used(number: "DOC-002", student: 1, type: "Academic")` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Missing_document_number_is_invalid` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Stale_record_update_is_rejected_after_another_editor_saves` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Student_cannot_manage_records` | PASS |
| `PRN212_GROUP4.Tests.StudentRecordTests.Updating_own_key_does_not_warn_but_matching_another_key_does` | PASS |
