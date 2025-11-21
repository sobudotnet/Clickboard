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

2. **Delete a Button:**
   - Right-click any clipboard button.
   - Select **Delete** to remove it.

3. **View or Send Diagnostics Log:**
   - Click the **LOGS** button in the top panel.
   - The log file (`Clickboard.log`) will open in Explorer.
   - Send this file to support if you encounter issues.

## Security

- All clipboard entries are encrypted using AES-256.
- The encryption key is generated and stored locally; only your app can decrypt the config.

## Troubleshooting

- If you encounter issues, click the **LOGS** button and send the `Clickboard.log` file to support.
- If the app cannot load buttons, you may see a warning. This can happen if the encryption key or config file is missing or corrupted.
- If your encryption is corrupted, your clipboard is lost. i will not be able to 
decrypt your encryption key.


**Need help?**  
Contact [@s.o.b.u on Discord](https://discord.com/) and send your diagnostics log for support.
