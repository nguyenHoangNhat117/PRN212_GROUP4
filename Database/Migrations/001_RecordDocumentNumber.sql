-- Connect to the existing application database before running this additive upgrade.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF COL_LENGTH('dbo.StudentRecords', 'DocumentNumber') IS NULL
    ALTER TABLE dbo.StudentRecords ADD DocumentNumber nvarchar(80) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.StudentRecords') AND name='IX_Records_BusinessKey')
    EXEC(N'CREATE INDEX IX_Records_BusinessKey ON dbo.StudentRecords(StudentID, RecordType, DocumentNumber)');
COMMIT;
-- Historical rows keep NULL. Enter the actual document number when editing;
-- do not fabricate business identifiers or delete existing records.
