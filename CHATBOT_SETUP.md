# AI Chatbot Setup Guide

## Overview
The ComRates AI Chatbot is a fully functional ML-powered assistant that helps users navigate the system, find products, track orders, and get support. It uses ML.NET for intent classification and provides contextual responses with helpful navigation links.

## Features Implemented

### 1. **ML-Powered Intent Recognition**
- Trained on 100+ sample phrases covering 30+ intents
- Categories include: products, orders, shipping, payments, returns, account management, and more
- Confidence scoring for each prediction
- Automatic model retraining when new data is added

### 2. **Comprehensive Navigation Support**
The chatbot can help users with:
- **Product Discovery**: Browse products, search, view categories
- **Shopping**: Add to cart, checkout, wishlist management
- **Order Management**: Track orders, view order history, check status
- **Shipping & Delivery**: Shipping times, costs, tracking
- **Returns & Refunds**: Return policy, refund process, order cancellation
- **Payments**: Payment methods, M-Pesa integration
- **Account Settings**: Profile management, password changes, notifications
- **Seller Features**: Listing products, managing inventory
- **Support**: Contact support, help documentation

### 3. **Enhanced User Interface**
- Modern, animated chat widget with gradient design
- Quick action buttons for common queries
- Typing indicators for better UX
- Conversation history saved in localStorage
- Clickable navigation links in responses
- Mobile-responsive design

### 4. **Conversation Tracking**
- All conversations stored in database
- User conversation history view
- Confidence scores tracked
- Feedback mechanism (ready for implementation)

### 5. **Admin Dashboard**
- Analytics dashboard with key metrics
- View all conversations
- Monitor low-confidence predictions
- Track top intents
- Export conversations to CSV
- Training data management
- Manual model retraining

## Database Schema

### New Tables Created:

#### ChatbotConversations
```sql
- Id (int, PK)
- UserId (string, FK to Users)
- UserMessage (string)
- BotResponse (string)
- DetectedIntent (string)
- ConfidenceScore (decimal)
- WasHelpful (bool)
- UserFeedback (string, nullable)
- CreatedAt (datetime)
- SessionId (string, nullable)
```

#### ChatbotTrainingData
```sql
- Id (int, PK)
- Text (string)
- Intent (string)
- IsActive (bool)
- AddedBy (string, nullable)
- CreatedAt (datetime)
- UpdatedAt (datetime, nullable)
```

## Setup Instructions

### Step 1: Create Database Migration

Run the following commands in the Package Manager Console:

```powershell
Add-Migration AddChatbotTables
Update-Database
```

Or using .NET CLI:

```bash
dotnet ef migrations add AddChatbotTables
dotnet ef database update
```

### Step 2: Verify Service Registration

The `EnhancedChatbotService` is already registered in `Program.cs`:
```csharp
builder.Services.AddScoped<IChatbotService, EnhancedChatbotService>();
```

### Step 3: Access the Chatbot

**For Users:**
- Click the green AI button (⭐) in the bottom-right corner of any page
- Type questions or use quick action buttons
- View conversation history at `/Messages/ChatHistory`

**For Admins:**
- Access admin dashboard at `/ChatbotAdmin/Dashboard`
- Manage training data at `/ChatbotAdmin/TrainingData`
- View all conversations at `/ChatbotAdmin/AllConversations`
- Export data at `/ChatbotAdmin/ExportConversations`

## Training the Chatbot

### Adding Training Data

1. Go to `/ChatbotAdmin/TrainingData`
2. Click "Add Training Data"
3. Enter:
   - **Text**: User question/phrase (e.g., "where is my order")
   - **Intent**: Category (e.g., "track_order")
4. Click "Add & Retrain"
5. Model automatically retrains with new data

### Best Practices for Training

1. **Add Variations**: Include multiple ways users might ask the same question
   ```
   Text: "where is my package" → Intent: track_order
   Text: "track my shipment" → Intent: track_order
   Text: "order status" → Intent: track_order
   ```

2. **Use Consistent Intent Names**: Keep intent names lowercase with underscores
   - ✅ `track_order`, `view_cart`, `shipping_info`
   - ❌ `TrackOrder`, `View Cart`, `shipping-info`

3. **Monitor Low Confidence**: Check the dashboard for predictions with <50% confidence
   - These indicate areas where more training is needed

4. **Regular Updates**: Add new training data based on:
   - User questions that get poor responses
   - New features added to the system
   - Common support queries

## Available Intents

The chatbot currently supports these intents:

**Navigation & Products**
- `greeting`, `browse_products`, `search_products`, `view_categories`, `product_details`

**Shopping**
- `add_to_cart`, `view_cart`, `checkout`, `wishlist`

**Orders**
- `my_orders`, `track_order`, `order_status`

**Shipping & Delivery**
- `shipping`, `delivery_time`, `shipping_cost`

**Returns & Refunds**
- `returns`, `refund`, `cancel_order`

**Payments**
- `payments`, `payment_methods`, `mpesa`

**Account**
- `my_account`, `profile_settings`, `change_password`, `notifications`

