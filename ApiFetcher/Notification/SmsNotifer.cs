using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

public class SmsService
{
    private readonly string _apiUrl;
    private readonly string _user;
    private readonly string _pass;
    private readonly string _sid;
    private readonly string _primaryNumber;
    private readonly List<string> _bulkNumbers;

    // To track sent SMS messages (Phone number => List of sent messages with timestamp)
    private readonly Dictionary<string, List<(string message, DateTime timestamp)>> _messageHistory;

    public SmsService()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        _apiUrl = config["SmsSettings:ApiUrl"];
        _user = config["SmsSettings:User"];
        _pass = config["SmsSettings:Pass"];
        _sid = config["SmsSettings:Sid"];
        _primaryNumber = config["SmsSettings:PrimaryNumber"];

        _bulkNumbers = new List<string>
        {
            config["SmsSettings:BulkNumber1"],
            config["SmsSettings:BulkNumber2"],
            config["SmsSettings:BulkNumber3"],
            config["SmsSettings:BulkNumber4"],
            config["SmsSettings:BulkNumber5"]
        };

        _messageHistory = new Dictionary<string, List<(string message, DateTime timestamp)>>();
    }

    public async Task<string> SendSmsAsync(string message)
    {
        try
        {
            if (string.IsNullOrEmpty(_primaryNumber))
            {
                Console.WriteLine("Primary number is not available. SMS not sent.");
                return "Primary number is missing.";
            }

            await SendSmsToPhoneNumber(_primaryNumber, message);

            return "Messages sent successfully.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }


    public async Task<string> SendBulkSmsAsync(string message)
    {
        try
        {
            var validNumbers = _bulkNumbers
           .Where(phone => !string.IsNullOrEmpty(phone) && phone.Trim().All(char.IsDigit))
           .Select(phone => phone.Trim())
           .ToList();


            if (!validNumbers.Any())
            {
                Console.WriteLine("No valid bulk numbers available. Bulk SMS not sent.");
                return "No valid bulk numbers.";
            }

            // Send SMS to all bulk numbers
            foreach (var phoneNumber in _bulkNumbers)
            {
                await SendSmsToPhoneNumber(phoneNumber, message);
            }

            return "Bulk messages sent successfully.";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return null;
        }
    }

    private async Task SendSmsToPhoneNumber(string phoneNumber, string message)
    {
        // Ensure the phone number starts with the country code "88"
        if (!phoneNumber.StartsWith("88"))
        {
            phoneNumber = "88" + phoneNumber;
        }

        // Check if message is being sent too frequently or too many times
        if (IsMessageRateLimited(phoneNumber, message))
        {
            Console.WriteLine($"Message to {phoneNumber} is rate-limited.");
            return;
        }

        // Add the message to the history for the phone number
        AddToMessageHistory(phoneNumber, message);

        // Build query parameters
        var queryParams = new Dictionary<string, string>
        {
            { "user", _user },
            { "pass", _pass },
            { "sid", _sid },
            { "sms", message },
            { "msisdn", phoneNumber },
            { "csmsid", Guid.NewGuid().ToString() } // Generate a unique ID for tracking
        };

        // Prepare HTTP client
        using (var client = new HttpClient())
        {
            var requestUri = $"{_apiUrl}?{new FormUrlEncodedContent(queryParams).ReadAsStringAsync().Result}";
            var response = await client.GetAsync(requestUri);

            // Check response
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"HTTP Error: {response.StatusCode}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {responseContent}");
        }
    }

    private bool IsMessageRateLimited(string phoneNumber, string message)
    {
        // Check if the phone number has sent more than 2 messages in the last hour
        if (_messageHistory.ContainsKey(phoneNumber))
        {
            var sentMessages = _messageHistory[phoneNumber];
            var oneHourAgo = DateTime.Now.AddHours(-1);

            // Remove messages older than 1 hour
            sentMessages.RemoveAll(msg => msg.timestamp < oneHourAgo);

            // Check if there are more than 2 messages sent in the last hour
            if (sentMessages.Count >= 2)
            {
                return true;
            }

            // Check if the same message was sent within the last 5 minutes
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);
            if (sentMessages.Exists(msg => msg.message == message && msg.timestamp >= fiveMinutesAgo))
            {
                return true;
            }
        }

        return false;
    }

    private void AddToMessageHistory(string phoneNumber, string message)
    {
        // If this is the first message for the phone number, create a new list
        if (!_messageHistory.ContainsKey(phoneNumber))
        {
            _messageHistory[phoneNumber] = new List<(string message, DateTime timestamp)>();
        }

        // Add the new message to the history
        _messageHistory[phoneNumber].Add((message, DateTime.Now));
    }
}
