## NEW FEATURE

### Optional PIN Protection
- **PIN Security:** You can set a 4-digit PIN to protect your clipboard data. If enabled, the app will prompt for the PIN on startup.
- **Secure Storage:** The PIN is securely hashed using PBKDF2 with a random salt and never stored in plaintext.
- **PIN Management:** Use the **PIN** button in the top panel to set, change, or remove your PIN at any time.
- **No PIN Required:** If you do not set a PIN, the app starts normally without prompting.

# Clickboard
**DEMO**
[![DEMO](https://img.youtube.com/vi/kaADbWzfjdQ/maxresdefault.jpg)](https://youtu.be/kaADbWzfjdQ)

**Clickboard** is an easy-to-use, lightweight clipboard manager for Windows. It allows users to create pressable buttons that instantly copy saved text to the clipboard. All buttons are securely saved and persist between sessions, with encryption and a unique key. The app features a modern color scheme, custom UI, and built-in diagnostics logging for troubleshooting.

- **Persistent & Secure:**  
  All buttons are saved to an encrypted config file. Only your app instance can decode them.

## Usage

1. **Add a Clipboard Button:**
   - Type your text in the input field.
   - Click the **Add** button.
   - A new button appears below. Click it to copy the text.

2. **Edit or Delete a Button:**
   - Right-click any clipboard button to open its context menu.
   - Select **Delete** to remove the button.
   - Select **Edit** to change the clipboard value (the text that gets copied).
   - Select **Edit Display Name** to change the button's visible label without affecting the copied value.

3. **View or Send Diagnostics Log:**
   - Click the **LOGS** button in the top panel.
   - The log file (`Clickboard.log`) will open in Explorer.
   - Send this file to support if you encounter issues.

4. **Set or Change PIN:**
   - Click the **PIN** button in the top panel to open the PIN management window.
   - Set, change, or remove your 4-digit PIN for additional security.
   - If a PIN is set, you must enter it each time the app starts.

## Security

- All clipboard entries are encrypted using AES-256.
- The encryption key is generated and stored locally; only your app can decrypt the config.
- **Optional PIN protection:** If enabled, your app requires a 4-digit PIN on startup. The PIN is securely hashed and salted using PBKDF2.

## Troubleshooting

- If you encounter issues, click the **LOGS** button and send the `Clickboard.log` file to support.
- If the app cannot load buttons, you may see a warning. This can happen if the encryption key or config file is missing or corrupted.
- If your encryption is corrupted, your clipboard is lost. i will not be able to 
decrypt your encryption key.

## Updating Clickboard

To update to the latest version:

1. **Backup your clipboard data:**  
   - Make a copy of `clickboard.cfg` and `clickboard.key` in your Clickboard folder.

2. **Download the new version:**  
   - Obtain the latest `Clickboard.exe` (and any DLLs if provided) from the official release.

3. **Replace application files:**  
   - Overwrite the old `Clickboard.exe` (and DLLs) with the new ones.
   - **Do not** replace `clickboard.cfg` or `clickboard.key`—these contain your saved clipboard entries and encryption key.

4. **Restart Clickboard:**  
   - Launch the updated app. Your buttons and settings will be preserved.

If you encounter issues, restore your backup files or contact support.

**Need help?**  
Contact [@s.o.b.u on Discord](https://discord.com/) and send your diagnostics log for support.
