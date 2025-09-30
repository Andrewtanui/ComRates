using Microsoft.AspNetCore.Mvc;
using TanuiApp.ViewModels;
using Microsoft.ML; // for ML.NET
using System.Collections.Generic;

namespace TanuiApp.Controllers
{
    public class SupportController : Controller
    {
        private static readonly List<FAQ> Faqs = new List<FAQ>
        {
            new FAQ { Question = "How do I reset my password?", Answer = "Go to Account > Verify Email, then change your password." },
            new FAQ { Question = "How do I sell an item?", Answer = "Click on 'Post Ad' in the dashboard and fill in the details of your item." },
            new FAQ { Question = "Is my profile public?", Answer = "You can choose to make it public or private in Profile Settings." }
        };

        private readonly MLContext mlContext;
        private readonly PredictionEngine<UserQuestion, AnswerPrediction>? predictionEngine;

        public SupportController()
        {
            mlContext = new MLContext();

            // For now we’ll mock the ML part.
            // Later you can load a real trained model with: mlContext.Model.Load("model.zip", out var schema);
            predictionEngine = null;
        }

        public IActionResult Index()
        {
            var model = new SupportViewModel
            {
                Faqs = Faqs
            };
            return View(model);
        }

        [HttpPost]
        public IActionResult AskQuestion(SupportViewModel model)
        {
            string answer;

            if (!string.IsNullOrEmpty(model.UserQuestion))
            {
                // Placeholder AI response (later replace with ML.NET model prediction)
                answer = $"You asked: '{model.UserQuestion}'. Our AI suggests: Please check FAQs or contact support.";
            }
            else
            {
                answer = "Please enter a valid question.";
            }

            model.Answer = answer;
            model.Faqs = Faqs;
            return View("Index", model);
        }
    }

    // FAQ model
    public class FAQ
    {
        public string Question { get; set; }
        public string Answer { get; set; }
    }

    // ML.NET input
    public class UserQuestion
    {
        public string Question { get; set; }
    }

    // ML.NET output
    public class AnswerPrediction
    {
        public string PredictedAnswer { get; set; }
    }
}
