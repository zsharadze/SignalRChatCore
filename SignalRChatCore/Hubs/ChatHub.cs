using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SignalRChatCore.Data;
using SignalRChatCore.Helpers;
using SignalRChatCore.Models;
using SignalRChatCore.Models.ViewModels;

namespace SignalRChatCore.Hubs
{
    public class ChatHub : Hub
    {
        #region Properties
        private ApplicationDbContext _context;

        /// <summary>
        /// List of online users
        /// </summary>
        public readonly static List<UserViewModel> _Connections = new List<UserViewModel>();

        /// <summary>
        /// List of available chat rooms
        /// </summary>
        private readonly static List<RoomViewModel> _Rooms = new List<RoomViewModel>();

        /// <summary>
        /// Mapping SignalR connections to application users.
        /// (We don't want to share connectionId)
        /// </summary>
        private readonly static Dictionary<string, string> _ConnectionsMap = new Dictionary<string, string>();
        #endregion

        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }


        public void Send(string roomName, string message)
        {
            if (message.StartsWith("/private"))
                SendPrivate(message);
            else
                SendToRoom(roomName, message);
        }

        public void SendPrivate(string message)
        {
            // message format: /private(receiverName) Lorem ipsum...
            string[] split = message.Split(')');
            string receiver = split[0].Split('(')[1];
            string userId;
            if (_ConnectionsMap.TryGetValue(receiver, out userId))
            {
                // Who is the sender;
                var sender = _Connections.Where(u => u.Username == IdentityName).First();

                message = Regex.Replace(message, @"\/private\(.*?\)", string.Empty).Trim();

                // Build the message
                MessageViewModel messageViewModel = new MessageViewModel()
                {
                    From = sender.DisplayName,
                    Avatar = sender.Avatar,
                    To = "",
                    Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", String.Empty),
                    Timestamp = DateTime.Now.ToLongTimeString()
                };

                // Send the message
                Clients.Client(userId).SendAsync("newMessage", messageViewModel);
                Clients.Caller.SendAsync("newMessage", messageViewModel);
            }
        }

        public void SendToRoom(string roomName, string message)
        {
            try
            {
                var user = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                var room = _context.Rooms.Where(r => r.Name == roomName).FirstOrDefault();

                // Create and save message in database
                Message msg = new Message()
                {
                    Content = Regex.Replace(message, @"(?i)<(?!img|a|/a|/img).*?>", String.Empty),
                    Timestamp = DateTime.Now.Ticks.ToString(),
                    FromUser = user,
                    ToRoom = room
                };
                _context.Messages.Add(msg);
                _context.SaveChanges();

                // Broadcast the message
                //map
                MessageViewModel messageViewModel = Mapper.MessageToMessageViewModel(msg);
                Clients.Group(roomName).SendAsync("newMessage", messageViewModel);

            }
            catch (Exception)
            {
                Clients.Caller.SendAsync("onError", "Message not send!");
            }
        }

