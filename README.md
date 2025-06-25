# 🔍 SearchMate

**SearchMate** is a fast and lightweight desktop file search utility built with WPF (C#).  
It helps you quickly locate files on all system drives — with support for dark mode, smart caching, and adjustable search behavior.

---

<table>
  <tr>
    <td><img width="400" alt="search" src="https://github.com/user-attachments/assets/3ffa8068-6006-4088-97fd-96ef4053faa6"></td>
    <td><img width="400" alt="resultPanel" src="https://github.com/user-attachments/assets/0f07c16c-f50e-419b-9f0d-2becadc9b7b4"></td>
  </tr>
</table>

---

## ✨ Features

- 🚀 **Fast Multithreaded Search**  
  Utilizes parallel processing to scan all drives and directories.

- 🌙 **Dark/Light Theme Support**  
  Switchable themes with polished styles for modern UI.

- 💾 **Smart File Caching**  
  Caches recent search results to improve performance.  
  Old cache entries are automatically removed based on user-defined rules:
  - Files not accessed in the last **X** days (default: 30)
  - More than **Y** cached entries (default: 100)

- ⚙️ **Customizable Settings**  
  - Enable/disable caching  
  - Set max search results  
  - Choose theme  
  - Clear all caches with one click

- 📁 **Search All Drives**  
  Scans all ready drives in parallel.</br>
  **Note**: SearchMate can only find files in folders that are accessible. Some system or private folders may be skipped due to permission restrictions.

- 🖱️ **File Launch Support**  
  Double-click to open file in Explorer (highlighted).

---

<table>
  <tr>
    <td><img width="400" alt="Settings" src="https://github.com/user-attachments/assets/e03e98b7-b737-48a8-81fa-cc4f040810e6"></td>
    <td><img width="400" alt="themechange" src="https://github.com/user-attachments/assets/f1ef3262-cb35-4661-b487-5dcbbaf49ddc"></td>
  </tr>
</table>

---

## 🛠️ Technologies Used

- **.NET 8 / WPF**
- **C#**
- **XAML**
- **JSON-based persistent settings**

---

## 📦 Installation

1. Clone the repository:
```
git clone https://github.com/your-username/SearchMate.git
```
2.Open **SearchMate.sln** using Visual Studio</br>
3.Build & Run (no external dependencies)

---

## 💬 Feedback

Suggestions, bug reports or feature ideas?</br>
Open an issue or reach me at [ziyaenismert@gmail.com]



---

   
