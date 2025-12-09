# Clickboard

Clickboard is a modern clipboard manager for Windows, designed for speed, security, and ease of use. Save text, images, and audio as pressable buttons—instantly copy them to your clipboard with a single click. All data is encrypted and persists between sessions.

---

## Features

- **Grid Layout:** Clipboard buttons are displayed in a 5-across grid for easy access and organization.
- **Drag-and-Drop:** Add images, audio, and other supported files by dragging them into the input field or directly onto the button grid.
- **Supported Formats:** PNG, BMP, JPG, JPEG, GIF, IMG, WEBP, MP3, WAV, OGG.
- **Audio Player:** Right-click an audio button and select "Play" to listen in the built-in player.
- **Themes:** Choose from hardcoded and custom themes. Instantly switch appearance from the theme selector or create your own.
- **PIN Security:** Set a 4-digit PIN for extra protection. Required on startup if enabled.
- **Load on Startup:** Optionally launch Clickboard automatically when Windows starts.
- **Encrypted Storage:** All clipboard entries are encrypted with AES-256, and salted. Only your app instance can decrypt them.
- **Easy Editing:** Right-click any button to edit its value, display name, or delete it.
- **Wipe Data:** Erase all clipboard entries and settings from the settings menu.

---

## Getting Started

1. **Add a Clipboard Button:**
   - Type text and click **Add**.
   - Or drag/drop images, audio, or supported files into the input field or grid.
   - New buttons appear in the grid. Click to copy their content.

2. **Edit or Delete:**
   - Right-click a button for options: Edit, Edit Display Name, Delete, or Play (for audio).

3. **Themes:**
   - Click the **T** button to open the theme selector and change the app's look. You can also create a custom theme, using the provided template. 
	- template.theme.json in the repo (Theme files must use 6-digit hex color codes in the format #RRGGBB (for example, #212B4E).
      Do not use 8-digit hex codes (#AARRGGBB) or include alpha values—only standard HTML hex color codes are supported)

4. **Settings:**
   - Click the **⚙** button to open settings. Enable "Load on Startup" or wipe all data.

5. **PIN Security:**
   - Click the **PIN** button to set, change, or remove your PIN.
	


---

## Security

- All clipboard data is encrypted using AES-256.
- The encryption key is generated and stored locally.
- Optional PIN protection uses secure hashing and salting (PBKDF2).

---

## Troubleshooting
- Window lost off screen? delete your `mainwindow.loc` or `audioplayer.loc` file in the Clickboard folder to reset its position.
- If you encounter issues, use the **LOGS** button to open the diagnostics log and send it to support.
- If the app cannot load buttons, check for missing/corrupted config or key files.
- Lost encryption key or cfg means lost clipboard data —keep backups if needed.

---

## Updating Clickboard

1. **Backup your clipboard data:**
   - Copy `clickboard.cfg` and `clickboard.key` from your Clickboard folder.
2. **Download the latest version:**
   - Replace the old `Clickboard.exe` and DLLs with the new ones.
   - Do **not** replace your config or key files.
3. **Restart Clickboard:**
   - Your buttons and settings will be preserved.

---

## Support

Need help? Contact [@s.o.b.u on Discord](https://discord.com/) and send your diagnostics log for support.
