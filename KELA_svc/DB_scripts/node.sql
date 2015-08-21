drop table [dbo].[Node]
go

CREATE TABLE [dbo].[Node] ( 
    [node_id]       	int IDENTITY(1,1) PRIMARY KEY CLUSTERED,
    [rte_id]         	int NOT NULL,
    [seq_no]            smallint,
    [type]              char,
    [street]            varchar(128),
    [streetnbr]         varchar(32),
    [locality]          varchar(64),
    [duetime]           datetime,
    [description]       varchar(128)
)
GO
ALTER TABLE [dbo].[Node]
    ADD CONSTRAINT [NodeKey1]
	UNIQUE ([node_id])
GO
ALTER TABLE [dbo].[Node]
    ADD CONSTRAINT [FK_NodeRteId]
    FOREIGN KEY (rte_id)
    REFERENCES [dbo].[Route](rte_id)
GO

CREATE PROCEDURE [dbo].[InsertNode]
( @aRteID int,
  @aSeqNo smallint,
  @aType char (1),
  @aStreet varchar(128),
  @aStreetNbr varchar(32),
  @aLocality varchar(64),
  @aDueTime datetime,
  @aDescript varchar(128)  )
AS

INSERT INTO [dbo].[Node]
    ([rte_id],[seq_no],[type],[street],[streetnbr],[locality],[duetime],[description])
VALUES
    (@aRteID,@aSeqNo,@aType,@aStreet,@aStreetNbr,@aLocality,@aDueTime,@aDescript)

SELECT @@identity
GO