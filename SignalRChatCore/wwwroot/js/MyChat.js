"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chathub").build();

$(document).ready(function () {

    $("#btnAddToDiv").click(function (event) {
        event.preventDefault();

        connection.invoke("testdivevent").catch(function (err) {
            return console.error(err.toString());
        });
       
       //.append("default " + event.type + " prevented")
        
    });

    connection.on("addtotestdiv", function (par1) {
        $("#testdiv").append("<br>" + par1);
    });

    connection.start().then(function () {
        
    }).catch(function (err) {
        return console.error(err.toString());
    });
});

