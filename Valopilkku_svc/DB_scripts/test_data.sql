--Clear out all data from tables
delete from [dbo].[ForeignResID]
go
delete from [dbo].[Connection]
go
delete from [dbo].[Contents]
go
delete from [dbo].[Node]
go
delete from [dbo].[Route]
go
delete from [dbo].[order]
go

--Create the main order
insert into [dbo].[order](order_id) values (21758)
go
--Link a route to this order
insert into [dbo].[route](order_id) values (21758)
go
--Add the first node and all related records for the node
insert into [dbo].[node](rte_id,seq_no,type,street,streetnbr,locality,duetime) values
 (@@identity,1,'P','WELCHVAG','12','GBG',GetDate())
insert into [dbo].[contents](node_id,type,name,description) values
 (@@identity,'T','BERGMARK/JAN AKE MR','BILJETTYP:B-LOS')
insert into [dbo].[connection](contents_id,type,name,arrdep,duetime) values
 (@@identity,'IATA:FLIGHT','SK213','A',GetDate())
DECLARE @connectID int
SELECT @connectID = max(connection.connection_id) from connection
insert into [dbo].[foreignresid](connection_id,src,id) values
 (@connectID,'AMADEUSPNR','Y8QUHC');
insert into [dbo].[foreignresid](connection_id,src,id) values
 (@connectID,'SK:CMP','BANVERK');
insert into [dbo].[foreignresid](connection_id,src,id) values
 (@connectID,'RESAID:PNR','OB085');
go 
--Add the second node and all related records for the node
DECLARE @RouteID int
SELECT @RouteID = max(route.rte_id) from Route
insert into [dbo].[node](rte_id,seq_no,type,street,streetnbr,locality,duetime) values
 (@RouteID,2,'D','NILSPERSVAG','11','GBG',GetDate()+.025)

go
