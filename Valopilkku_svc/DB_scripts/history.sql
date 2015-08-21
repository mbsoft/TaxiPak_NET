
drop TABLE [dbo].[History]
go


CREATE TABLE [dbo].[History] ( 
    [hist_id]	int IDENTITY(1,1) NOT NULL,
    [msg_id] 	bigint NOT NULL,
    [created] 	datetime NULL,
    [xml]	    varchar(8000) NULL,
    CONSTRAINT [PK_History] PRIMARY KEY([hist_id])
)
GO
ALTER TABLE [dbo].[History]
    ADD CONSTRAINT [HistoryKey1]
	UNIQUE ([hist_id])
GO

CREATE TRIGGER [dbo].[HistoryInsert] ON History
FOR INSERT
AS
    UPDATE History
    SET created = GetDate()
    FROM History, inserted
    WHERE History.msg_id = inserted.msg_id
GO
