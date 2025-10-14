using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TanuiApp.Data;
using TanuiApp.Models;
using Microsoft.EntityFrameworkCore;

namespace TanuiApp.Services
{
    public class EnhancedChatbotService : IChatbotService
    {
        private readonly MLContext _mlContext;
        private PredictionEngine<IntentInput, IntentPrediction> _predictor;
        private Dictionary<string, string> _intentToResponse;
        private Dictionary<string, List<(string text, string url)>> _intentToLinks;
        private readonly AppDbContext _context;
        private ITransformer? _model;

        public EnhancedChatbotService(AppDbContext context)
        {
            _context = context;
            _mlContext = new MLContext(seed: 1);
            _predictor = null!; // Will be initialized in TrainModel
            _intentToResponse = new Dictionary<string, string>();
            _intentToLinks = new Dictionary<string, List<(string text, string url)>>();
            
            InitializeResponseMappings();
            TrainModel();
        }

        private void InitializeResponseMappings()
        {
            _intentToResponse = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                // Greetings
                ["greeting"] = "Hey there! 😊 I'm your ComRates assistant. What can I help you find today?",
                
                // Navigation - Products
                ["browse_products"] = "I can help you browse our products! Check out our full catalog or search for specific items.",
                ["search_products"] = "Looking for something specific? Use the search bar at the top or browse by category.",
                ["view_categories"] = "We have various categories to explore. Click on Categories in the menu to see all available options.",
                ["product_details"] = "To view product details, click on any product card. You'll see descriptions, prices, reviews, and seller information.",
                
                // Shopping & Cart
                ["add_to_cart"] = "To add items to your cart, click the 'Add to Cart' button on any product page. Your cart icon will update with the count.",
                ["view_cart"] = "You can view your cart anytime by clicking the cart icon in the navigation bar.",
                ["checkout"] = "Ready to checkout? Go to your cart and click 'Proceed to Checkout' to complete your purchase.",
                ["wishlist"] = "Save items for later by adding them to your wishlist. Click the heart icon on any product.",
                
                // Orders & Tracking
                ["my_orders"] = "To view your orders, go to your Dashboard and click on 'My Orders'. You can track status and view details there.",
                ["track_order"] = "Track your order by going to My Orders and clicking on the specific order. You'll see real-time status updates.",
                ["order_status"] = "Order statuses include: Pending, Processing, Shipped, Delivered, and Cancelled. Check My Orders for current status.",
                
                // Shipping & Delivery
                ["shipping"] = "Most orders ship within 1-2 business days and arrive within 2-5 business days depending on your location.",
                ["delivery_time"] = "Delivery typically takes 2-5 business days. You'll receive tracking information once your order ships.",
                ["shipping_cost"] = "Shipping costs are calculated at checkout based on your location and order size.",
                
                // Returns & Refunds
                ["returns"] = "Returns are easy! You can initiate a return within 14 days of delivery from your My Orders page.",
                ["refund"] = "Refunds are processed within 5-7 business days after we receive your returned item.",
                ["cancel_order"] = "You can cancel orders that haven't shipped yet from your My Orders page. Click 'Cancel Order' on the order details.",
                
                // Payments
                ["payments"] = "We accept major credit/debit cards and M-Pesa. Choose your preferred payment method at checkout.",
                ["payment_methods"] = "Available payment methods: Visa, Mastercard, American Express, and M-Pesa mobile payments.",
                ["mpesa"] = "To pay with M-Pesa, select it as your payment method at checkout and follow the prompts on your phone.",
                
                // Account Management
                ["my_account"] = "Access your account by clicking on your profile icon in the top right corner.",
                ["profile_settings"] = "Update your profile, address, and preferences in Profile Settings from the account menu.",
                ["change_password"] = "Change your password in Profile Settings under the Security section.",
                ["notifications"] = "Manage your notification preferences in Profile Settings. You'll get updates on orders and messages.",
                
                // Seller Features
                ["become_seller"] = "Want to sell on ComRates? Contact our admin team to upgrade your account to a seller account.",
                ["list_product"] = "Sellers can list products by going to the Seller Dashboard and clicking 'Add New Product'.",
                ["manage_products"] = "Manage your products from the Seller Dashboard. You can edit, delete, or update inventory there.",
                ["seller_dashboard"] = "Access your Seller Dashboard from the main navigation menu to manage products, orders, and sales.",
                
                // Messages & Support
                ["messages"] = "Send and receive messages by clicking the chat icon in the navigation bar.",
                ["contact_seller"] = "Contact a seller by clicking 'Message Seller' on any product page.",
                ["customer_support"] = "Need help? Use this AI assistant or send a message to our support team through the Messages page.",
                ["help"] = "I'm here to help! Ask me about products, orders, shipping, payments, or navigating the site.",
                
                // Reviews & Ratings
                ["leave_review"] = "Leave a review on products you've purchased. Go to My Orders, find the product, and click 'Write Review'.",
                ["view_reviews"] = "See what others think! Product reviews are displayed on each product detail page.",
                
                // Pricing & Deals
                ["pricing"] = "Prices are shown on each product page. Look for special deals and discounts highlighted on the homepage.",
                ["deals"] = "Check out our latest deals and promotions on the homepage. We regularly update special offers!",
                ["compare_prices"] = "Compare prices by browsing similar products in the same category.",
                
                // Technical Issues
                ["login_issue"] = "Having trouble logging in? Try resetting your password or clearing your browser cache. Contact support if issues persist.",
                ["payment_failed"] = "If your payment fails, verify your payment details and try again. Contact your bank if the issue continues.",
                ["page_error"] = "Experiencing errors? Try refreshing the page or clearing your browser cache. Report persistent issues to support.",
                
                // General
                ["how_it_works"] = "ComRates is a marketplace where buyers and sellers connect. Browse products, add to cart, checkout, and track your orders!",
                ["about"] = "ComRates is your trusted online marketplace for quality products. Learn more on our About page.",
                ["safety"] = "We prioritize your safety. All transactions are secure, and we verify our sellers. Report any suspicious activity.",
                
                // Fallback
                ["unknown"] = "I'm not sure about that yet. Could you rephrase your question? Or try asking about products, orders, shipping, or account settings."
            };

