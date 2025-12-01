# CosmoChess Android App

A native Android chess application that connects to the CosmoChess backend server.

## Features

- **User Authentication**: Login and registration
- **Play vs Bot**: Chess games against AI opponents with:
  - 6 difficulty levels (Beginner to Master)
  - 3 playing styles (Balanced, Aggressive, Solid)
- **Interactive Chess Board**: Touch-based drag-and-drop interface
- **Real-time Communication**: SignalR integration for live game updates
- **Material Design**: Modern Android UI with Material Components

## Technology Stack

- **Language**: Kotlin
- **Min SDK**: 24 (Android 7.0)
- **Target SDK**: 34 (Android 14)
- **Architecture**: MVVM with Repository pattern
- **Networking**:
  - Retrofit for REST API
  - SignalR for WebSocket connections
- **UI**: Material Design Components, Navigation Component

## Prerequisites

- Android Studio (latest version recommended)
- JDK 8 or higher
- Android device or emulator running Android 7.0+

## Configuration

### Backend URL

By default, the app connects to `http://10.0.2.2/` which is the localhost address for Android emulators.

To change the backend URL, edit `ApiClient.kt`:

```kotlin
private val BASE_URL = "http://your-server-url/"
```

For physical devices:
- Use your computer's local IP (e.g., `http://192.168.1.100/`)
- Or use your production server URL

## Building the Project

### Using Android Studio

1. Open Android Studio
2. Select "Open an Existing Project"
3. Navigate to `CosmoChess/android-app`
4. Wait for Gradle sync to complete
5. Click "Run" or press Shift+F10

### Using Command Line

```bash
cd android-app

# Build debug APK
./gradlew assembleDebug

# Install on connected device
./gradlew installDebug

# Build and run
./gradlew installDebug && adb shell am start -n com.cosmochess/.ui.MainActivity
```

## Project Structure

```
app/src/main/
├── java/com/cosmochess/
│   ├── ChessApplication.kt          # Application class
│   ├── chess/
│   │   └── ChessEngine.kt           # Chess logic and FEN parsing
│   ├── data/
│   │   ├── model/                   # Data models (User, Game, etc.)
│   │   └── repository/              # Repository layer
│   ├── network/
│   │   ├── ApiClient.kt             # Retrofit configuration
│   │   ├── ApiService.kt            # API endpoints
│   │   └── SignalRManager.kt        # SignalR connection
│   └── ui/
│       ├── MainActivity.kt          # Main activity
│       ├── auth/                    # Login & Register fragments
│       ├── menu/                    # Main menu fragment
│       ├── game/                    # Game fragments
│       └── views/
│           └── ChessBoardView.kt    # Custom chess board view
└── res/
    ├── layout/                      # XML layouts
    ├── navigation/                  # Navigation graph
    └── values/                      # Strings, colors, themes
```

## Key Components

### ChessBoardView
Custom view that renders the chess board and handles touch input:
- Displays pieces using Unicode chess symbols
- Highlights selected squares and legal moves
- Touch-based piece selection and movement

### ChessEngine
Lightweight chess logic implementation:
- FEN string parsing and generation
- Basic move validation
- Board state management

### SignalRManager
Handles real-time game updates:
- Connects to backend SignalR hub
- Receives opponent moves
- Broadcasts game events

### Repositories
- **AuthRepository**: User authentication and session management
- **GameRepository**: Game creation, moves, and state

## API Integration

The app communicates with the CosmoChess backend via:

### REST API
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/games/vs-bot` - Create bot game
- `POST /api/games/move` - Make a move

### SignalR WebSocket
- `/api/gamehub` - Real-time game hub
  - `ReceiveMove` - Opponent move notification
  - `GameOver` - Game end notification
  - `PlayerJoined` - Player join notification

## Testing

### With Android Emulator
1. Start your CosmoChess backend (via Docker or locally)
2. Launch Android emulator
3. Run the app
4. Backend will be accessible at `http://10.0.2.2/`

### With Physical Device
1. Ensure device and computer are on the same network
2. Update `BASE_URL` in `ApiClient.kt` to your computer's IP
3. Enable USB debugging on your device
4. Connect via USB and run the app

## Troubleshooting

### Network Connection Issues
- Verify backend is running and accessible
- Check BASE_URL configuration
- Ensure `INTERNET` permission is in AndroidManifest.xml
- For HTTPS, you may need to add network security config

### Build Errors
- Clean and rebuild: `Build > Clean Project` then `Build > Rebuild Project`
- Invalidate caches: `File > Invalidate Caches / Restart`
- Check Gradle sync errors in Build tab

### SignalR Connection Fails
- Verify backend SignalR hub is running at `/api/gamehub`
- Check authentication token is being sent correctly
- Monitor logcat for SignalR connection logs

## Future Enhancements

- [ ] Online multiplayer game listing
- [ ] Game history and replay
- [ ] Position analysis with Stockfish
- [ ] Captured pieces display
- [ ] Time controls
- [ ] Push notifications for game events
- [ ] Dark mode support
- [ ] Tablet layout optimization

## License

This project is part of CosmoChess and follows the same license as the main repository.
