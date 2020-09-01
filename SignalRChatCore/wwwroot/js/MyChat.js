"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chathub").build();
var chatRooms = [];
var chatUsers = [];
var joinedRoom;
var myName;
var myAvatar;

$(document).ready(function () {

    $("#createRoom").click(function (event) {
        event.preventDefault();
        createRoom();
    });

    $("#deleteRoom").click(function (event) {
        event.preventDefault();
        connection.invoke("deleteRoom", joinedRoom);
    });

    $("#btn-send-message").click(function (event) {
        event.preventDefault();
        sendMessage();
    });

    $("#chat-message").on('keyup', function (e) {
        if (e.key === 'Enter' || e.keyCode === 13) {
            sendMessage();
        }
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

    connection.on("onError", function (message) {
        $("#serverInfoMessage").html(message);
        $("#errorAlert").removeClass("hidden").show().delay(5000).fadeOut(500);
    });

    connection.on("getProfileInfo", function (displayName, avatar) {
        myName = displayName;
        myAvatar = avatar;
        $("#userName").html(myName);
        $("#userAvatar").attr("src", "/images/icons/avatar" + (parseInt(avatar)+1)+".png");
    });

    connection.on("addChatRoom", function (room) {
        chatRooms.push(room);
        addRoomToLeftPanel(room.Name);
    });

    connection.on("onRoomDeleted", function (message) {
        $("#serverInfoMessage").html(message);
        $("#errorAlert").removeClass("hidden").show().delay(5000).fadeOut(500);

        if (chatRooms.length == 0) {
            joinedRoom = "";
        }
        else {
            // Join to the first room in list
            $("ul#room-list li a")[0].click();
        }
    });

    connection.on("removeChatRoom", function (room) {
        roomDeleted(room.Id);
    });

    connection.on("newMessage", function (newMessage) {
        addMessageToHistory(newMessage);
         
        $(".chat-body").animate({ scrollTop: $(".chat-body")[0].scrollHeight }, 1000);
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
            addRoomToLeftPanel(result[i].Name)
        }

        detectRoom();
    }); 
}

function userList() {
    connection.invoke("getUsers", joinedRoom).then((result) => {
        chatUsers = [];
        for (var i = 0; i < result.length; i++) {
            chatUsers.push(result[i]);
            $("#usersList").append("<br>" + result[i].Username);
        }
    });
}

function joinRoom() {
    connection.invoke("join", joinedRoom).then((result) => {
        userList();
        $("#joinedRoom").html(joinedRoom);
        detectRoom();
        messageHistory();
    });
}

function createRoom () {
    var name = $("#roomName").val();
    connection.invoke("createRoom", name);
    $("#roomName").val("");
}

function addRoomToLeftPanel(roomName) {
    var sub_li = $('<li/>');
    var aTag = $("<a>");
    aTag.attr("href", "#");
    aTag.attr("data-roomName", roomName);
    aTag.text(roomName);
    aTag.attr("id","roomBtn"+roomName);
    if (joinedRoom == roomName)
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
}

function detectRoom() {
    if (joinedRoom) {
        $("#pleaseJoinTxt").css("display", "none");
        $("#lobbyMain").css("display", "flex");
    }
    else {
        $("#pleaseJoinTxt").css("display", "flex");
        $("#lobbyMain").css("display", "none");
    }
}

function roomDeleted(id) {
    var roomNameToRemove;
    for (var i = 0; i < chatRooms.length; i++) {
        if (chatRooms[i].Id == id) {
            roomNameToRemove = chatRooms[i].Name;
            chatRooms.splice(i, 1);
            break;
        }
    }

    $("#roomBtn" + roomNameToRemove).parent().remove();
    joinedRoom = null;
    detectRoom();
}

function messageHistory() {
    connection.invoke("getMessageHistory", joinedRoom).then((result) => {
        $("#chatMessages").html("");
        for (var i = 0; i < result.length; i++) {
            addMessageToHistory(result[i]);
        }

        $(".chat-body").animate({ scrollTop: $(".chat-body")[0].scrollHeight }, 1000);

    });
}

function addMessageToHistory(newMessage) {
    var isMine = newMessage.From == myName;

    var sub_li = $('<li/>');
    var divMain = $('<div>');
    divMain.addClass("chat-message");
    divMain.addClass("d-flex");
    if (isMine)
        divMain.addClass("ismine");
    else
        divMain.addClass("isother");

    var leftSideDiv = $('<div>');
    leftSideDiv.addClass("left-side");
    var imgAvatar = $('<img/>');
    imgAvatar.addClass("message-avatar");
    imgAvatar.attr("src", "/images/icons/avatar" + (parseInt(newMessage.Avatar) + 1) + ".png");
    leftSideDiv.append(imgAvatar);
    divMain.append(leftSideDiv);

    var flexcolumnDiv = $('<div>');
    flexcolumnDiv.addClass("message-content");
    flexcolumnDiv.addClass("d-flex");
    flexcolumnDiv.addClass("flex-column");

    var justifycontentbetweenDiv = $('<div>');
    justifycontentbetweenDiv.addClass("d-flex");
    justifycontentbetweenDiv.addClass("justify-content-between");

    var authorSpan = $('<span>');
    authorSpan.addClass("author");
    authorSpan.html(newMessage.From);
    justifycontentbetweenDiv.append(authorSpan);

    var timestampSpan = $('<span>');
    timestampSpan.addClass("timestamp");
    var glyphicontime = $('<i>');
    glyphicontime.addClass("glyphicon");
    glyphicontime.addClass("glyphicon-time");
    timestampSpan.append(glyphicontime);

    var spanTextTimestamp2 = $('<span>');
    spanTextTimestamp2.html(newMessage.Timestamp);
    timestampSpan.append(spanTextTimestamp2);
    justifycontentbetweenDiv.append(timestampSpan);
    flexcolumnDiv.append(justifycontentbetweenDiv);

    var contentSpan = $('<span>');
    contentSpan.addClass("content");
    contentSpan.html(newMessage.Content);
    flexcolumnDiv.append(contentSpan);
    divMain.append(flexcolumnDiv);
    sub_li.append(divMain);
    $("#chatMessages").append(sub_li);
}

function sendMessage() {
    if (!$("#chat-message").val())
        return;
    connection.invoke("send", joinedRoom, $("#chat-message").val());
    $("#chat-message").val("");
}