            _intentToLinks = new Dictionary<string, List<(string, string)>>(StringComparer.OrdinalIgnoreCase)
            {
                ["greeting"] = new() { ("Browse Products", "/Products/Index"), ("View Profile", "/Profile/Settings"), ("My Orders", "/Cart/MyOrders") },
                ["browse_products"] = new() { ("All Products", "/Products/Index"), ("Categories", "/Products/Index") },
                ["search_products"] = new() { ("Search Products", "/Products/Search"), ("Browse All", "/Products/Index") },
                ["view_categories"] = new() { ("Browse Products", "/Products/Index") },
                ["add_to_cart"] = new() { ("View Products", "/Products/Index"), ("My Cart", "/Cart/MyCart") },
                ["view_cart"] = new() { ("Go to Cart", "/Cart/MyCart") },
                ["checkout"] = new() { ("View Cart", "/Cart/MyCart"), ("Checkout", "/Cart/Checkout") },
                ["wishlist"] = new() { ("My Wishlist", "/Cart/Wishlist"), ("Browse Products", "/Products/Index") },
                ["my_orders"] = new() { ("View Orders", "/Cart/MyOrders") },
                ["track_order"] = new() { ("My Orders", "/Cart/MyOrders") },
                ["order_status"] = new() { ("Check Orders", "/Cart/MyOrders") },
                ["shipping"] = new() { ("View Cart", "/Cart/MyCart"), ("My Orders", "/Cart/MyOrders") },
                ["delivery_time"] = new() { ("Track Orders", "/Cart/MyOrders") },
                ["returns"] = new() { ("My Orders", "/Cart/MyOrders") },
                ["refund"] = new() { ("My Orders", "/Cart/MyOrders"), ("Contact Support", "/Messages/ContactSupport") },
                ["cancel_order"] = new() { ("My Orders", "/Cart/MyOrders") },
                ["payments"] = new() { ("Checkout", "/Cart/Checkout") },
                ["payment_methods"] = new() { ("Go to Checkout", "/Cart/Checkout") },
                ["my_account"] = new() { ("Profile Settings", "/Profile/Settings") },
                ["profile_settings"] = new() { ("Edit Profile", "/Profile/Settings") },
                ["notifications"] = new() { ("View Notifications", "/Notifications/Index"), ("Settings", "/Profile/Settings") },
                ["become_seller"] = new() { ("Contact Support", "/Messages/ContactSupport"), ("About Us", "/Home/About") },
                ["seller_dashboard"] = new() { ("Seller Dashboard", "/SellerDashboard/Index") },
                ["list_product"] = new() { ("Add Product", "/Products/Create") },
                ["manage_products"] = new() { ("Seller Dashboard", "/SellerDashboard/Index") },
                ["messages"] = new() { ("Open Messages", "/Messages/Chat") },
                ["contact_seller"] = new() { ("Messages", "/Messages/Chat") },
                ["customer_support"] = new() { ("Contact Support", "/Messages/ContactSupport") },
                ["help"] = new() { ("How It Works", "/Home/Index#how"), ("About", "/Home/About"), ("Contact Support", "/Messages/ContactSupport") },
                ["leave_review"] = new() { ("My Orders", "/Cart/MyOrders") },
                ["pricing"] = new() { ("Browse Deals", "/Home/Index#deals"), ("All Products", "/Products/Index") },
                ["deals"] = new() { ("View Deals", "/Home/Index#deals"), ("Browse Products", "/Products/Index") },
                ["how_it_works"] = new() { ("Learn More", "/Home/Index#how"), ("About Us", "/Home/About") },
                ["about"] = new() { ("About Page", "/Home/About") },
                ["unknown"] = new() { ("Browse Products", "/Products/Index"), ("Contact Support", "/Messages/ContactSupport"), ("Home", "/Home/Index") }
            };
        }

