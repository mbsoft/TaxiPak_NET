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

CREATE TABLE [dbo].[OrderSUTI] ( 
    [order_id]       	int NOT NULL,
    [tpak_id]         	int,
    [created]           datetime,
    [modified]          datetime,
    CONSTRAINT [PK_Order] PRIMARY KEY([order_id])
)
GO
ALTER TABLE [dbo].[OrderSUTI]
    ADD CONSTRAINT [OrderKey1]
	UNIQUE ([order_id])
GO


CREATE TRIGGER [dbo].[OrderInsert] ON OrderSUTI
FOR INSERT
AS
    UPDATE OrderSUTI
    SET created = GetDate()
    FROM OrderSUTI, inserted
    WHERE OrderSUTI.order_id = inserted.order_id
GO

CREATE TABLE [dbo].[Route] ( 
    [rte_id]       	int IDENTITY(1,1) PRIMARY KEY CLUSTERED,
    [order_id]      int NOT NULL
)
GO
ALTER TABLE [dbo].[Route]
    ADD CONSTRAINT [RouteKey1]
	UNIQUE ([rte_id])
GO
ALTER TABLE [dbo].[Route]
    ADD CONSTRAINT [FK_OrderId]
    FOREIGN KEY (order_id)
    REFERENCES [dbo].[OrderSUTI](order_id)
GO



CREATE TABLE [dbo].[Node] ( 
    [node_id]       	int IDENTITY(1,1) PRIMARY KEY CLUSTERED,
    [rte_id]         	int NOT NULL,
    [seq_no]            smallint,
    [type]              char,
    [street]            varchar(128),
    [streetnbr]         varchar(32),
    [locality]          varchar(64),
    [duetime]           datetime
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



CREATE TABLE [dbo].[Economy] ( 
    [order_id]       	int NOT NULL,
    [fixed_price]       char,
    [price]             money,
    [vatinclusive]      char,
    CONSTRAINT [PK_Economy] PRIMARY KEY([order_id])
)
GO
ALTER TABLE [dbo].[Economy]
    ADD CONSTRAINT [EconomyKey1]
	UNIQUE ([order_id])
GO
ALTER TABLE [dbo].[Economy]
    ADD CONSTRAINT [FK_EconomyOrderId]
    FOREIGN KEY (order_id)
    REFERENCES [dbo].[OrderSUTI](order_id)
GO



CREATE TABLE [dbo].[Vehicle] ( 
    [order_id]       	int NOT NULL,
    [src]               varchar(128),
    [id]                int,
    [nbr_seats]         smallint,
    CONSTRAINT [PK_Vehicle] PRIMARY KEY([order_id])
)
GO
ALTER TABLE [dbo].[Vehicle]
    ADD CONSTRAINT [VehicleKey1]
	UNIQUE ([order_id])
GO
ALTER TABLE [dbo].[Vehicle]
    ADD CONSTRAINT [FK_VehicleOrderId]
    FOREIGN KEY (order_id)
    REFERENCES [dbo].[OrderSUTI](order_id)
GO



CREATE TABLE [dbo].[Driver] ( 
    [order_id]       	int NOT NULL,
    [src]               varchar(128),
    [id]                int,
    CONSTRAINT [PK_Driver] PRIMARY KEY([order_id])
)
GO
ALTER TABLE [dbo].[Driver]
    ADD CONSTRAINT [DriverKey1]
	UNIQUE ([order_id])
GO
ALTER TABLE [dbo].[Driver]
    ADD CONSTRAINT [FK_DriverOrderId]
    FOREIGN KEY (order_id)
    REFERENCES [dbo].[OrderSUTI](order_id)
GO



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

---stored procedures
CREATE PROCEDURE [dbo].[InsertRoute]
(   @aOrderID int )
AS

INSERT INTO [dbo].[Route]
    ([order_id])
VALUES
    (@aOrderID)

SELECT @@identity
GO

CREATE PROCEDURE [dbo].[InsertNode]
( @aRteID int,
  @aSeqNo smallint,
  @aType char (1),
  @aStreet varchar(128),
  @aStreetNbr varchar(32),
  @aLocality varchar(64),
  @aDueTime datetime  )
AS

INSERT INTO [dbo].[Node]
    ([rte_id],[seq_no],[type],[street],[streetnbr],[locality],[duetime])
VALUES
    (@aRteID,@aSeqNo,@aType,@aStreet,@aStreetNbr,@aLocality,@aDueTime)

SELECT @@identity
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

CREATE PROCEDURE [dbo].[UpdateOrderTaxiPakID]
( @aOrderID int,
  @aTaxiPakID int
)
AS

UPDATE [dbo].[OrderSUTI]
    SET tpak_id=@aTaxiPakID 
    WHERE order_id=@aOrderID

go
