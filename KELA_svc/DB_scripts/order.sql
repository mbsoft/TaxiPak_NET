drop table [dbo].[Order]
go

CREATE TABLE [dbo].[Order] ( 
    [order_id]       	int NOT NULL,
    [tpak_id]         	int,
    [created]           datetime,
    [modified]          datetime,
    CONSTRAINT [PK_Order] PRIMARY KEY([order_id])
)
GO
ALTER TABLE [dbo].[Order]
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

CREATE PROCEDURE [dbo].[UpdateOrderTaxiPakID]
( @aOrderID int,
  @aTaxiPakID int
)
AS

UPDATE [dbo].[OrderSUTI]
    SET tpak_id=@aTaxiPakID 
    WHERE order_id=@aOrderID

go