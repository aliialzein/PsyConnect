// Services/ChatbotService.cs
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace PsyConnect.Services
{
    public interface IChatbotService
    {
        Task<string> GetReplyAsync(string userMessage);
    }

    public class ChatbotService : IChatbotService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChatbotService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }
        private static readonly string[] CrisisKeywords =
        {
            "suicide",
            "kill myself",
            "killing myself",
            "end my life",
            "want to die",
            "i want to die",
            "self-harm",
            "self harm",
            "cut myself",
            "hurt myself",
            "overdose",
            "ending it all"
        };

        public async Task<string> GetReplyAsync(string userMessage)
        {
            var lower = userMessage.ToLowerInvariant();

            if (CrisisKeywords.Any(k => lower.Contains(k)))
            {
                return
                    "It sounds like you might be going through something very serious. " +
                    "I’m not able to help with emergencies, crisis situations, or self-harm. " +
                    "Please contact your local emergency services, a trusted person, or a mental health hotline in your country immediately. " +
                    "If you are in immediate danger, call your local emergency number right now.\n\n" +
                    "This is general information, not a diagnosis or emergency service.";
            }
            // 0) Read config
            var apiKey = _config["AI:ApiKey"];
            var model = _config["AI:Model"] ?? "gpt-4.1-mini";

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("OpenAI API key is not configured (AI:ApiKey).");

            // 1) Build system prompt for SAFE psychotherapy assistant
            var systemPrompt =
                "You are PsyConnect Assistant, a virtual assistant that provides only general, " +
                "educational information about psychotherapy, mental health concepts, and how to use the PsyConnect platform. " +
                "You are not a therapist, you cannot diagnose, cannot prescribe medication, and cannot handle emergencies or crises. " +
                "If the user mentions self-harm, suicide, or urgent danger, you MUST tell them to immediately contact local emergency services " +
                "or a trusted adult/professional and you must not give instructions. " +
                "Always end your answer with this sentence: " +
                "\"This is general information, not a diagnosis or emergency service.\"";

            // 2) Build request body for chat completions
            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userMessage }
                },
                max_tokens = 500,
                temperature = 0.4
            };

            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.openai.com/v1/chat/completions"
            );

            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    // Log errorText if you have logging
                    return "Sorry, I couldn't process your request right now. Please try again later.";
                }

                var responseJson = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(responseJson);
                // Path: choices[0].message.content
                var root = doc.RootElement;

                var choices = root.GetProperty("choices");
                if (choices.GetArrayLength() == 0)
                {
                    return "Sorry, I couldn't find an answer.";
                }

                var message = choices[0].GetProperty("message");
                var content = message.GetProperty("content").GetString();

                return content ?? "Sorry, I couldn't find an answer.";
            }
            catch (Exception)
            {
                // You can log ex here
                return "Sorry, something went wrong while contacting the assistant.";
            }
        }
    }
}