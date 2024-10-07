using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ChoresApp.Models.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChoresApp.Integrations
{
    public class SmtpEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendInvitationEmail(InvitationDto invitation)
        {
            _logger.LogDebug("Starting to send invitation email to {InviteeEmail}", invitation.InviteeEmail);

            var smtpClient = new SmtpClient(_configuration["SmtpServer"])
            {
                Port = int.Parse(_configuration["SmtpPort"]),
                Credentials = new NetworkCredential(_configuration["SmtpUsername"], _configuration["SmtpPassword"]),
                EnableSsl = bool.Parse(_configuration["SmtpEnableSsl"]),
            };

            _logger.LogDebug("SMTP client configured with host: {SmtpHost}, port: {SmtpPort}, SSL: {EnableSsl}", 
                smtpClient.Host, smtpClient.Port, smtpClient.EnableSsl);

            var mailMessage = new MailMessage
            {
                From = new MailAddress("Chore@forstberg.com", "Chores"),
                Subject = "You are invited to join a family in the Chores app",
                Body = $@"Hi there! You've been invited to join a family in the Chores app.

Please click the link below to sign up and start managing your chores and earning some extra cash!

[Sign up now](https://chores.forstberg.com/signup)

If you have any questions, please contact {invitation.InviterName}.

Best regards,
The Chores team",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(invitation.InviteeEmail);

            _logger.LogDebug("Mail message created with subject: {Subject}", mailMessage.Subject);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Invitation email sent successfully to {InviteeEmail}", invitation.InviteeEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send invitation email to {InviteeEmail}", invitation.InviteeEmail);
                throw;
            }
        }
    }
}