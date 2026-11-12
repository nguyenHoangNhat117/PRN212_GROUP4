using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using PRN212_GROUP4.Extensions.Services;
using PRN212_GROUP4.Services;

namespace PRN212_GROUP4.Extensions.Views;

/// <summary>
/// Separate extension workspace. The original project files are referenced but not modified.
/// </summary>
public sealed class ExtendedModulesWindow : Window
{
    private readonly FeatureDataService data = new();
    private readonly BackupRestoreService backup = new();
    private readonly TabControl tabs = new();

    private TextBox searchText = new();
    private DataGrid studentGrid = new();

    private DataGrid courseGrid = new();
    private TextBox courseCode = new();
    private TextBox courseName = new();
    private TextBox courseCredits = new();
    private ComboBox courseDepartment = new();
    private int? selectedCourseId;
    private CourseRow? selectedCourse;

    private DataGrid classGrid = new();
    private TextBox classCode = new();
    private ComboBox classCourse = new();
    private ComboBox classSemester = new();
    private ComboBox classLecturer = new();
    private TextBox classCapacity = new();
    private TextBox classSchedule = new();
    private ComboBox classStatus = new();
    private int? selectedClassId;
    private ClassRow? selectedClass;

    private DataGrid registrationGrid = new();
    private ComboBox matchingClass = new();
    private RegistrationRow? selectedRegistration;

    private DataGrid usersGrid = new();
    private TextBox username = new();
    private TextBox displayName = new();
    private TextBox email = new();
    private PasswordBox temporaryPassword = new();
    private ComboBox accountRole = new();
    private CheckBox accountActive = new();
    private int? selectedUserId;

    private ComboBox permissionRole = new();
    private DataGrid permissionGrid = new();

    private TextBox backupPath = new();
    private TextBlock backupStatus = new();

    public ExtendedModulesWindow()
    {
        Title = "Student Management · Additional Modules";
        Width = 1420;
        Height = 900;
        MinWidth = 1120;
        MinHeight = 720;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(255, 248, 238));
        FontFamily = new FontFamily("Segoe UI");

