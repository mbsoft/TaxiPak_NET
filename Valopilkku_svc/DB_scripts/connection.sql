drop table [dbo].[Connection]
go

CREATE TABLE [dbo].[Connection] ( 
    [connection_id]       	int IDENTITY(1,1) PRIMARY KEY CLUSTERED,
    [contents_id]           int NOT NULL,
    [type]                  varchar(64),
    [name]                  varchar(64),
    [arrdep]                char,
    [duetime]               datetime
)
GO
ALTER TABLE [dbo].[Connection]
    ADD CONSTRAINT [ConnectionKey1]
	UNIQUE ([connection_id])
GO
ALTER TABLE [dbo].[Connection]
    ADD CONSTRAINT [FK_ConnectionContentsId]
    FOREIGN KEY (contents_id)
    REFERENCES [dbo].[Contents](contents_id)
GO