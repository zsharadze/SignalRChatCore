"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chathub").build();
var chatRooms = new Object();

$(document).ready(function () {

    $("#btnAddToDiv").click(function (event) {
        event.preventDefault();

        //connection.invoke("testdivevent").catch(function (err) {
        //    return console.error(err.toString());
        //});
       
       //.append("default " + event.type + " prevented")
        
    });

    //connection.on("addtotestdiv", function (par1) {
    //    $("#testdiv").append("<br>" + par1);
    //});

    connection.start().then(function () {
        console.log("SignalR started");
        roomList();
        //model.userList();
    }).catch(function (message) {
        $("#serverInfoMessage").html(message);
        $("#errorAlert").removeClass("hidden").show().delay(5000).fadeOut(500);
        return console.error(err.toString());
    });
});

function roomList() {
    connection.invoke("GetRooms").then((result) => {
        for (var i = 0; i < result.length; i++) {
            chatRooms[result[i].Id] = result[i].Name;
            $("#roomsList").append("<br>" + result[i].Name);
        }

    });

    //chatHub.server.getRooms().done(function (result) {
    //    self.chatRooms.removeAll();
    //    for (var i = 0; i < result.length; i++) {
    //        self.chatRooms.push(new ChatRoom(result[i].Id, result[i].Name));
    //    }
    //});
}