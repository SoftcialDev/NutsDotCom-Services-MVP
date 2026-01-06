# Postman Testing Guide

## API Endpoint

**URL:** `http://localhost:7071/api/complaints` (Local)  
**URL:** `https://<your-function-app>.azurewebsites.net/api/complaints` (Azure)

**Method:** `POST`

**Headers:**
```
Content-Type: application/json
```

## Request Body Format

```json
{
  "text": "Your complaint text here"
}
```

## Sample Complaints for Testing

### 1. Negative Sentiment - Poor Service
```json
{
  "text": "I am extremely disappointed with the service I received. The staff was rude and unhelpful. I waited over an hour for assistance and when I finally got help, they couldn't resolve my issue. The product I received was damaged and not as described. This is unacceptable and I demand a full refund immediately."
}
```

### 2. Negative Sentiment - Billing Issue
```json
{
  "text": "I have been charged incorrectly for the past three months. Despite multiple calls to customer service, no one has been able to fix this billing error. I am frustrated with the lack of resolution and the time I've wasted trying to get this sorted out. Please credit my account immediately."
}
```

### 3. Negative Sentiment - Delivery Problem
```json
{
  "text": "My order was supposed to arrive last week but it still hasn't shown up. The tracking information hasn't been updated in days. I've tried contacting support multiple times but keep getting automated responses. This is completely unacceptable for a paid service."
}
```

### 4. Positive Sentiment - Great Experience
```json
{
  "text": "I wanted to express my gratitude for the excellent service I received. The staff went above and beyond to help me with my request. The product quality exceeded my expectations and the delivery was prompt. I will definitely be using your services again and recommending you to friends."
}
```

### 5. Positive Sentiment - Problem Resolved
```json
{
  "text": "Thank you for quickly resolving the issue I reported last week. Your customer service team was professional, courteous, and efficient. The replacement product arrived quickly and works perfectly. I appreciate your commitment to customer satisfaction."
}
```

### 6. Neutral Sentiment - Information Request
```json
{
  "text": "I am writing to inquire about the return policy for items purchased online. Can you please provide information about the time frame for returns and any restocking fees that may apply? I would also like to know the process for initiating a return."
}
```

### 7. Neutral Sentiment - Product Question
```json
{
  "text": "I am interested in purchasing your product but have a few questions before making a decision. What are the dimensions of the item? Does it come with a warranty? What is the estimated shipping time to my location? Please provide this information at your earliest convenience."
}
```

### 8. Negative Sentiment - Technical Issue
```json
{
  "text": "The software I purchased is constantly crashing and losing my work. I've followed all the troubleshooting steps provided but the problem persists. This is causing significant disruption to my business operations. I need immediate technical support or a refund."
}
```

### 9. Negative Sentiment - Mixed Feelings
```json
{
  "text": "While I appreciate the quality of the product itself, I am very disappointed with the customer service experience. The product arrived late and when I called to inquire, I was put on hold for 45 minutes. The product works well but the overall experience was poor."
}
```

### 10. Positive Sentiment - Exceptional Service
```json
{
  "text": "I am writing to commend your team for the outstanding service I received. Not only was the product exactly as described, but when I had a question, your support team responded within minutes and provided clear, helpful guidance. This level of service is rare and greatly appreciated."
}
```

### 11. Neutral Sentiment - Account Inquiry
```json
{
  "text": "I need to update my account information including my email address and phone number. Can you please guide me through the process or provide a link to where I can make these changes? I also want to confirm that my payment method on file is still valid."
}
```

### 12. Negative Sentiment - Cancellation Issue
```json
{
  "text": "I cancelled my subscription over a month ago but I'm still being charged. I've sent multiple emails and made several phone calls but the charges continue. This is unauthorized billing and I want it stopped immediately. I also want a refund for all charges after my cancellation date."
}
```

### 13. Positive Sentiment - Exceeded Expectations
```json
{
  "text": "I am thrilled with my purchase! The product arrived earlier than expected, was beautifully packaged, and works even better than advertised. The customer service representative I spoke with was knowledgeable and friendly. This has been one of my best online shopping experiences."
}
```

### 14. Neutral Sentiment - Feedback
```json
{
  "text": "I would like to provide feedback on my recent experience. The ordering process was straightforward and the website was easy to navigate. I received email confirmations at each step. Overall, the process was smooth, though I think the checkout could be simplified slightly."
}
```

### 15. Negative Sentiment - Product Defect
```json
{
  "text": "The item I received is defective and doesn't work at all. I've tried everything to get it functioning but it appears to be a manufacturing defect. I need a replacement or refund as soon as possible. This is very inconvenient as I needed this item for an important project."
}
```

## Expected Response Format

```json
{
  "requestId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "timestamp": "2024-01-15T10:30:00Z",
  "sentiment": "Negative",
  "keyPhrases": [
    {
      "text": "poor service",
      "confidenceScore": 1.0
    },
    {
      "text": "rude staff",
      "confidenceScore": 1.0
    },
    {
      "text": "damaged product",
      "confidenceScore": 1.0
    }
  ]
}
```

## Postman Setup Steps

1. **Create a new request:**
   - Method: `POST`
   - URL: `http://localhost:7071/api/complaints` (or your Azure URL)

2. **Set Headers:**
   - Key: `Content-Type`
   - Value: `application/json`

3. **Set Body:**
   - Select `raw`
   - Select `JSON` from dropdown
   - Paste one of the sample requests above

4. **Send Request**

## Testing Different Scenarios

- **Negative Sentiments:** Use complaints #1, #2, #3, #8, #9, #12, #15
- **Positive Sentiments:** Use complaints #4, #5, #10, #13
- **Neutral Sentiments:** Use complaints #6, #7, #11, #14

## Verifying Results

1. Check `sentiment` field - should be "Positive", "Negative", or "Neutral"
2. Check `keyPhrases` array - should contain relevant phrases from the complaint
3. Check `requestId` - should be a valid GUID
4. Check `timestamp` - should be in ISO 8601 format (UTC)

## Error Responses

### Missing Text
```json
{
  "error": "Text is required",
  "requestId": "guid"
}
```

### Invalid JSON
```json
{
  "error": "Invalid JSON format",
  "requestId": "guid"
}
```

### Service Error
```json
{
  "error": "Analysis service unavailable",
  "requestId": "guid"
}
```

