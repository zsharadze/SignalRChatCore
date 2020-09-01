using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SignalRChatCore.Data;
using SignalRChatCore.Helpers;
using SignalRChatCore.Hubs;
using SignalRChatCore.Models;
using SignalRChatCore.Models.ViewModels;

namespace SignalRChatCore.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private IWebHostEnvironment _IWebHostEnvironment;
        private ApplicationDbContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public HomeController(ILogger<HomeController> logger,
            IWebHostEnvironment IWebHostEnvironment,
            ApplicationDbContext context,
            IHubContext<ChatHub> hubContext)
        {
            _logger = logger;
            _IWebHostEnvironment = IWebHostEnvironment;
            _context = context;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if(file==null)
                return Json("No files selected");

            try
            {
                // Some basic checks...
                if (file != null && !FileValidator.ValidSize(file.Length))
                    return Json("File size too big. Maximum File Size: 500KB");
                else if (FileValidator.ValidType(file.ContentType))
                    return Json("This file extension is not allowed!");
                else
                {
                    // Save file to Disk
                    var fileName = DateTime.Now.ToString("yyyymmddMMss") + "_" + Path.GetFileName(file.FileName);
                    var filePath = Path.Combine(_IWebHostEnvironment.WebRootPath, "uploads", fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    string htmlImage = string.Format(
                        "<a href=\"/uploads/{0}\" target=\"_blank\">" +
                        "<img src=\"/uploads/{0}\" class=\"post-image\">" +
                        "</a>", fileName);

                    // Get sender & chat room
                    var senderViewModel = ChatHub._Connections.Where(u => u.Username == User.Identity.Name).FirstOrDefault();
                    var sender = _context.Users.Where(u => u.UserName == senderViewModel.Username).FirstOrDefault();
                    var room = _context.Rooms.Where(r => r.Name == senderViewModel.CurrentRoom).FirstOrDefault();

                    // Build message
                    Message msg = new Message()
                    {
                        Content = Regex.Replace(htmlImage, @"(?i)<(?!img|a|/a|/img).*?>", String.Empty),
                        Timestamp = DateTime.Now.Ticks.ToString(),
                        FromUser = sender,
                        ToRoom = room
                    };

                    _context.Messages.Add(msg);
                    _context.SaveChanges();

                    // Send image-message to group
                    MessageViewModel messageViewModel = Mapper.MessageToMessageViewModel(msg);

                    await _hubContext.Clients.Group(senderViewModel.CurrentRoom).SendAsync("newMessage", messageViewModel);
                    return Json("Success");
                }

            }
            catch (Exception ex)
            {
                return Json("Error while uploading" + ex.Message);
            }

            

        } // Upload

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
