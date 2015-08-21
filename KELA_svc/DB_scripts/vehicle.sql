drop table [dbo].[Vehicle]
go

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
    REFERENCES [dbo].[Order](order_id)
GO