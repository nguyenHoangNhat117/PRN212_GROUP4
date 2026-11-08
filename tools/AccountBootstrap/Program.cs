using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

if(args.Length!=1) { Console.Error.WriteLine("Usage: AccountBootstrap <existing-username>; set STUDENT_ADMIN_SQL. Resets an existing account; does not create roles."); return 1; }
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
command.Parameters.AddWithValue("@hash",Convert.ToHexString(hash)); command.Parameters.AddWithValue("@salt",Convert.ToHexString(salt)); command.Parameters.AddWithValue("@username",args[0]);
if(await command.ExecuteNonQueryAsync()!=1) { Console.Error.WriteLine("Active account not found."); return 1; }
Console.WriteLine("Password reset. Account must change password at next login."); return 0;