        private void TrainModel()
        {
            // Get training data from database or use default samples
            var trainingData = GetTrainingData();
            
            var data = _mlContext.Data.LoadFromEnumerable(trainingData);

            var pipeline = _mlContext.Transforms.Text.FeaturizeText("Features", nameof(IntentInput.Text))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(
                    labelColumnName: "Label",
                    featureColumnName: "Features",
                    maximumNumberOfIterations: 100))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            _model = pipeline.Fit(data);
            _predictor = _mlContext.Model.CreatePredictionEngine<IntentInput, IntentPrediction>(_model);
        }

        private List<IntentInput> GetTrainingData()
        {
            var samples = new List<IntentInput>
            {
                // Greetings
                new IntentInput { Text = "hi", Label = "greeting" },
                new IntentInput { Text = "hello", Label = "greeting" },
                new IntentInput { Text = "hey", Label = "greeting" },
                new IntentInput { Text = "good morning", Label = "greeting" },
                new IntentInput { Text = "good afternoon", Label = "greeting" },
                new IntentInput { Text = "how are you", Label = "greeting" },
                new IntentInput { Text = "whats up", Label = "greeting" },

                // Browse Products
                new IntentInput { Text = "show me products", Label = "browse_products" },
                new IntentInput { Text = "browse products", Label = "browse_products" },
                new IntentInput { Text = "view products", Label = "browse_products" },
                new IntentInput { Text = "what products do you have", Label = "browse_products" },
                new IntentInput { Text = "show catalog", Label = "browse_products" },
                new IntentInput { Text = "see all items", Label = "browse_products" },

                // Search
                new IntentInput { Text = "search", Label = "search_products" },
                new IntentInput { Text = "find product", Label = "search_products" },
                new IntentInput { Text = "looking for", Label = "search_products" },
                new IntentInput { Text = "where can I search", Label = "search_products" },
                new IntentInput { Text = "how to search", Label = "search_products" },

                // Categories
                new IntentInput { Text = "categories", Label = "view_categories" },
                new IntentInput { Text = "product categories", Label = "view_categories" },
                new IntentInput { Text = "browse by category", Label = "view_categories" },
                new IntentInput { Text = "what categories", Label = "view_categories" },

                // Cart
                new IntentInput { Text = "add to cart", Label = "add_to_cart" },
                new IntentInput { Text = "how to add to cart", Label = "add_to_cart" },
                new IntentInput { Text = "put in cart", Label = "add_to_cart" },
                new IntentInput { Text = "cart", Label = "view_cart" },
                new IntentInput { Text = "my cart", Label = "view_cart" },
                new IntentInput { Text = "shopping cart", Label = "view_cart" },
                new IntentInput { Text = "view cart", Label = "view_cart" },
                new IntentInput { Text = "whats in my cart", Label = "view_cart" },

                // Checkout
                new IntentInput { Text = "checkout", Label = "checkout" },
                new IntentInput { Text = "buy now", Label = "checkout" },
                new IntentInput { Text = "complete purchase", Label = "checkout" },
                new IntentInput { Text = "how to checkout", Label = "checkout" },
                new IntentInput { Text = "place order", Label = "checkout" },

                // Wishlist
                new IntentInput { Text = "wishlist", Label = "wishlist" },
                new IntentInput { Text = "save for later", Label = "wishlist" },
                new IntentInput { Text = "favorites", Label = "wishlist" },
                new IntentInput { Text = "add to wishlist", Label = "wishlist" },

                // Orders
                new IntentInput { Text = "my orders", Label = "my_orders" },
                new IntentInput { Text = "order history", Label = "my_orders" },
                new IntentInput { Text = "view orders", Label = "my_orders" },
                new IntentInput { Text = "past orders", Label = "my_orders" },
                new IntentInput { Text = "track order", Label = "track_order" },
                new IntentInput { Text = "where is my order", Label = "track_order" },
                new IntentInput { Text = "order tracking", Label = "track_order" },
                new IntentInput { Text = "order status", Label = "order_status" },
                new IntentInput { Text = "check order status", Label = "order_status" },

                // Shipping
                new IntentInput { Text = "shipping", Label = "shipping" },
                new IntentInput { Text = "delivery", Label = "shipping" },
                new IntentInput { Text = "shipping time", Label = "delivery_time" },
                new IntentInput { Text = "how long delivery", Label = "delivery_time" },
                new IntentInput { Text = "when will it arrive", Label = "delivery_time" },
                new IntentInput { Text = "delivery time", Label = "delivery_time" },
                new IntentInput { Text = "shipping cost", Label = "shipping_cost" },
                new IntentInput { Text = "delivery fee", Label = "shipping_cost" },

                // Returns
                new IntentInput { Text = "return", Label = "returns" },
                new IntentInput { Text = "return policy", Label = "returns" },
                new IntentInput { Text = "how to return", Label = "returns" },
                new IntentInput { Text = "send back", Label = "returns" },
                new IntentInput { Text = "refund", Label = "refund" },
                new IntentInput { Text = "get money back", Label = "refund" },
                new IntentInput { Text = "cancel order", Label = "cancel_order" },
                new IntentInput { Text = "cancel my order", Label = "cancel_order" },

                // Payments
                new IntentInput { Text = "payment", Label = "payments" },
                new IntentInput { Text = "payment methods", Label = "payment_methods" },
                new IntentInput { Text = "how to pay", Label = "payment_methods" },
                new IntentInput { Text = "pay with mpesa", Label = "mpesa" },
                new IntentInput { Text = "mpesa payment", Label = "mpesa" },
                new IntentInput { Text = "mobile money", Label = "mpesa" },

                // Account
                new IntentInput { Text = "my account", Label = "my_account" },
                new IntentInput { Text = "account settings", Label = "profile_settings" },
                new IntentInput { Text = "profile", Label = "profile_settings" },
                new IntentInput { Text = "edit profile", Label = "profile_settings" },
                new IntentInput { Text = "change password", Label = "change_password" },
                new IntentInput { Text = "update password", Label = "change_password" },
                new IntentInput { Text = "notifications", Label = "notifications" },
                new IntentInput { Text = "notification settings", Label = "notifications" },

                // Seller
                new IntentInput { Text = "become seller", Label = "become_seller" },
                new IntentInput { Text = "sell products", Label = "become_seller" },
                new IntentInput { Text = "start selling", Label = "become_seller" },
                new IntentInput { Text = "list product", Label = "list_product" },
                new IntentInput { Text = "add product", Label = "list_product" },
                new IntentInput { Text = "seller dashboard", Label = "seller_dashboard" },
                new IntentInput { Text = "manage products", Label = "manage_products" },

                // Messages
                new IntentInput { Text = "messages", Label = "messages" },
                new IntentInput { Text = "inbox", Label = "messages" },
                new IntentInput { Text = "chat", Label = "messages" },
                new IntentInput { Text = "contact seller", Label = "contact_seller" },
                new IntentInput { Text = "message seller", Label = "contact_seller" },
                new IntentInput { Text = "support", Label = "customer_support" },
                new IntentInput { Text = "customer support", Label = "customer_support" },
                new IntentInput { Text = "contact support", Label = "customer_support" },
                new IntentInput { Text = "help", Label = "help" },
                new IntentInput { Text = "need help", Label = "help" },

                // Reviews
                new IntentInput { Text = "write review", Label = "leave_review" },
                new IntentInput { Text = "leave review", Label = "leave_review" },
                new IntentInput { Text = "rate product", Label = "leave_review" },
                new IntentInput { Text = "reviews", Label = "view_reviews" },
                new IntentInput { Text = "product reviews", Label = "view_reviews" },

                // Pricing
                new IntentInput { Text = "price", Label = "pricing" },
                new IntentInput { Text = "pricing", Label = "pricing" },
                new IntentInput { Text = "cost", Label = "pricing" },
                new IntentInput { Text = "how much", Label = "pricing" },
                new IntentInput { Text = "deals", Label = "deals" },
                new IntentInput { Text = "discounts", Label = "deals" },
                new IntentInput { Text = "offers", Label = "deals" },
                new IntentInput { Text = "promotions", Label = "deals" },

                // Technical
                new IntentInput { Text = "cant login", Label = "login_issue" },
                new IntentInput { Text = "login problem", Label = "login_issue" },
                new IntentInput { Text = "payment failed", Label = "payment_failed" },
                new IntentInput { Text = "payment not working", Label = "payment_failed" },
                new IntentInput { Text = "error", Label = "page_error" },
                new IntentInput { Text = "page not loading", Label = "page_error" },

                // General
                new IntentInput { Text = "how it works", Label = "how_it_works" },
                new IntentInput { Text = "how does this work", Label = "how_it_works" },
                new IntentInput { Text = "about", Label = "about" },
                new IntentInput { Text = "about comrates", Label = "about" },
                new IntentInput { Text = "safety", Label = "safety" },
                new IntentInput { Text = "is it safe", Label = "safety" },
                new IntentInput { Text = "secure", Label = "safety" }
            };

            // Try to load additional training data from database
            try
            {
                var dbTrainingData = _context.ChatbotTrainingData
                    .Where(t => t.IsActive)
                    .Select(t => new IntentInput { Text = t.Text, Label = t.Intent })
                    .ToList();
                
                samples.AddRange(dbTrainingData);
            }
            catch
            {
                // Database might not be initialized yet, use default samples only
            }

            return samples;
        }

        public async Task<string> GetBotReplyAsync(string userMessage, string? userId = null)
        {
            var (text, _) = await GetBotReplyWithLinksAsync(userMessage, null, userId);
            return text;
        }

        public async Task<(string text, List<(string text, string url)> links)> GetBotReplyWithLinksAsync(
            string userMessage, 
            string? userName = null, 
            string? userId = null)
        {
            var reply = "";
            var links = new List<(string text, string url)>();

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                reply = "Tell me what you're looking for and I'll point you in the right direction! 😊";
                links = new List<(string text, string url)>
                {
                    ("Browse Products", "/Products/Index"),
                    ("My Orders", "/Cart/MyOrders"),
                    ("Contact Support", "/Messages/ContactSupport")
                };
                return (reply, links);
            }

            var prediction = _predictor.Predict(new IntentInput { Text = userMessage.ToLower() });
            var intent = prediction.PredictedLabel ?? "unknown";
            // Attempt to compute confidence if available; otherwise default to 0.75
            float confidence = 0.75f;
            try
            {
                // If the prediction engine exposes scores, take max as confidence
                var scoresProp = prediction.GetType().GetProperty("Score");
                if (scoresProp != null)
                {
                    var scores = scoresProp.GetValue(prediction) as float[];
                    if (scores != null && scores.Length > 0)
                    {
                        confidence = scores.Max();
                    }
                }
            }
            catch { /* fallback to default confidence */ }

            // Build base reply text
            if (_intentToResponse.TryGetValue(intent, out var baseReply))
            {
                reply = string.IsNullOrWhiteSpace(userName)
                    ? baseReply
                    : $"{userName.Split(' ').FirstOrDefault()}, {baseReply}".TrimStart(',', ' ');
            }
            else
            {
                reply = _intentToResponse["unknown"];
            }

            // Build links for this intent if any
            if (_intentToLinks.TryGetValue(intent, out var mappedLinks))
            {
                links = mappedLinks;
            }

            // Log conversation to database
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    var conversation = new ChatbotConversation
                    {
                        UserId = userId,
                        UserMessage = userMessage,
                        BotResponse = "", // Will be updated after we generate response
                        DetectedIntent = intent,
                        ConfidenceScore = confidence,
                        SessionId = Guid.NewGuid().ToString()
                    };

                    if (_intentToResponse.TryGetValue(intent, out var baseText))
                    {
                        reply = string.IsNullOrWhiteSpace(userName) 
                            ? baseText 
                            : $"{userName.Split(' ').FirstOrDefault()}, {baseText}".TrimStart(',', ' ');
                    }
                    else
                    {
                        reply = _intentToResponse["unknown"];
                    }

                    conversation.BotResponse = reply;
                    _context.ChatbotConversations.Add(conversation);
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Continue even if logging fails
                }
            }
            else
            {
                // No userId provided, just generate response
                if (_intentToResponse.TryGetValue(intent, out var baseText))
                {
                    reply = baseText;
                }
                else
                {
                    reply = _intentToResponse["unknown"];
                }
            }

            if (_intentToLinks.TryGetValue(intent, out var intentLinks))
            {
                links = intentLinks;
            }

            return (reply, links);
        }

        public async Task RetrainModelAsync()
        {
            await Task.Run(() => TrainModel());
        }

        public async Task<bool> AddTrainingDataAsync(string text, string intent, string? addedBy = null)
        {
            try
            {
                var trainingData = new ChatbotTrainingData
                {
                    Text = text.ToLower(),
                    Intent = intent.ToLower(),
                    AddedBy = addedBy,
                    IsActive = true
                };

                _context.ChatbotTrainingData.Add(trainingData);
                await _context.SaveChangesAsync();

                // Retrain model with new data
                await RetrainModelAsync();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<ChatbotConversation>> GetUserConversationHistoryAsync(string userId, int limit = 10)
        {
            return await _context.ChatbotConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> ProvideFeedbackAsync(int conversationId, bool wasHelpful, string? feedback = null)
        {
            try
            {
                var conversation = await _context.ChatbotConversations.FindAsync(conversationId);
                if (conversation == null) return false;

                conversation.WasHelpful = wasHelpful;
                conversation.UserFeedback = feedback;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
