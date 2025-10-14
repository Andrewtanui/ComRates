using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TanuiApp.Models;

namespace TanuiApp.Services
{
    public class ChatbotService : IChatbotService
    {
        private readonly MLContext _mlContext;
        private readonly PredictionEngine<IntentInput, IntentPrediction> _predictor;
        private readonly Dictionary<string, string> _intentToResponse;
        private readonly Dictionary<string, List<(string text, string url)>> _intentToLinks;

        public ChatbotService()
        {
            _mlContext = new MLContext(seed: 1);

            var samples = new List<IntentInput>
            {
                new IntentInput { Text = "hi" , Label = "greeting" },
                new IntentInput { Text = "hello" , Label = "greeting" },
                new IntentInput { Text = "hey" , Label = "greeting" },
                new IntentInput { Text = "how are you" , Label = "greeting" },
                new IntentInput { Text = "help" , Label = "help" },
                new IntentInput { Text = "support" , Label = "help" },
                new IntentInput { Text = "contact support" , Label = "help" },
                new IntentInput { Text = "shipping" , Label = "shipping" },
                new IntentInput { Text = "delivery time" , Label = "shipping" },
                new IntentInput { Text = "how long delivery" , Label = "shipping" },
                new IntentInput { Text = "return" , Label = "returns" },
                new IntentInput { Text = "refund" , Label = "returns" },
                new IntentInput { Text = "cancel order" , Label = "returns" },
                new IntentInput { Text = "payment methods" , Label = "payments" },
                new IntentInput { Text = "pay with mpesa" , Label = "payments" },
                new IntentInput { Text = "pricing" , Label = "pricing" },
                new IntentInput { Text = "cost" , Label = "pricing" },
                new IntentInput { Text = "how much" , Label = "pricing" }
            };

            var data = _mlContext.Data.LoadFromEnumerable(samples);

            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(IntentInput.Text))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            var model = pipeline.Fit(data);
            _predictor = _mlContext.Model.CreatePredictionEngine<IntentInput, IntentPrediction>(model);

            _intentToResponse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["greeting"] = "Hey there! 😊 I'm your ComRates assistant. What can I help you find today?",
                ["help"] = "I've got you! Ask me about finding products, shipping, returns, or managing your account.",
                ["shipping"] = "Great question! Most orders ship out in 1–2 days and arrive within 2–5 business days depending on your location.",
                ["returns"] = "No worries—returns are easy. You can start a return within 14 days of delivery from your orders page.",
                ["payments"] = "We currently support major cards and M-Pesa. You can pick your method at checkout.",
                ["pricing"] = "Prices are shown on each product page. You'll also spot discounts and deals on the home page."
            };

            _intentToLinks = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase)
            {
                ["greeting"] = new() { ("Browse products", "/Products/Index"), ("View my profile", "/Profile/Settings") },
                ["help"] = new() { ("How it works", "/Home/Index#how"), ("Contact messages", "/Messages/Chat") },
                ["shipping"] = new() { ("View cart", "/Cart/MyCart"), ("Track my orders", "/Cart/MyOrders") },
                ["returns"] = new() { ("My orders", "/Cart/MyOrders") },
                ["payments"] = new() { ("Go to checkout", "/Cart/Checkout") },
                ["pricing"] = new() { ("Browse deals", "/Home/Index#deals"), ("All products", "/Products/Index") }
            };
        }

        public Task<string> GetBotReplyAsync(string userMessage, string? userId = null)
        {
            if (string.IsNullOrWhiteSpace(userMessage))
            {
                return Task.FromResult("Please type something so I can help.");
            }

            var prediction = _predictor.Predict(new IntentInput { Text = userMessage });
            var intent = prediction.PredictedLabel ?? string.Empty;

            if (_intentToResponse.TryGetValue(intent, out var response))
            {
                return Task.FromResult(response);
            }

            return Task.FromResult("I'm not sure yet. Try asking about shipping, returns, or payments.");
        }

        public Task<(string text, List<(string text, string url)> links)> GetBotReplyWithLinksAsync(string userMessage, string? userName = null, string? userId = null)
        {
            var reply = "";
            var links = new List<(string text, string url)>();

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                reply = "Tell me a bit about what you're looking for and I'll point you in the right direction.";
                return Task.FromResult((reply, links));
            }

            var prediction = _predictor.Predict(new IntentInput { Text = userMessage });
            var intent = prediction.PredictedLabel ?? string.Empty;

            if (_intentToResponse.TryGetValue(intent, out var baseText))
            {
                reply = string.IsNullOrWhiteSpace(userName) ? baseText : $"{userName.Split(' ').FirstOrDefault() ?? ""}, {baseText}".TrimStart(',', ' ');
            }
            else
            {
                reply = "I want to get this right. Could you share a bit more detail? You can also browse these sections.";
            }

            if (_intentToLinks.TryGetValue(intent, out var intentLinks))
            {
                links = intentLinks;
            }

            return Task.FromResult((reply, links));
        }

        public Task RetrainModelAsync()
        {
            return Task.CompletedTask;
        }

        public Task<bool> AddTrainingDataAsync(string text, string intent, string? addedBy = null)
        {
            return Task.FromResult(false);
        }

        public Task<List<ChatbotConversation>> GetUserConversationHistoryAsync(string userId, int limit = 10)
        {
            return Task.FromResult(new List<ChatbotConversation>());
        }

        public Task<bool> ProvideFeedbackAsync(int conversationId, bool wasHelpful, string? feedback = null)
        {
            return Task.FromResult(false);
        }
    }

    public class IntentInput
    {
        public string Text { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class IntentPrediction
    {
        [ColumnName("PredictedLabel")] public string PredictedLabel { get; set; } = string.Empty;
        public float[] Score { get; set; } = Array.Empty<float>();
    }
}



