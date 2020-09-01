using SignalRChatCore.Models;
using SignalRChatCore.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChatCore.Helpers
{
    public class Mapper
    {
        public static MessageViewModel MessageToMessageViewModel(Message msg)
        {
            MessageViewModel messageViewModel = new MessageViewModel();
            messageViewModel.Content = BasicEmojis.ParseEmojis(msg.Content);
            messageViewModel.From = msg.FromUser.DisplayName;
            messageViewModel.To = msg.ToRoom.Name;
            messageViewModel.Avatar = msg.FromUser.Avatar;
            messageViewModel.Timestamp = new DateTime(long.Parse(msg.Timestamp)).ToLongTimeString();

            return messageViewModel;
        }

        public static RoomViewModel RoomToRoomViewModel(Room room)
        {
            RoomViewModel roomViewModel = new RoomViewModel();
            roomViewModel.Id = room.Id;
            roomViewModel.Name = room.Name;

            return roomViewModel;
        }

        public static UserViewModel ApplicationUserToUserViewModel(ApplicationUser user)
        {
            UserViewModel userViewModel = new UserViewModel();
            userViewModel.Username = user.UserName;
            //userViewModel.Device = GetDevice();
            userViewModel.CurrentRoom = "";
            userViewModel.Avatar = user.Avatar;
            userViewModel.DisplayName = user.DisplayName;

            return userViewModel;
        }
    }
}
