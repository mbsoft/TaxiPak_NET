drop table [dbo].[Contents]
go

CREATE TABLE [dbo].[Contents] ( 
    [contents_id]       	int IDENTITY(1,1) PRIMARY KEY CLUSTERED,
    [node_id]         	int NOT NULL,
    [type]              char,
    [name]              varchar(128),
    [description]       varchar(512),
    [contact_phone]     varchar(64)
)
GO
ALTER TABLE [dbo].[Contents]
    ADD CONSTRAINT [ContentsKey1]
	UNIQUE ([contents_id])
GO
ALTER TABLE [dbo].[Contents]
    ADD CONSTRAINT [FK_ContentsNodeId]
    FOREIGN KEY (node_id)
    REFERENCES [dbo].[Node](node_id)
GO

CREATE PROCEDURE [dbo].[InsertContent]
( @aNodeID int,
  @aType char (1),
  @aName varchar(128),
  @aDescription varchar(512),
  @aContactPhone varchar(64))

AS

INSERT INTO [dbo].[Contents]
    ([node_id],[type],[name],[description],[contact_phone])
VALUES
    (@aNodeID,@aType,@aName,@aDescription,@aContactPhone)

SELECT @@identity
GO