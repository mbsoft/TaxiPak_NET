drop table [dbo].[ForeignResID]
go

CREATE TABLE [dbo].[ForeignResID] ( 
    [connection_id]       	int NOT NULL,
    [src]                  varchar(64),
    [id]                  varchar(64)
)
GO

ALTER TABLE [dbo].[ForeignResID]
    ADD CONSTRAINT [FK_ForeignResIDConnectionId]
    FOREIGN KEY (connection_id)
    REFERENCES [dbo].[Connection](connection_id)
GO