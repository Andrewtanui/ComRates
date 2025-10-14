# AI Chatbot - Quick Start Guide

## 🚀 Getting Started in 3 Steps

### Step 1: Run the Application
```bash
dotnet run
```
The database migration has already been applied, so you're ready to go!

### Step 2: Test the Chatbot (As User)
1. **Login** to the application as any user
2. **Look for the green ⭐ button** in the bottom-right corner
3. **Click it** to open the AI assistant
4. **Try these example queries**:
   - "Show me products"
   - "Track my orders"
   - "How do I return an item?"
   - "What payment methods do you accept?"
   - "Help"

### Step 3: Access Admin Dashboard (As Admin)
1. **Login as SystemAdmin**
2. **Navigate to**: `/ChatbotAdmin/Dashboard`
3. **Explore**:
   - View conversation statistics
   - Monitor top intents
   - Check low-confidence predictions
   - Add training data at `/ChatbotAdmin/TrainingData`

---

## 💡 Quick Tips

### For Users
- **Quick Actions**: Use the emoji buttons for instant queries
- **Navigation Links**: Click the blue buttons in bot responses to navigate
- **History**: View your past conversations at `/Messages/ChatHistory`
- **Clear Chat**: Click the trash icon to clear the conversation

### For Admins
- **Monitor Daily**: Check the dashboard for usage trends
- **Improve Accuracy**: Add training data for queries with low confidence (<50%)
- **Export Data**: Download CSV reports for detailed analysis
- **Retrain Model**: Click "Retrain Model" after adding multiple training samples

---

## 🎯 Example Conversations

### Finding Products
**User**: "Show me products"  
**Bot**: "I can help you browse our products! Check out our full catalog or search for specific items."  
**Links**: [All Products] [Categories]

### Tracking Orders
**User**: "Where is my order?"  
**Bot**: "Track your order by going to My Orders and clicking on the specific order. You'll see real-time status updates."  
**Links**: [My Orders]

### Shipping Questions
**User**: "How long does shipping take?"  
**Bot**: "Delivery typically takes 2-5 business days. You'll receive tracking information once your order ships."  
**Links**: [Track Orders]

### Payment Help
**User**: "Can I pay with mpesa?"  
**Bot**: "To pay with M-Pesa, select it as your payment method at checkout and follow the prompts on your phone."  
**Links**: [Go to Checkout]

---

## 🔧 Admin Tasks

### Adding Training Data
1. Go to `/ChatbotAdmin/TrainingData`
2. Click "Add Training Data"
3. Enter:
   - **Text**: "where is my package"
   - **Intent**: "track_order"
4. Click "Add & Retrain"
5. Model automatically updates!

### Monitoring Performance
1. Check **Average Confidence** - should be >70%
2. Review **Low Confidence Conversations** - add training for these
3. Monitor **Top Intents** - understand what users ask most
4. Export data monthly for trend analysis

---

## 📱 Widget Features

### Buttons & Controls
- **⭐ FAB Button**: Opens/closes the widget
- **🗑️ Clear**: Clears conversation history
- **✖️ Close**: Closes the widget
- **Quick Actions**: 🛍️ Products, 📦 Orders, 🚚 Shipping, ↩️ Returns, ❓ Help

### Keyboard Shortcuts
- **Enter**: Send message
- **Escape**: Close widget (future enhancement)

---

## 📊 What to Monitor

### Key Metrics
1. **Total Conversations**: Overall usage
2. **Daily Conversations**: Engagement trends
3. **Average Confidence**: Model accuracy (target: >70%)
4. **Top Intents**: Most common user needs

### Red Flags
- ⚠️ Average confidence <50% → Add more training data
- ⚠️ Many "unknown" intents → Users asking about unsupported topics
- ⚠️ Low daily conversations → Widget visibility issue or user awareness

---

## 🎨 Customization

### Change Welcome Message
Edit `Views/Shared/_AskAIWidget.cshtml` line 159:
```html
<div class="askai-bubble bot">
    👋 Your custom welcome message here!
</div>
```

### Add New Intent
1. Edit `Services/EnhancedChatbotService.cs`
2. Add response in `_intentToResponse` dictionary
3. Add links in `_intentToLinks` dictionary
4. Add training samples in `GetTrainingData()` method
5. Or use admin panel to add training data

### Modify Quick Actions
Edit `Views/Shared/_AskAIWidget.cshtml` lines 150-154:
```html
<button class="btn btn-outline-secondary btn-sm askai-quick-btn" data-prompt="Your query">
    🎯 Your Label
</button>
```

---

## 🐛 Common Issues & Fixes

### Widget Not Showing
**Problem**: Green button doesn't appear  
**Fix**: 
- Ensure user is logged in
- Check if page has `ViewData["HideAIWidget"] = true`
- Clear browser cache

### Bot Not Responding
**Problem**: No response after sending message  
**Fix**:
- Check browser console for errors
- Verify database connection
- Ensure `EnhancedChatbotService` is registered in `Program.cs`

### Low Accuracy
**Problem**: Bot gives wrong answers  
**Fix**:
- Go to `/ChatbotAdmin/Dashboard`
- Review "Low Confidence Conversations"
- Add training data for those queries
- Click "Retrain Model"

### Database Errors
**Problem**: Error accessing conversation history  
**Fix**:
```bash
dotnet ef database update
```

---

## 📚 Learn More

- **Full Documentation**: See `CHATBOT_SETUP.md`
- **Implementation Details**: See `CHATBOT_IMPLEMENTATION_SUMMARY.md`
- **Admin Dashboard**: `/ChatbotAdmin/Dashboard`
- **User History**: `/Messages/ChatHistory`

---

## ✅ Quick Test Checklist

Before going live, test these:

- [ ] Widget opens and closes
- [ ] Quick action buttons work
- [ ] Bot responds to "hello"
- [ ] Bot responds to "track order"
- [ ] Navigation links are clickable
- [ ] Conversation saves in history
- [ ] Admin dashboard loads
- [ ] Can add training data
- [ ] Export to CSV works
- [ ] Mobile view works

---

## 🎉 You're All Set!

The AI Chatbot is now fully functional and ready to help your users navigate the ComRates system!

**Need Help?**
- Check the documentation files
- Review the admin dashboard
- Contact the development team

---

**Quick Links**:
- User Widget: Click ⭐ button (bottom-right)
- User History: `/Messages/ChatHistory`
- Admin Dashboard: `/ChatbotAdmin/Dashboard`
- Training Data: `/ChatbotAdmin/TrainingData`
- All Conversations: `/ChatbotAdmin/AllConversations`
