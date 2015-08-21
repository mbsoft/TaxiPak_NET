drop table [dbo].[Driver]
go

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
    REFERENCES [dbo].[Order](order_id)
GO