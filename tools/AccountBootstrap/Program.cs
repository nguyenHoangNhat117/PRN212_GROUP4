using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

bool createAdmin = args.Length==2 && args[1]=="--create-first-admin";
if(args.Length!=1 && !createAdmin) { Console.Error.WriteLine("Usage: AccountBootstrap <username> [--create-first-admin]; set STUDENT_ADMIN_SQL. Resets an existing account; does not create roles."); return 1; }
if(string.IsNullOrWhiteSpace(args[0]) || args[0].Length>80) { Console.Error.WriteLine("Username must contain 1-80 characters."); return 1; }
var connection=Environment.GetEnvironmentVariable("STUDENT_ADMIN_SQL");
if(string.IsNullOrWhiteSpace(connection)) { Console.Error.WriteLine("Set STUDENT_ADMIN_SQL to an administrator connection."); return 1; }
Console.Write("Temporary password (6-128 characters): ");
string ReadSecret() { var chars=new List<char>(); while(true) { var key=Console.ReadKey(true); if(key.Key==ConsoleKey.Enter) break; if(key.Key==ConsoleKey.Backspace) { if(chars.Count>0) chars.RemoveAt(chars.Count-1); } else if(!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar); } Console.WriteLine(); return new string(chars.ToArray()); }
var password=ReadSecret(); Console.Write("Confirm: "); var confirm=ReadSecret();
if(password!=confirm || string.IsNullOrWhiteSpace(password) || password.Length<6 || password.Length>128) { Console.Error.WriteLine("Invalid password or confirmation."); return 1; }
var salt=RandomNumberGenerator.GetBytes(16);
var hash=Rfc2898DeriveBytes.Pbkdf2(password,salt,210000,HashAlgorithmName.SHA256,32);
await using var db=new SqlConnection(connection); await db.OpenAsync();
await using var command=new SqlCommand("UPDATE dbo.Users SET PasswordHash=@hash,PasswordSalt=@salt,PasswordAlgorithm='PBKDF2-SHA256',PasswordIterations=210000,MustChangePassword=1,UpdatedAt=SYSUTCDATETIME() WHERE Username=@username AND IsActive=1",db);
if(createAdmin)
{
    command.CommandText = @"SET XACT_ABORT ON; SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRY
 BEGIN TRAN;
 DECLARE @n int;
 SELECT @n=COUNT(*) FROM dbo.Users WITH(UPDLOCK,HOLDLOCK);
 IF EXISTS(SELECT 1 FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.RoleID=ur.RoleID WHERE r.RoleCode='ADMIN')
 THROW 51630,'An administrator already exists; use account maintenance.',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Roles WHERE RoleCode='ADMIN') THROW 51631,'ADMIN role missing.',1;
 INSERT dbo.Users(Username,DisplayName,PasswordHash,PasswordSalt,PasswordAlgorithm,PasswordIterations,MustChangePassword)
 VALUES(@username,@username,@hash,@salt,'PBKDF2-SHA256',210000,1);
 DECLARE @id int=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.UserRoles(UserID,RoleID) SELECT @id,RoleID FROM dbo.Roles WHERE RoleCode='ADMIN';
 COMMIT;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;";
}
command.Parameters.AddWithValue("@hash",Convert.ToHexString(hash)); command.Parameters.AddWithValue("@salt",Convert.ToHexString(salt)); command.Parameters.AddWithValue("@username",args[0]);
int changed;
try { changed=await command.ExecuteNonQueryAsync(); }
catch(SqlException ex) { Console.Error.WriteLine($"Credential update rejected (SQL {ex.Number}): {ex.Message}"); return 1; }
if(!createAdmin && changed!=1) { Console.Error.WriteLine("Active account not found."); return 1; }
Console.WriteLine("Credential initialized. Account must change password at next login."); return 0;
