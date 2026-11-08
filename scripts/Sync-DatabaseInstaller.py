"""Keep the one-file fresh installer and migration 002 identical. Run with Python 3."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
p=root/'Database/StudentManagement_G4.sql'
s=p.read_text(); marker='-- BEGIN RUNTIME HARDENING (generated from migration 002)'
s=s[:s.index(marker)].rstrip()+'\n\n'
body=(root/'Database/Migrations/002_RuntimeHardening.sql').read_text().split('\n',1)[1]
s+=marker+"\nDECLARE @HardeningSql nvarchar(max)=N'USE '+QUOTENAME(@DatabaseName)+N';\n"+body.replace("'","''")+"';\nEXEC sys.sp_executesql @HardeningSql;\n-- END RUNTIME HARDENING\n"
p.write_text(s)