        var root = new DockPanel { Margin = new Thickness(18) };
        var header = new DockPanel { Margin = new Thickness(4, 0, 4, 14) };
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock
        {
            Text = "CAMPUS / GROUP 04",
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#9A3412")
        });
        heading.Children.Add(new TextBlock
        {
            Text = "Chức năng nghiệp vụ mở rộng",
            FontSize = 25,
            FontWeight = FontWeights.Bold,
            Foreground = Brush("#253044")
        });
        heading.Children.Add(new TextBlock
        {
            Text = $"Đăng nhập: {AppSession.DisplayAccess?.DisplayName ?? "User"} · Các chức năng được hiển thị theo quyền database",
            FontSize = 12,
            Foreground = Brush("#596274")
        });
        header.Children.Add(heading);
        var close = Button("Đóng cửa sổ", Close);
        close.HorizontalAlignment = HorizontalAlignment.Right;
        DockPanel.SetDock(close, Dock.Right);
        header.Children.Add(close);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);
        root.Children.Add(tabs);
        Content = root;
        Loaded += async (_, _) => await InitializeModulesAsync();
    }

    private async Task InitializeModulesAsync()
    {
        try
        {
            var codes = await data.GetAvailableFeaturesAsync();
            if (codes.Contains("F04")) tabs.Items.Add(BuildSearchTab());
            if (codes.Contains("F06")) tabs.Items.Add(await BuildCoursesTabAsync());
            if (codes.Contains("F07")) tabs.Items.Add(await BuildClassesTabAsync());
            if (codes.Contains("F10")) tabs.Items.Add(await BuildAssignmentTabAsync());
            if (codes.Contains("F17")) tabs.Items.Add(await BuildUsersTabAsync());
            if (codes.Contains("F18")) tabs.Items.Add(await BuildPermissionsTabAsync());
            if (codes.Contains("F19")) tabs.Items.Add(BuildBackupTab());

            if (tabs.Items.Count == 0)
                tabs.Items.Add(new TabItem { Header = "Không có quyền", Content = new TextBlock
                {
                    Text = "Tài khoản hiện tại chưa được cấp các chức năng mở rộng F04, F06, F07, F10, F17, F18 hoặc F19.",
                    Margin = new Thickness(24), FontSize = 16
                }});
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Không tải được chức năng", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }
    }

    private TabItem BuildSearchTab()
    {
        searchText = Field("Nhập Student ID, mã sinh viên, tên, lớp, email, số điện thoại hoặc khoa", 520);
        studentGrid = Grid<StudentSearchRow>();
        var search = Button("Tìm kiếm", async () => await RunAsync(async () =>
        {
            studentGrid.ItemsSource = await data.SearchStudentsAsync(searchText.Text);
        }));
        var clear = Button("Xóa lọc", async () =>
        {
            searchText.Clear();
            await RunAsync(async () => { studentGrid.ItemsSource = await data.SearchStudentsAsync(""); });
        });
        var top = FormPanel("F04 · Search Student", "Academic Staff chỉ xem sinh viên thuộc khoa mình; Lecturer chỉ xem sinh viên trong lớp được phân công.",
            Row("Tiêu chí tìm kiếm", searchText, search, clear));
        var panel = Page(top, studentGrid);
        return new TabItem { Header = "F04 · Search Student", Content = panel };
    }

    private async Task<TabItem> BuildCoursesTabAsync()
    {
        courseGrid = Grid<CourseRow>();
        courseCode = Field("Mã môn", 150); courseName = Field("Tên môn học", 260);
        courseCredits = Field("Tín chỉ", 80); courseDepartment = LookupCombo(190);
        courseDepartment.ItemsSource = await data.DepartmentsAsync();
        courseDepartment.SelectedValuePath = "Id"; courseDepartment.DisplayMemberPath = "Name";
        courseGrid.SelectionChanged += (_, _) =>
        {
            selectedCourse = courseGrid.SelectedItem as CourseRow;
            selectedCourseId = selectedCourse?.CourseId;
            if (selectedCourse != null)
            {
                courseCode.Text = selectedCourse.CourseCode; courseName.Text = selectedCourse.CourseName;
                courseCredits.Text = selectedCourse.Credits.ToString();
                courseDepartment.SelectedItem = ((IEnumerable<LookupItem>)courseDepartment.ItemsSource!).FirstOrDefault(x => x.Name == selectedCourse.Department);
            }
        };
        var form = Row("Mã", courseCode, "Tên", courseName, "Tín chỉ", courseCredits, "Khoa", courseDepartment);
        var buttons = Row(
            Button("Mới", () => ClearCourse()),
            Button("Lưu môn học", async () => await RunAsync(async () =>
            {
                if (!int.TryParse(courseCredits.Text, out var credits)) throw new ArgumentException("Tín chỉ phải là số nguyên.");
                await data.SaveCourseAsync(selectedCourseId, courseCode.Text, courseName.Text, credits, courseDepartment.SelectedValue is int departmentId ? departmentId : (int?)null);
                await RefreshCoursesAsync();
                MessageBox.Show(this, "Đã lưu môn học.");
            })),
            Button("Ngừng / Kích hoạt", async () => await RunAsync(async () =>
            {
                if (selectedCourse == null) throw new InvalidOperationException("Chọn một môn học trước.");
                await data.SetCourseActiveAsync(selectedCourse.CourseId, !selectedCourse.IsActive);
                await RefreshCoursesAsync();
            })),
            Button("Tải lại", async () => await RunAsync(RefreshCoursesAsync)));
        var top = FormPanel("F06 · Manage Courses", "Thêm, chỉnh sửa và ngừng hoạt động môn học. Không xóa lịch sử đăng ký.", form, buttons);
        var tab = new TabItem { Header = "F06 · Courses", Content = Page(top, courseGrid) };
        await RefreshCoursesAsync();
        return tab;
    }

    private async Task<TabItem> BuildClassesTabAsync()
    {
        classGrid = Grid<ClassRow>();
        classCode = Field("Mã lớp", 140);
        classCourse = LookupCombo(230); classSemester = LookupCombo(210); classLecturer = LookupCombo(210);
        var lookups = await data.ClassLookupsAsync();
        SetLookup(classCourse, lookups.Courses); SetLookup(classSemester, lookups.Semesters); SetLookup(classLecturer, lookups.Lecturers, allowEmpty: true);
        classCapacity = Field("Sức chứa", 90); classSchedule = Field("Ghi chú lịch học", 220);
        classStatus = new ComboBox { Width = 130, ItemsSource = new[] { "OPEN", "CLOSED", "CANCELLED" }, SelectedItem = "OPEN" };
        classGrid.SelectionChanged += (_, _) =>
        {
            selectedClass = classGrid.SelectedItem as ClassRow; selectedClassId = selectedClass?.ClassId;
            if (selectedClass != null)
            {
                classCode.Text = selectedClass.ClassCode; classCourse.SelectedValue = selectedClass.CourseId;
                classSemester.SelectedValue = selectedClass.SemesterId; classLecturer.SelectedValue = selectedClass.LecturerId;
                classCapacity.Text = selectedClass.Capacity?.ToString() ?? ""; classSchedule.Text = selectedClass.ScheduleNote ?? "";
                classStatus.SelectedItem = selectedClass.Status;
            }
        };
        var form1 = Row("Mã lớp", classCode, "Môn học", classCourse, "Học kỳ", classSemester);
        var form2 = Row("Giảng viên", classLecturer, "Sức chứa", classCapacity, "Trạng thái", classStatus);
        var form3 = Row("Lịch học / ghi chú", classSchedule);
        var buttons = Row(
            Button("Mới", () => ClearClass()),
            Button("Lưu lớp", async () => await RunAsync(async () =>
            {
                int? capacity = string.IsNullOrWhiteSpace(classCapacity.Text) ? null : int.TryParse(classCapacity.Text, out var parsed) ? parsed : throw new ArgumentException("Sức chứa phải là số nguyên.");
                await data.SaveClassAsync(selectedClassId, classCode.Text,
                    RequiredSelectedId(classCourse, "Môn học"), RequiredSelectedId(classSemester, "Học kỳ"),
                    classLecturer.SelectedValue is int lecturerId && lecturerId > 0 ? lecturerId : null,
                    capacity, classSchedule.Text, classStatus.SelectedItem?.ToString() ?? "OPEN");
                await RefreshClassesAsync(); MessageBox.Show(this, "Đã lưu lớp.");
            })),
            Button("Tải lại", async () => await RunAsync(RefreshClassesAsync)));
        var top = FormPanel("F07 · Manage Classes", "Quản lý lớp, môn học, học kỳ, giảng viên, sức chứa và ghi chú lịch. Không cho đổi môn/học kỳ khi lớp đã có đăng ký.", form1, form2, form3, buttons);
        var tab = new TabItem { Header = "F07 · Classes", Content = Page(top, classGrid) };
        await RefreshClassesAsync();
        return tab;
    }

    private async Task<TabItem> BuildAssignmentTabAsync()
    {
        registrationGrid = Grid<RegistrationRow>();
        registrationGrid.SelectionChanged += async (_, _) =>
        {
            selectedRegistration = registrationGrid.SelectedItem as RegistrationRow;
            matchingClass.ItemsSource = null;
            if (selectedRegistration != null)
                await RunAsync(async () =>
                {
                    var choices = await data.MatchingClassesAsync(selectedRegistration.CourseId, selectedRegistration.SemesterId);
                    SetLookup(matchingClass, choices);
                    if (matchingClass.Items.Count > 0) matchingClass.SelectedIndex = 0;
                });
        };
        matchingClass = LookupCombo(360);
        var buttons = Row(
            Button("Tải đăng ký chưa xếp lớp", async () => await RunAsync(RefreshRegistrationsAsync)),
            Button("Xếp sinh viên vào lớp", async () => await RunAsync(async () =>
            {
                if (selectedRegistration == null) throw new InvalidOperationException("Chọn đăng ký cần xếp lớp.");
                int classId = RequiredSelectedId(matchingClass, "Lớp");
                await data.AssignStudentAsync(selectedRegistration.CourseEnrollmentId, classId);
                await RefreshRegistrationsAsync();
                MessageBox.Show(this, "Xếp lớp thành công. EnrollmentID đã được tạo.");
            })));
        var top = FormPanel("F10 · Assign Students to Classes", "Chỉ chọn đăng ký còn hiệu lực. Hệ thống kiểm tra môn học, học kỳ, sĩ số và đăng ký trùng trước khi lưu.",
            Row("Lớp phù hợp", matchingClass), buttons);
        var tab = new TabItem { Header = "F10 · Assign Students", Content = Page(top, registrationGrid) };
        await RefreshRegistrationsAsync();
        return tab;
    }

    private async Task<TabItem> BuildUsersTabAsync()
    {
        usersGrid = Grid<UserAccountRow>();
        usersGrid.SelectionChanged += (_, _) =>
        {
            var item = usersGrid.SelectedItem as UserAccountRow;
            if (item == null) { selectedUserId = null; return; }
            selectedUserId = item.UserId; username.Text = item.Username; username.IsReadOnly = true;
            displayName.Text = item.DisplayName; email.Text = item.Email ?? ""; accountActive.IsChecked = item.IsActive;
            accountRole.SelectedValue = item.PrimaryRoleId; temporaryPassword.Clear();
        };
        username = Field("Username", 160); displayName = Field("Tên hiển thị", 200); email = Field("Email", 220);
        temporaryPassword = new PasswordBox
        {
            Width = 180,
            Height = 32,
            Padding = new Thickness(7, 5, 7, 5),
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = "Mật khẩu tạm (8–128 ký tự)"
        };
        accountRole = LookupCombo(210); SetLookup(accountRole, await data.RolesAsync());
        accountActive = new CheckBox { Content = "Đang hoạt động", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
        var form1 = Row("Username", username, "Tên", displayName, "Email", email);
        var form2 = Row("Mật khẩu tạm", temporaryPassword, "Role", accountRole, accountActive);
        var buttons = Row(
            Button("Tạo tài khoản", async () => await RunAsync(async () =>
            {
                await data.CreateUserAsync(username.Text, displayName.Text, email.Text, temporaryPassword.Password, RequiredSelectedId(accountRole, "Role"));
                await RefreshUsersAsync(); ClearUserForm(); MessageBox.Show(this, "Tạo tài khoản thành công. Người dùng phải đổi mật khẩu khi đăng nhập lần đầu.");
            })),
            Button("Cập nhật tài khoản", async () => await RunAsync(async () =>
            {
                if (selectedUserId == null) throw new InvalidOperationException("Chọn tài khoản cần cập nhật.");
                await data.UpdateUserAsync(selectedUserId.Value, displayName.Text, email.Text,
                    RequiredSelectedId(accountRole, "Role"), accountActive.IsChecked == true);
                await RefreshUsersAsync(); MessageBox.Show(this, "Đã cập nhật tài khoản.");
            })),
            Button("Reset mật khẩu", async () => await RunAsync(async () =>
            {
                if (selectedUserId == null) throw new InvalidOperationException("Chọn tài khoản cần reset.");
                await data.ResetUserPasswordAsync(selectedUserId.Value, temporaryPassword.Password);
                MessageBox.Show(this, "Đã đặt mật khẩu tạm. Người dùng phải đổi mật khẩu ở lần đăng nhập tiếp theo.");
                temporaryPassword.Clear();
            })),
            Button("Làm mới", async () => { ClearUserForm(); await RunAsync(RefreshUsersAsync); }));
        var top = FormPanel("F17 · Manage User Accounts", "Tạo, cập nhật, gán role, kích hoạt/vô hiệu hóa và reset mật khẩu. Mật khẩu chỉ lưu dạng PBKDF2-SHA256; tài khoản mới/reset phải đổi mật khẩu.", form1, form2, buttons);
        var tab = new TabItem { Header = "F17 · User Accounts", Content = Page(top, usersGrid) };
        await RefreshUsersAsync();
        return tab;
    }

    private async Task<TabItem> BuildPermissionsTabAsync()
    {
        permissionRole = LookupCombo(380); SetLookup(permissionRole, await data.ListRolesForPermissionsAsync());
        permissionGrid = new DataGrid
        {
            AutoGenerateColumns = false, CanUserAddRows = false, IsReadOnly = false,
            SelectionMode = DataGridSelectionMode.Single, Margin = new Thickness(0, 10, 0, 0)
        };
        permissionGrid.Columns.Add(new DataGridTextColumn { Header = "Function", Binding = new Binding(nameof(RolePermissionRow.FunctionCode)), IsReadOnly = true, Width = 110 });
        permissionGrid.Columns.Add(new DataGridTextColumn { Header = "Tên chức năng", Binding = new Binding(nameof(RolePermissionRow.FunctionName)), IsReadOnly = true, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        permissionGrid.Columns.Add(new DataGridCheckBoxColumn { Header = "Được phép", Binding = new Binding(nameof(RolePermissionRow.Granted)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged }, Width = 120 });
        permissionRole.SelectionChanged += async (_, _) => await RunAsync(LoadRolePermissionsAsync);
        var buttons = Row(
            Button("Tải quyền", async () => await RunAsync(LoadRolePermissionsAsync)),
            Button("Lưu phân quyền", async () => await RunAsync(async () =>
            {
                permissionGrid.CommitEdit(DataGridEditingUnit.Cell, true);
                permissionGrid.CommitEdit(DataGridEditingUnit.Row, true);
                var rows = permissionGrid.ItemsSource as IEnumerable<RolePermissionRow> ?? Array.Empty<RolePermissionRow>();
                await data.SaveRolePermissionsAsync(RequiredSelectedId(permissionRole, "Role"), rows);
                await LoadRolePermissionsAsync();
                MessageBox.Show(this, "Đã cập nhật quyền cho role.");
            })));
        var top = FormPanel("F18 · Manage Roles and Permissions", "Chọn role rồi đánh dấu các chức năng F01–F20 được phép sử dụng. Hệ thống bảo vệ quyền đăng nhập và F18 của role ADMIN để tránh tự khóa quản trị.", Row("Vai trò", permissionRole), buttons);
        var tab = new TabItem { Header = "F18 · Roles & Permissions", Content = Page(top, permissionGrid) };
        if (permissionRole.Items.Count > 0) permissionRole.SelectedIndex = 0;
        return tab;
    }

    private TabItem BuildBackupTab()
    {
        backupPath = Field("Đường dẫn file .bak", 620);
        backupStatus = new TextBlock { TextWrapping = TextWrapping.Wrap, Foreground = Brush("#596274"), Margin = new Thickness(4, 12, 4, 0) };
        var chooseBackup = Button("Chọn nơi lưu", () =>
        {
            var dialog = new SaveFileDialog { Filter = "SQL Server Backup (*.bak)|*.bak", FileName = "StudentManagement_backup_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".bak" };
            if (dialog.ShowDialog(this) == true) backupPath.Text = dialog.FileName;
        });
        var chooseRestore = Button("Chọn file để phục hồi", () =>
        {
            var dialog = new OpenFileDialog { Filter = "SQL Server Backup (*.bak)|*.bak" };
            if (dialog.ShowDialog(this) == true) backupPath.Text = dialog.FileName;
        });
        var doBackup = Button("Backup database", async () => await RunAsync(async () =>
        {
            var progress = new Progress<string>(s => backupStatus.Text = s);
            await backup.BackupAsync(backupPath.Text, progress);
            backupStatus.Text = "Backup hoàn tất: " + backupPath.Text;
            MessageBox.Show(this, backupStatus.Text, "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
        }));
        var doRestore = Button("Restore database", async () => await RunAsync(async () =>
        {
            var confirm = MessageBox.Show(this,
                "CẢNH BÁO: Restore sẽ thay thế dữ liệu hiện tại của database đang cấu hình. Hãy chắc chắn bạn có backup mới nhất. Tiếp tục?",
                "Xác nhận phục hồi", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;
            var progress = new Progress<string>(s => backupStatus.Text = s);
            await backup.RestoreAsync(backupPath.Text, progress);
            MessageBox.Show(this, "Restore hoàn tất. Ứng dụng có thể cần đăng nhập lại.", "Restore", MessageBoxButton.OK, MessageBoxImage.Information);
        }));
        var top = FormPanel("F19 · Backup and Restore Data", "Backup tạo file .bak. Restore thay thế database đích và cần quyền SQL Server phù hợp (thường là sysadmin/dbcreator). Không thể hoàn tác thao tác restore.",
            Row("File .bak", backupPath, chooseBackup, chooseRestore), Row(doBackup, doRestore), backupStatus);
        return new TabItem { Header = "F19 · Backup / Restore", Content = new ScrollViewer { Content = top, VerticalScrollBarVisibility = ScrollBarVisibility.Auto } };
    }

    private async Task RefreshCoursesAsync()
    {
        courseGrid.ItemsSource = await data.ListCoursesAsync();
    }
    private async Task RefreshClassesAsync()
    {
        classGrid.ItemsSource = await data.ListClassesAsync();
    }
    private async Task RefreshRegistrationsAsync()
    {
        registrationGrid.ItemsSource = await data.ListUnassignedRegistrationsAsync();
    }
    private async Task RefreshUsersAsync()
    {
        usersGrid.ItemsSource = await data.ListUsersAsync();
    }
    private async Task LoadRolePermissionsAsync()
    {
        if (permissionRole.SelectedValue is int roleId) permissionGrid.ItemsSource = await data.ListRolePermissionsAsync(roleId);
    }

    private void ClearCourse()
    {
        selectedCourseId = null; selectedCourse = null; courseGrid.SelectedItem = null;
        courseCode.Clear(); courseName.Clear(); courseCredits.Text = "3"; courseDepartment.SelectedIndex = -1;
    }
    private void ClearClass()
    {
        selectedClassId = null; selectedClass = null; classGrid.SelectedItem = null;
        classCode.Clear(); classCourse.SelectedIndex = -1; classSemester.SelectedIndex = -1;
        classLecturer.SelectedIndex = -1; classCapacity.Clear(); classSchedule.Clear(); classStatus.SelectedItem = "OPEN";
    }
    private void ClearUserForm()
    {
        selectedUserId = null; usersGrid.SelectedItem = null; username.Clear(); username.IsReadOnly = false;
        displayName.Clear(); email.Clear(); temporaryPassword.Clear(); accountRole.SelectedIndex = -1; accountActive.IsChecked = true;
    }

    private async Task RunAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Thao tác không thành công", MessageBoxButton.OK, MessageBoxImage.Warning); }
    }

    private static StackPanel Page(UIElement form, UIElement grid)
    {
        var panel = new StackPanel { Margin = new Thickness(10) };
        panel.Children.Add(form);
        panel.Children.Add(grid);
        return panel;
    }

    private static Border FormPanel(string title, string description, params UIElement[] rows)
    {
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = Brush("#253044"), Margin = new Thickness(4, 2, 4, 4) });
        panel.Children.Add(new TextBlock { Text = description, FontSize = 12, Foreground = Brush("#596274"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4, 0, 4, 8) });
        foreach (var row in rows) panel.Children.Add(row);
        return new Border { Background = Brushes.White, BorderBrush = Brush("#D7C7B8"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 12), Child = panel };
    }

    private static StackPanel Row(params UIElement[] elements)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 0, 4) };
        foreach (var element in elements)
        {
            if (element is FrameworkElement frameworkElement)
            {
                frameworkElement.Margin = new Thickness(4, 3, 10, 3);
            }

            row.Children.Add(element);
        }
        return row;
    }

    private static StackPanel Row(params object[] elements)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 4, 0, 4) };
        foreach (var item in elements)
        {
            if (item is string label)
            {
                row.Children.Add(new TextBlock { Text = label, Width = 78, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 3, 2, 3), Foreground = Brush("#596274") });
            }
            else if (item is UIElement element)
            {
                if (element is FrameworkElement frameworkElement)
                {
                    frameworkElement.Margin = new Thickness(4, 3, 10, 3);
                }

                row.Children.Add(element);
            }
        }
        return row;
    }

    private static TextBox Field(string placeholder, double width) => new()
    {
        Width = width, Height = 32, Padding = new Thickness(7, 5, 7, 5),
        ToolTip = placeholder, VerticalContentAlignment = VerticalAlignment.Center
    };

    private static ComboBox LookupCombo(double width) => new()
    {
        Width = width, Height = 32, DisplayMemberPath = "Name", SelectedValuePath = "Id",
        VerticalContentAlignment = VerticalAlignment.Center
    };

    private static void SetLookup(ComboBox combo, IEnumerable<LookupItem> items, bool allowEmpty = false)
    {
        var list = items.ToList();
        if (allowEmpty) list.Insert(0, new LookupItem(0, "(Chưa gán)"));
        combo.ItemsSource = list;
        combo.DisplayMemberPath = nameof(LookupItem.Name);
        combo.SelectedValuePath = nameof(LookupItem.Id);
    }

    private static int RequiredSelectedId(ComboBox combo, string field)
    {
        if (combo.SelectedValue is int id && id > 0) return id;
        throw new ArgumentException("Vui lòng chọn " + field + ".");
    }

    private static DataGrid Grid<T>() => new()
    {
        AutoGenerateColumns = true, IsReadOnly = true, CanUserAddRows = false,
        SelectionMode = DataGridSelectionMode.Single, SelectionUnit = DataGridSelectionUnit.FullRow,
        Margin = new Thickness(0, 0, 0, 8), MinHeight = 280, MaxHeight = 560,
        HeadersVisibility = DataGridHeadersVisibility.Column, GridLinesVisibility = DataGridGridLinesVisibility.Horizontal
    };

    private static Button Button(string text, Action action)
    {
        var button = new Button
        {
            Content = text, Padding = new Thickness(12, 7, 12, 7), MinWidth = 95,
            Background = Brush("#F97316"), Foreground = Brush("#172033"),
            BorderBrush = Brush("#253044"), BorderThickness = new Thickness(1),
            FontWeight = FontWeights.SemiBold, Cursor = System.Windows.Input.Cursors.Hand
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static SolidColorBrush Brush(string color) => new((Color)ColorConverter.ConvertFromString(color));
}
