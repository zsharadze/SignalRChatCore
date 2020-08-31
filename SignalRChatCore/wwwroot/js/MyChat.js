"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chathub").build();
var chatRooms = [];
var chatUsers = [];
var joinedRoom;
var myName;
var myAvatar;

$(document).ready(function () {

    $("#btnAddToDiv").click(function (event) {
        event.preventDefault();
        
    });

    connection.start().then(function () {
        console.log("SignalR started");
        roomList();
        userList();
    }).catch(function (message) {
        $("#serverInfoMessage").html(message);
        $("#errorAlert").removeClass("hidden").show().delay(5000).fadeOut(500);
        return console.error(err.toString());
    });

    connection.on("getProfileInfo", function (displayName, avatar) {
        myName = displayName;
        myAvatar = avatar;
        $("#userName").html(myName);
        $("#userAvatar").attr("src", "/images/icons/avatar" + (parseInt(avatar)+1)+".png");
    });
});

function roomList() {
    connection.invoke("GetRooms").then((result) => {
        chatRooms = [];
        for (var i = 0; i < result.length; i++) {
            chatRooms.push(result[i]);
            //join first room
            if (i == 0) {
                joinedRoom = chatRooms[0].Name;
                joinRoom();
            }
            //

            //add room names to left list
            var sub_li = $('<li/>');
            var aTag = $("<a>"); 
            aTag.attr("href", "#");
            aTag.attr("data-roomName", result[i].Name);
            aTag.text(result[i].Name);
            if (joinedRoom == result[i].Name)
                aTag.addClass("active");
            aTag.click(function (event) {
                event.preventDefault();

                joinedRoom = $(this).attr("data-roomName");
                joinRoom();
                $("#room-list").find("[class=active]").removeClass("active");
                $(this).addClass("active");
            });

            $(sub_li).html(aTag);

            $("#room-list").append(sub_li);
            //
        }

        //if (chatRooms.length > 0) {
        //    joinedRoom = chatRooms[0].Name;
        //    joinRoom();
        //}
    });

    //chatHub.server.getRooms().done(function (result) {
    //    self.chatRooms.removeAll();
    //    for (var i = 0; i < result.length; i++) {
    //        self.chatRooms.push(new ChatRoom(result[i].Id, result[i].Name));
    //    }
    //});
}

function userList() {
    connection.invoke("getUsers", joinedRoom).then((result) => {
        chatUsers = [];
        for (var i = 0; i < result.length; i++) {
            chatUsers.push(result[i]);
            $("#usersList").append("<br>" + result[i].Username);
        }
    });

    //chatHub.server.getUsers(self.joinedRoom()).done(function (result) {
    //    self.chatUsers.removeAll();
    //    for (var i = 0; i < result.length; i++) {
    //        self.chatUsers.push(new ChatUser(result[i].Username,
    //            result[i].DisplayName,
    //            result[i].Avatar,
    //            result[i].CurrentRoom,
    //            result[i].Device))
    //    }
    //});
}

function joinRoom() {
    connection.invoke("join", joinedRoom).then((result) => {
        userList();
    });
    //chatHub.server.join(self.joinedRoom()).done(function () {
    //    self.userList();
    //    self.messageHistory();
    //});
}