**Seller**
- `become_seller`, `list_product`, `manage_products`, `seller_dashboard`

**Support**
- `messages`, `contact_seller`, `customer_support`, `help`

**Reviews & Pricing**
- `leave_review`, `view_reviews`, `pricing`, `deals`, `compare_prices`

**Technical**
- `login_issue`, `payment_failed`, `page_error`

**General**
- `how_it_works`, `about`, `safety`

## Customization

### Adding New Intents

1. **Update EnhancedChatbotService.cs**:
   - Add response in `_intentToResponse` dictionary
   - Add navigation links in `_intentToLinks` dictionary
   - Add training samples in `GetTrainingData()` method

2. **Add Training Data via Admin Panel**:
   - Go to Training Data page
   - Add multiple sample phrases for the new intent

### Modifying Responses

Edit the `_intentToResponse` dictionary in `EnhancedChatbotService.cs`:

```csharp
_intentToResponse = new Dictionary<string, string>
{
    ["your_intent"] = "Your custom response here with helpful information.",
    // ... other intents
};
```

### Adding Navigation Links

Edit the `_intentToLinks` dictionary:

```csharp
_intentToLinks = new Dictionary<string, List<(string, string)>>
{
    ["your_intent"] = new() { 
        ("Link Text", "/Controller/Action"),
        ("Another Link", "/Path/To/Page")
    },
    // ... other intents
};
```

## Monitoring & Analytics

### Key Metrics to Track

1. **Total Conversations**: Overall usage
2. **Daily Conversations**: Engagement trends
3. **Average Confidence**: Model accuracy
4. **Top Intents**: Most common user needs
5. **Low Confidence Predictions**: Areas needing improvement

### Improving Accuracy

1. **Review Low Confidence Conversations**:
   - Check predictions with <50% confidence
   - Add similar phrases to training data

2. **Monitor Top Intents**:
   - Ensure popular intents have many training samples
   - Add variations for frequently used intents

3. **User Feedback** (Future Enhancement):
   - Implement thumbs up/down on responses
   - Use feedback to identify poor predictions

## Troubleshooting

### Chatbot Not Responding
- Check if `EnhancedChatbotService` is registered in `Program.cs`
- Verify database tables exist
- Check browser console for JavaScript errors

### Low Confidence Scores
- Add more training data for the specific intent
- Ensure training samples are diverse
- Check for typos in training data

### Database Errors
- Run migrations: `Update-Database`
- Check connection string in `appsettings.json`
- Verify SQL Server is running

### Widget Not Appearing
- Check if `HideAIWidget` is set in ViewData
- Verify user is authenticated
- Check `_Layout.cshtml` includes the widget partial

## Future Enhancements

Potential improvements to consider:

1. **Sentiment Analysis**: Detect user frustration and escalate to human support
2. **Multi-language Support**: Translate responses based on user preference
3. **Voice Input**: Add speech-to-text capability
4. **Rich Media**: Send images, videos, or product cards in responses
5. **Proactive Suggestions**: Suggest actions based on user behavior
6. **Integration with Support Tickets**: Create tickets from chatbot conversations
7. **A/B Testing**: Test different response variations
8. **Context Awareness**: Remember previous messages in conversation
9. **Product Recommendations**: Suggest products based on queries
10. **Order Updates**: Proactively notify about order status changes

## API Endpoints

### User Endpoints
- `POST /Messages/AskBot` - Send message to chatbot
- `GET /Messages/ChatHistory` - View conversation history
- `POST /Messages/ProvideChatbotFeedback` - Submit feedback

### Admin Endpoints
- `GET /ChatbotAdmin/Dashboard` - Analytics dashboard
- `GET /ChatbotAdmin/TrainingData` - Manage training data
- `POST /ChatbotAdmin/AddTrainingData` - Add new training sample
- `POST /ChatbotAdmin/DeleteTrainingData` - Remove training sample
- `POST /ChatbotAdmin/RetrainModel` - Manually retrain model
- `GET /ChatbotAdmin/AllConversations` - View all conversations
- `GET /ChatbotAdmin/ExportConversations` - Export to CSV

## Security Considerations

1. **Admin Access**: Only SystemAdmin role can access admin endpoints
2. **CSRF Protection**: All POST requests require anti-forgery tokens
3. **Data Privacy**: User conversations are private and only visible to admins
4. **Input Validation**: User messages are sanitized before processing
5. **Rate Limiting**: Consider adding rate limiting to prevent abuse

## Performance Optimization

1. **Model Caching**: ML model is loaded once and reused
2. **Database Indexing**: Indexes on UserId, SessionId, CreatedAt
3. **Lazy Loading**: Conversations loaded on-demand
4. **Client-side Caching**: Recent conversations cached in localStorage
5. **Async Operations**: All database operations are asynchronous

## Support

For issues or questions:
1. Check this documentation
2. Review the admin dashboard for insights
3. Check low confidence conversations for problematic patterns
4. Contact the development team

---

**Version**: 1.0  
**Last Updated**: 2025-10-14  
**ML Framework**: ML.NET 4.0.2  
**Database**: SQL Server
