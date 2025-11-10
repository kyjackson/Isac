# ISAC Mobile App Setup

## API Configuration

The ISAC mobile app requires API keys for:
1. **OpenAI Realtime API** - For speech-to-text and AI responses
2. **Cartesia TTS API** - For voice cloning and text-to-speech

### Setting up API Keys

#### Option 1: Through the App Settings
1. Launch the ISAC mobile app
2. Navigate to Settings tab
3. Enter your API keys:
   - OpenAI API Key (must have access to `gpt-4o-realtime-preview`)
   - Cartesia API Key
   - Voice ID (obtained from voice enrollment)
4. Tap "Save Settings"

#### Option 2: Environment Variables (Development)
For development, you can set environment variables:
- `isac_ai_api_key` - Your OpenAI API key
- `isac_api_key` - Your Cartesia API key
- `voice_id` - Your Cartesia voice ID

### Getting API Keys

1. **OpenAI API Key**
   - Sign up at https://platform.openai.com
   - Generate an API key from the API Keys section
   - Ensure you have access to the Realtime API (currently in preview)

2. **Cartesia API Key**
   - Sign up at https://cartesia.ai
   - Generate an API key from your dashboard

### Testing the Connection

1. After entering your API keys in Settings, go to the Home tab
2. Tap "Test API Connection"
3. You should see a response from the AI assistant

### Voice Enrollment

To use your own voice for responses:
1. Go to the Enrollment tab
2. Record voice samples as prompted
3. The app will create a voice profile with Cartesia
4. Your Voice ID will be automatically saved

## Security Notes

- API keys are stored in device preferences (secure storage on mobile)
- Never commit API keys to source control
- For production, consider using a proxy backend to avoid exposing keys