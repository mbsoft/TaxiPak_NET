drop table [dbo].[Suborder]
go

CREATE TABLE [dbo].[Suborder] ( 
    [sub_id]       	        int PRIMARY KEY,
    [contents_id]           int NOT NULL,
    [src]                   varchar(64)
)
GO
ALTER TABLE [dbo].[Suborder]
    ADD CONSTRAINT [SuborderKey1]
	UNIQUE ([sub_id])
GO
ALTER TABLE [dbo].[Suborder]
    ADD CONSTRAINT [FK_SuborderContentsId]
    FOREIGN KEY (contents_id)
    REFERENCES [dbo].[Contents](contents_id)
GO