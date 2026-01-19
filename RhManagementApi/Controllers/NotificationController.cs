using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using RhManagementApi.Constants;
using RhManagementApi.Data;
using RhManagementApi.DTOs;
using RhManagementApi.Models;

namespace RhManagementApi.Controllers
{
    public partial class NotificationController : ControllerBase
    {
        private readonly AdventureWorksContext db;
        private readonly IMapper mapper;

        public NotificationController(AdventureWorksContext db, IMapper mapper)
        {
            this.db = db;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var notifications = await db.Notifications.ToListAsync();
            return Ok(notifications);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.NotificationID == id);
            if (notification == null) return NotFound();

            var notificationDTO = mapper.Map<NotificationDTO>(notification);
            return Ok(notificationDTO);
        }

        [HttpGet("recipient/{employeeId}")]
        public async Task<IActionResult> GetByRecipient(int employeeId)
        {
            var notifications = await db.Notifications.Where(n => n.RecipientID == employeeId).ToListAsync();
            return Ok(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> Create(NotificationDTO notificationDTO)
        {
            var notification = mapper.Map<Notification>(notificationDTO);

            notification.CreatedAtUtc = DateTime.UtcNow;
            notification.IsRead = false;

            db.Notifications.Add(notification);
            await db.SaveChangesAsync();

            var readNotificationDTO = mapper.Map<NotificationDTO>(notification);
            return CreatedAtAction(nameof(Get), new { id = notification.NotificationID }, readNotificationDTO);
        }

        [HttpPatch("{id}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.NotificationID == id);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            notification.ReadAtUtc = DateTime.UtcNow;

            db.Notifications.Update(notification);
            await db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var notification = await db.Notifications.FirstOrDefaultAsync(n => n.NotificationID == id);
            if (notification == null) return NotFound();

            db.Notifications.Remove(notification);
            await db.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("recipient/{employeeId}/read")]
        public async Task<IActionResult> DeleteAllReadNotifications(int employeeId)
        {
            var readNotifications = await db.Notifications
                 .Where(n => n.RecipientID == employeeId && n.IsRead)
                 .ToListAsync();

            if (readNotifications.Count == 0) return NoContent();

            db.Notifications.RemoveRange(readNotifications);
            await db.SaveChangesAsync();

            return NoContent();
        }
    }
}
