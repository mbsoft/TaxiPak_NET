drop table [dbo].[Economy]
go

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
    REFERENCES [dbo].[Order](order_id)
GO