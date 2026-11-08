"""Linux/Docker verification: regression tests, bootstrap, backup and actual restore.
Run after dotnet build and dotnet build tools/AccountBootstrap. Uses disposable SQL only.
"""
import subprocess,os,secrets,time,pty,select
from pathlib import Path
root=str(Path(__file__).resolve().parents[1])
dotnet=os.environ.get('DOTNET_EXECUTABLE','dotnet')
name='g4-package-'+secrets.token_hex(5); password='Runtime!9a'+secrets.token_hex(12)
env=os.environ.copy();env['MSSQL_SA_PASSWORD']=password
subprocess.run(['docker','run','-d','--name',name,'-e','ACCEPT_EULA=Y','-e','MSSQL_PID=Developer','-e','MSSQL_SA_PASSWORD','-p','127.0.0.1:14334:1433','mcr.microsoft.com/mssql/server:2022-latest'],env=env,check=True,stdout=subprocess.DEVNULL)
def sql(text):
 result=subprocess.run(['docker','exec','-i',name,'sh','-c','SQLCMDPASSWORD="$MSSQL_SA_PASSWORD" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -C -b'],input=text,text=True,stdout=subprocess.PIPE,stderr=subprocess.STDOUT)
 if result.returncode: raise RuntimeError(result.stdout[-2000:])
 return result.stdout
try:
 for i in range(90):
  try: sql('SELECT 1');break
  except RuntimeError:time.sleep(1)
 else:raise RuntimeError('SQL startup timeout')
 env['STUDENT_TEST_SQL']=f'Server=127.0.0.1,14334;Database=master;User ID=sa;Password={password};Encrypt=True;TrustServerCertificate=True'
 subprocess.run([dotnet,'test','--no-build','--nologo','--logger','trx'],cwd=root,env=env,check=True)
 install=open(root+'/Database/Install_Production.sql').read().replace('@GradePolicyConfirmed bit=0','@GradePolicyConfirmed bit=1')
 sql(install)
 env['STUDENT_ADMIN_SQL']=f'Server=127.0.0.1,14334;Database=PRN212_StudentManagement_G4_Production;User ID=sa;Password={password};Encrypt=True;TrustServerCertificate=True'
 def bootstrap(user,create):
  master,slave=pty.openpty()
  proc=subprocess.Popen([dotnet,root+'/tools/AccountBootstrap/bin/Debug/net8.0/AccountBootstrap.dll',user]+(['--create-first-admin'] if create else []),stdin=slave,stdout=slave,stderr=slave,env=env)
  os.close(slave);output=b'';sent1=sent2=False;deadline=time.time()+20
  while time.time()<deadline:
   readable,_,_=select.select([master],[],[],0.2)
   if readable:
    try: chunk=os.read(master,8192)
    except OSError:break
    if not chunk:break
    output+=chunk
    if b'Temporary password' in output and not sent1:os.write(master,b'Temporary!987\n');sent1=True
    if b'Confirm:' in output and not sent2:os.write(master,b'Temporary!987\n');sent2=True
   if proc.poll() is not None:break
  if proc.poll() is None:proc.kill();raise RuntimeError('Bootstrap prompt timed out')
  os.close(master);return proc.wait()
 assert bootstrap('real_admin',True)==0,'First admin creation failed'
 assert bootstrap('second_admin',True)!=0,'Second bootstrap must be refused'
 assert bootstrap('real_admin',False)==0,'Password reset failed'
 sql("""USE PRN212_StudentManagement_G4_Production;
 IF (SELECT COUNT(*) FROM dbo.Users)<>1 THROW 51999,'Unexpected accounts',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Users WHERE Username='real_admin' AND MustChangePassword=1 AND LEN(PasswordHash)=64 AND LEN(PasswordSalt)=32) THROW 51999,'Bad bootstrap credential',1;
 BACKUP DATABASE PRN212_StudentManagement_G4_Production TO DISK='/var/opt/mssql/data/G4_test.bak' WITH INIT,CHECKSUM;
 RESTORE VERIFYONLY FROM DISK='/var/opt/mssql/data/G4_test.bak' WITH CHECKSUM;
 """)
 # Determine logical names instead of assuming installation-specific names.
 sql("""DECLARE @data sysname,@log sysname,@restore nvarchar(max);
 SELECT @data=name FROM sys.master_files WHERE database_id=DB_ID('PRN212_StudentManagement_G4_Production') AND type=0;
 SELECT @log=name FROM sys.master_files WHERE database_id=DB_ID('PRN212_StudentManagement_G4_Production') AND type=1;
 SET @restore=N'RESTORE DATABASE G4_Restore_Test FROM DISK=''/var/opt/mssql/data/G4_test.bak'' WITH MOVE '+QUOTENAME(@data,'''')+N' TO ''/var/opt/mssql/data/G4_restore.mdf'', MOVE '+QUOTENAME(@log,'''')+N' TO ''/var/opt/mssql/data/G4_restore.ldf'', CHECKSUM;';
 EXEC sys.sp_executesql @restore;
 IF (SELECT COUNT(*) FROM G4_Restore_Test.dbo.Users)<>1 THROW 51999,'Restore count mismatch',1;
 """)
 print('PASS: production install, interactive first-admin bootstrap, duplicate-admin refusal, reset, BACKUP CHECKSUM, VERIFYONLY and actual RESTORE with account preserved.')
finally:
 subprocess.run(['docker','rm','-f',name],stdout=subprocess.DEVNULL)