        public void Join(string roomName)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).FirstOrDefault();
                if (user != null && user.CurrentRoom != roomName)
                {
                    // Remove user from others list
                    if (!string.IsNullOrEmpty(user.CurrentRoom))
                        Clients.OthersInGroup(user.CurrentRoom).SendAsync("removeUser", user);

                    // Join to new chat room
                    Leave(user.CurrentRoom);
                    Groups.AddToGroupAsync(Context.ConnectionId, roomName);
                    user.CurrentRoom = roomName;

                    // Tell others to update their list of users
                    Clients.OthersInGroup(roomName).SendAsync("addUser", user);
                }
            }
            catch (Exception ex)
            {
                Clients.Caller.SendAsync("onError", "You failed to join the chat room!" + ex.Message);
            }
        }

        private void Leave(string roomName)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        }

        public void CreateRoom(string roomName)
        {
            try
            {
                // Accept: Letters, numbers and one space between words.
                Match match = Regex.Match(roomName, @"^\w+( \w+)*$");
                if (!match.Success)
                {
                    Clients.Caller.SendAsync("onError", "Invalid room name!\nRoom name must contain only letters and numbers.");
                }
                else if (roomName.Length < 5 || roomName.Length > 20)
                {
                    Clients.Caller.SendAsync("onError", "Room name must be between 5-20 characters!");
                }
                else if (_context.Rooms.Any(r => r.Name == roomName))
                {
                    Clients.Caller.SendAsync("onError", "Another chat room with this name exists");
                }
                else
                {
                    // Create and save chat room in database
                    var user = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();
                    var room = new Room()
                    {
                        Name = roomName,
                        UserAccount = user
                    };
                    _context.Rooms.Add(room);
                    _context.SaveChanges();

                    if (room != null)
                    {
                        // Update room list
                        //map
                        RoomViewModel roomViewModel = Mapper.RoomToRoomViewModel(room);

                        _Rooms.Add(roomViewModel);
                        Clients.All.SendAsync("addChatRoom", roomViewModel);
                    }
                }
            }
            catch (Exception ex)
            {
                Clients.Caller.SendAsync("onError", "Couldn't create chat room: " + ex.Message);
            }
        }

        public void DeleteRoom(string roomName)
        {
            try
            {
                // Delete from database
                var room = _context.Rooms.Where(r => r.Name == roomName && r.UserAccount.UserName == IdentityName).FirstOrDefault();
                _context.Rooms.Remove(room);
                _context.SaveChanges();

                // Delete from list
                var roomViewModel = _Rooms.First<RoomViewModel>(r => r.Name == roomName);
                _Rooms.Remove(roomViewModel);

                // Move users back to Lobby
                Clients.Group(roomName).SendAsync("onRoomDeleted", string.Format("Room {0} has been deleted.\nYou are now moved to the Lobby!", roomName));

                // Tell all users to update their room list
                Clients.All.SendAsync("removeChatRoom", roomViewModel);
            }
            catch (Exception)
            {
                Clients.Caller.SendAsync("onError", "Can't delete this chat room.");
            }
        }

        public IEnumerable<MessageViewModel> GetMessageHistory(string roomName)
        {
            var messageHistory = _context.Messages.Where(m => m.ToRoom.Name == roomName)
                .OrderByDescending(m => m.Timestamp)
                .Take(20)
                .AsEnumerable()
                .Reverse()
                .ToList();

            List<MessageViewModel> returnVal = new List<MessageViewModel>();
            foreach (var item in messageHistory)
            {
                MessageViewModel messageViewModel = Mapper.MessageToMessageViewModel(item);
                returnVal.Add(messageViewModel);
            }
            return returnVal.AsEnumerable();
        }

        public IEnumerable<RoomViewModel> GetRooms()
        {
            // First run?
            if (_Rooms.Count == 0)
            {
                foreach (var room in _context.Rooms)
                {
                    //map
                    RoomViewModel roomViewModel = Mapper.RoomToRoomViewModel(room);

                    _Rooms.Add(roomViewModel);
                }
            }
            return _Rooms.ToList();
        }

        public IEnumerable<UserViewModel> GetUsers(string roomName)
        {
            return _Connections.Where(u => u.CurrentRoom == roomName).ToList();
        }

        #region OnConnected/OnDisconnected
        public override Task OnConnectedAsync()
        {
            try
            {
                var user = _context.Users.Where(u => u.UserName == IdentityName).FirstOrDefault();

                //map
                UserViewModel userViewModel = Mapper.ApplicationUserToUserViewModel(user);

                if (!_Connections.Any(u => u.Username == IdentityName))
                {
                    _Connections.Add(userViewModel);
                    _ConnectionsMap.Add(IdentityName, Context.ConnectionId);
                }

                Clients.Caller.SendAsync("getProfileInfo", user.DisplayName, user.Avatar);
            }
            catch (Exception ex)
            {
                Clients.Caller.SendAsync("onError", "OnConnected:" + ex.Message);
            }

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var user = _Connections.Where(u => u.Username == IdentityName).First();
                _Connections.Remove(user);

                // Tell other users to remove you from their list
                Clients.OthersInGroup(user.CurrentRoom).SendAsync("removeUser", user);

                // Remove mapping
                _ConnectionsMap.Remove(user.Username);
            }
            catch (Exception ex)
            {
                Clients.Caller.SendAsync("onError", "OnDisconnected: " + ex.Message);
            }

            return base.OnDisconnectedAsync(exception);
        }

        //public override Task OnReconnected()
        //{
        //    var user = _Connections.Where(u => u.Username == IdentityName).FirstOrDefault();
        //    if (user != null)
        //        Clients.Caller.getProfileInfo(user.DisplayName, user.Avatar);

        //    return base.OnReconnected();
        //}
        #endregion

        private string IdentityName
        {
            get { return Context.User.Identity.Name; }
        }

        //private string GetDevice()
        //{
        //    string device = Context.Headers.Get("Device");

        //    if (device != null && (device.Equals("Desktop") || device.Equals("Mobile")))
        //        return device;

        //    return "Web";
        //}
    }
}
