drop table [dbo].[Route]
go

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
    REFERENCES [dbo].[Order](order_id)
GO