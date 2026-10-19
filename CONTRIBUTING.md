# 參與 What's up my neighbor for Windows

謝謝你願意幫忙。Windows 版目前是**預發布測試版**：自動化測試已在 GitHub Actions 通過，但真實 Windows 10／11 電腦上的麥克風、藍牙、輸入法與各種 App 還沒有完整實機驗收。所以這個階段最有價值的參與，是在你自己的電腦上試用並回報結果。

macOS 版在另一個 repo：<https://github.com/yuipan0611/whats-up-my-neighbor-ai-input-macos>。兩邊各自測試、各自發布。

## 回報實機測試結果

到 [Issues](https://github.com/yuipan0611/whats-up-my-neighbor-ai-input-windows/issues) 開一個 Issue，盡量寫下：

- **環境**：Windows 版本（例如 Windows 11 23H2）、x64 或 ARM64、輸入法（注音、倉頡、微軟拼音⋯⋯）、使用的麥克風或藍牙耳機。
- **App 版本**：系統匣選單目前不顯示版本，請寫安裝檔名稱（例如 `Neighbor-Setup-0.1.0-beta.1-x64.exe`），或 Windows「設定 → 應用程式 → 已安裝的應用程式」裡 What's up my neighbor 顯示的版本。
- **在哪個 App 裡輸入**：例如 Chrome 的某個網站、LINE、Word、VS Code。
- **步驟、預期與實際結果**，以及畫面上的錯誤訊息原文。

能成功的情況也很有幫助。例如「Windows 11 + 注音 + LINE 桌面版，自動貼上正常」，可以幫我們確認哪些組合已經驗證過。對照目前已驗證與未驗證的範圍，見 [VALIDATION.md](VALIDATION.md)。

請**不要**在 Issue、截圖或記錄裡貼出 Groq API Key、錄音或私人文件。安全性問題請改用私密回報，見 [SECURITY.md](SECURITY.md)。

## 修正文件

錯字、步驟過時、說明和 App 畫面不一致，都可以直接送 PR。說明 App 行為的文字要和實際版本一致；不確定的地方寫「待確認」。

| 文件 | 內容 |
|---|---|
| `README.md` | 功能、安裝、隱私與故障處理、建置 |
| `PRIVACY.md` | 資料傳輸與本機儲存 |
| `VALIDATION.md` | 已驗證與待實機驗收的範圍 |
| `使用說明.txt` | 安裝包內給使用者看的說明 |

## 送程式修正

### 開發環境

需要 Windows、.NET SDK、.NET Framework 4.8 targeting pack 與 Inno Setup 6。完整指令與依賴版本在 [README.md](README.md) 的「建置與驗證」。

```powershell
./scripts/build.ps1
./Neighbor.Tests/bin/Release/net48/Neighbor.Tests.exe --interactive
./scripts/package.ps1
./scripts/test-install.ps1
```

整合測試會開啟隔離的文字視窗並使用剪貼簿，請在測試用的桌面工作階段執行，不要在正在工作的桌面上跑。測試不讀正式金鑰、不錄真實麥克風、不呼叫 Groq。

### 專案結構

| 資料夾 | 內容 |
|---|---|
| `Neighbor.Core/` | 不依賴 Windows 介面的核心邏輯：錄音流程、Groq 辨識、文字規則、語言、詞典與修字學習、設定 |
| `Neighbor.Windows/` | 系統匣 App、快捷鍵、錄音、貼上與 UI Automation |
| `Neighbor.Tests/` | 離線核心測試與受控桌面 UI 測試 |
| `packaging/` | Inno Setup 安裝檔設定 |
| `scripts/` | 建置、打包、安裝流程測試 |

### 流程

1. 先開 Issue 或在既有 Issue 留言，說明你打算怎麼改。小的錯誤修正可以直接送 PR。
2. Fork 後從 `main` 開新分支。
3. 修改程式並補上測試；能放在 `Neighbor.Core` 的邏輯盡量放在那裡，測試不需要開視窗。
4. 本機跑過建置與測試。
5. 送 PR 到 `main`，CI 會在 Windows runner 建置並執行測試。

PR 說明請寫：改了什麼、為什麼、跑了哪些測試，以及有沒有在真實 Windows 電腦上試過（沒有也請寫出來）。

### 需要特別小心的地方

- **只貼上，不送出**：App 永遠不替使用者按 Enter。無法確認輸入框、安全欄位、管理員權限的程式或中文組字狀態不明時，只複製文字。
- **金鑰與資料位置**：認證管理員目標 `WhatsUpMyNeighbor/GroqApiKey`、設定資料夾 `%LOCALAPPDATA%\WhatsUpMyNeighbor`。改了會讓既有使用者失去金鑰或設定。
- **資料去向**：新增任何網路請求、檔案寫入或記錄時，同步更新 `PRIVACY.md`。
- **安裝包大小與內容**：安裝檔要小於 20,000,000 bytes，並且使用明確的檔案清單，不打包測試、除錯符號、模型或測試音訊。
- **依賴版本**：NAudio 與 UI Automation 介面套件的版本是固定的；新增依賴前請先在 Issue 討論。
- **不要求管理員權限**：安裝到目前使用者的個人資料夾，App 也不以管理員身分執行。

## 授權

送出的貢獻會以本專案的 [MIT License](LICENSE) 授權。
