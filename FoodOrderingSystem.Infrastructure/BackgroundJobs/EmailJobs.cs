using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FoodOrderingSystem.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FoodOrderingSystem.Infrastructure.BackgroundJobs
{
    public class EmailJobs
    {
        private readonly ILogger<EmailJobs> _logger;

        public EmailJobs(ILogger<EmailJobs> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends an email asynchronously as a background job
        /// </summary>
        public async Task SendEmailAsync(
            string email, 
            string subject, 
            string htmlBody, 
            string smtpServer, 
            int smtpPort, 
            string senderAddress, 
            string password, 
            string username)
        {
            try
            {
                if (string.IsNullOrEmpty(smtpServer))
                {
                    _logger.LogWarning("Email settings are not configured. Skipping email notification.");
                    return;
                }

                _logger.LogInformation("[Background Job] Sending email to {Email} with subject '{Subject}'", email, subject);

                var mail = new MimeMessage
                {
                    Sender = MailboxAddress.Parse(senderAddress),
                    Subject = subject,
                };

                mail.To.Add(MailboxAddress.Parse(email));

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };
                
                mail.Body = builder.ToMessageBody();
                mail.From.Add(new MailboxAddress(username, senderAddress));

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderAddress, password);
                await smtp.SendAsync(mail);

                smtp.Disconnect(true);
                
                _logger.LogInformation("[Background Job] Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Background Job] Error sending email to {Email}", email);
                // Re-throw the exception to let Hangfire know the job failed
                throw;
            }
        }

        /// <summary>
        /// Sends an email with attachments asynchronously as a background job
        /// </summary>
        public async Task SendEmailWithAttachmentsAsync(
            string email, 
            string subject, 
            string htmlBody, 
            List<EmailAttachment> attachments,
            string smtpServer, 
            int smtpPort, 
            string senderAddress, 
            string password, 
            string username)
        {
            try
            {
                if (string.IsNullOrEmpty(smtpServer))
                {
                    _logger.LogWarning("Email settings are not configured. Skipping email notification.");
                    return;
                }

                _logger.LogInformation("[Background Job] Sending email with attachments to {Email}", email);

                var mail = new MimeMessage
                {
                    Sender = MailboxAddress.Parse(senderAddress),
                    Subject = subject,
                };

                mail.To.Add(MailboxAddress.Parse(email));

                var builder = new BodyBuilder
                {
                    HtmlBody = htmlBody
                };

                // Add attachments if any
                if (attachments != null)
                {
                    foreach (var attachment in attachments)
                    {
                        builder.Attachments.Add(attachment.FileName, attachment.FileBytes, ContentType.Parse(attachment.ContentType));
                    }
                }

                mail.Body = builder.ToMessageBody();
                mail.From.Add(new MailboxAddress(username, senderAddress));

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderAddress, password);
                await smtp.SendAsync(mail);

                smtp.Disconnect(true);
                
                _logger.LogInformation("[Background Job] Email with attachments sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Background Job] Error sending email with attachments to {Email}", email);
                // Re-throw the exception to let Hangfire know the job failed
                throw;
            }
        }
    }

    /// <summary>
    /// Simple class to store attachment information
    /// </summary>
    public class EmailAttachment
    {
        public string FileName { get; set; }
        public byte[] FileBytes { get; set; }
        public string ContentType { get; set; }
    }
} 