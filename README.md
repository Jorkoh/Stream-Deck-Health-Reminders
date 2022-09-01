## Build

- Project>Manage NuGet Packages>Update all
- Project>Properties>Build>Output path: `..\..\com.jorkoh.health.reminders.sdPlugin\`
- Solution>Properties>Configuration Properties>Configuration>Release
- Mark all resources as `Copy always`
- Build
- On projects root (outside of repo folder) place [`DistributionTool.exe`](https://developer.elgato.com/documentation/stream-deck/sdk/packaging/)
- On projects root (outside of repo folder) create a folder named `install_output`
- On projects root (outside of repo folder) create `install.bat` with this content:

```
setlocal
SET OUTPUT_DIR=C:\Users\jorge\VisualStudioProjects\install_output
SET DISTRIBUTION_TOOL="C:\Users\jorge\VisualStudioProjects\DistributionTool.exe"
SET STREAM_DECK_FILE="C:\Program Files\Elgato\StreamDeck\StreamDeck.exe"
SET STREAM_DECK_LOAD_TIMEOUT=4

taskkill /f /im streamdeck.exe
taskkill /f /im %2.exe
timeout /t 2
del %OUTPUT_DIR%\%2.streamDeckPlugin
%DISTRIBUTION_TOOL% -b -i %2.sdPlugin -o %OUTPUT_DIR%
rmdir %APPDATA%\Elgato\StreamDeck\Plugins\%2.sdPlugin /s /q
START "" %STREAM_DECK_FILE%
timeout /t %STREAM_DECK_LOAD_TIMEOUT%
%OUTPUT_DIR%\%2.streamDeckPlugin
```

- Run with `install.bat RELEASE com.jorkoh.health.reminders`
- Plugin auto-installs and is packaged in `install_output`