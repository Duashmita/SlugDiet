# Undercover Barber - Unity Setup Guide

## Project Setup

### 1. Create New Unity Project
1. Open Unity Hub
2. Create new project with **3D** or **3D URP** template
3. Name it "UndercoverBarber"
4. Copy the contents of this `Assets` folder into your project's Assets folder

### 2. Required Packages
Install via Package Manager (Window > Package Manager):
- **TextMeshPro** (usually included)
- **Input System** (for modern input handling)
- **AR Foundation** (if adding AR support)
- **Newtonsoft JSON** (for API parsing) - via git URL: `com.unity.nuget.newtonsoft-json`

### 3. Scene Setup

Create the following scenes in `Assets/Scenes/`:

#### MainGame Scene
1. Create empty GameObjects for managers:
   - `GameManager` - attach `GameManager.cs`
   - `ChatbotService` - attach `ChatbotService.cs`
   - `DialogueManager` - attach `DialogueManager.cs`
   - `HaircutController` - attach `HaircutController.cs`
   - `UIManager` - attach `UIManager.cs`

2. Create Canvas for UI:
   - Set Canvas Scaler to "Scale With Screen Size"
   - Reference Resolution: 1920x1080

3. Create screen panels as children of Canvas (see UI setup below)

### 4. API Key Configuration

#### Setting Up Chatbot API:

1. Create ChatbotConfig asset:
   - Right-click in Project > Create > Undercover Barber > Chatbot Config
   - Name it "ChatbotConfig"
   - Place in `Assets/Resources/` folder

2. Configure the asset:
   ```
   Provider: OpenAI (or Anthropic)
   API Key: YOUR_API_KEY_HERE
   API Endpoint: https://api.openai.com/v1/chat/completions
   Model Name: gpt-3.5-turbo (or gpt-4)
   Temperature: 0.8
   Max Tokens: 150
   ```

3. **SECURITY NOTE**:
   - Never commit API keys to version control
   - For production, use environment variables or secure key storage
   - Consider using a backend proxy server

#### API Providers Supported:
- **OpenAI**: GPT-3.5-turbo, GPT-4
- **Anthropic**: Claude models
- **Custom**: Any OpenAI-compatible API

### 5. Create ScriptableObject Databases

#### Suspect Database:
1. Right-click > Create > Undercover Barber > Suspect Profile
2. Create 3 suspects:
   - "The Clipper"
   - "Slick Eddie"
   - "The Professor"
3. Fill in traits and clue dialogues
4. Create Suspect Database asset and add all suspects

#### Customer Database:
1. Right-click > Create > Undercover Barber > Customer Data
2. Create 5+ customers with different personalities
3. Add dialogue responses and personality prompts
4. Create Customer Database asset and add all customers

### 6. UI Setup

Create these UI elements under Canvas:

```
Canvas
├── TitleScreen
│   ├── TitleText
│   ├── SubtitleText
│   ├── BadgeIcon
│   └── StartButton
├── BriefingScreen
│   ├── ClassifiedHeader
│   ├── SuspectPanel
│   │   ├── SilhouetteImage
│   │   ├── CodenameText
│   │   └── TraitsText
│   ├── MissionPanel
│   └── BeginButton
├── BarbershopScreen
│   ├── ShopHeader
│   ├── CustomerDisplay
│   ├── HaircutArea
│   │   ├── HairCanvas
│   │   ├── ToolButtons
│   │   └── ProgressBar
│   ├── DialogueArea
│   │   ├── DialogueHistory (ScrollView)
│   │   └── DialogueButtons
│   ├── ThoughtBubble
│   └── ActionBar
├── StreetChaseScreen
│   ├── ChaseHeader
│   ├── ChaseArena
│   ├── StaminaBar
│   └── Controls
├── CarChaseScreen
│   ├── ChaseHeader
│   ├── RoadArea
│   ├── HealthBar
│   └── Controls
└── ResultScreen
    ├── ResultIcon
    ├── TitleText
    ├── MessageText
    ├── StatsText
    └── PlayAgainButton
```

### 7. Prefab Setup

Create prefabs for:
- `DialogueEntry` - UI panel for dialogue messages
- `HairParticle` - 3D object or sprite for hair strands
- `Obstacle_TrashCan`, `Obstacle_Barrier`, etc.
- `TrafficCar_Sedan`, `TrafficCar_SUV`, etc.

### 8. Adding AR Support (Optional)

For AR hair cutting:
1. Import AR Foundation package
2. Create AR Session and AR Session Origin
3. Modify `HaircutController` to use AR raycasting
4. Use AR Face Tracking for realistic hair placement

### 9. Build Settings

#### Mobile (iOS/Android):
- Player Settings > Resolution: Portrait or Landscape
- Enable Touch input
- Optimize textures for mobile

#### WebGL:
- Compression: Gzip
- Memory Size: 256MB minimum

## File Structure
```
Assets/
├── Scripts/
│   ├── Core/           - GameManager, state machine
│   ├── API/            - Chatbot integration
│   ├── Data/           - ScriptableObjects
│   ├── Gameplay/       - Haircut, dialogue systems
│   ├── Chase/          - Street and car chase
│   └── UI/             - UI controllers
├── Scenes/
├── Prefabs/
├── Art/
│   ├── Characters/
│   ├── Environments/
│   ├── UI/
│   └── Effects/
├── Audio/
│   ├── Music/
│   └── SFX/
├── Animations/
├── Materials/
├── Fonts/
└── Resources/          - ChatbotConfig goes here
```

## Testing

1. **Without API**: Game works with fallback dialogue responses
2. **With API**: Test with low-cost model (gpt-3.5-turbo) first
3. **Chase sequences**: Use keyboard or touch controls

## Troubleshooting

### API Not Working:
- Check API key is correct
- Check internet connectivity
- Look for errors in Unity Console
- Fallback responses should still work

### UI Not Showing:
- Ensure UIManager references are assigned
- Check Canvas render mode
- Verify screen panels are in hierarchy

### Chase Not Starting:
- Verify GameManager state transitions
- Check event subscriptions in Start()
