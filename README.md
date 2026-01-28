# 🎮 AI-NPC Unity Game - Team Setup Guide

Welcome to the team! Here's everything you need to get started.

## 📍 Repository Information

**GitHub Repository:** https://github.com/Shivom2110/AI-NPC-Unity-Game

**Project Type:** Single-player Unity game with AI-powered NPCs  
**Timeline:** 6 weeks  
**Your Role:**
- 🎨 **Shaan:** UI & Gameplay Interaction
- 🤖 **Ayush:** OpenAI API & NPC Behavior Logic
- 🔧 **Shivom:** Firebase Backend & System Integration

---

## 🖥️ PART 1: Software Setup (One-Time Setup)

### For Windows Users

#### Step 1: Install Unity Hub + Unity Editor

1. **Download Unity Hub:** https://unity.com/download
2. **Install Unity Hub**
3. **Open Unity Hub** → Click "Installs" tab
4. **Click "Install Editor"**
5. **Choose Version:** Unity 2021.3 LTS (Long Term Support)
6. **Select Modules:**
   - ✅ Visual Studio Community (IMPORTANT - this is your code editor)
   - ✅ Documentation
   - ✅ WebGL Build Support (optional)
   - ✅ Android Build Support (optional)
7. **Click "Install"** (takes 10-20 minutes)

#### Step 2: Verify Visual Studio

After Unity installation:
1. **Open Unity** (any project)
2. **Double-click any C# script**
3. **Visual Studio should open automatically**
4. If it doesn't:
   - Go to: Edit → Preferences → External Tools
   - Set External Script Editor to "Visual Studio"

#### Step 3: Install Git

1. **Download:** https://git-scm.com/download/win
2. **Run installer** with default settings
3. **Verify installation:**
   - Open PowerShell or Command Prompt
   - Type: `git --version`
   - Should show: `git version 2.x.x`

#### Step 4: Configure Git

Open PowerShell or Command Prompt and run:

```powershell
# Set your name and email (use your GitHub email)
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"

# Set default branch name
git config --global init.defaultBranch main

# Set line ending handling (important for cross-platform)
git config --global core.autocrlf true
```

---

### For Mac Users

#### Step 1: Install Unity Hub + Unity Editor

1. **Download Unity Hub:** https://unity.com/download
2. **Install Unity Hub**
3. **Open Unity Hub** → Click "Installs" tab
4. **Click "Install Editor"**
5. **Choose Version:** Unity 2021.3 LTS (Long Term Support)
6. **Select Modules:**
   - ✅ Documentation
   - ✅ Mac Build Support (Il2CPP)
   - ✅ WebGL Build Support (optional)
7. **Click "Install"** (takes 10-20 minutes)

#### Step 2: Install Visual Studio Code

1. **Download VS Code:** https://code.visualstudio.com
2. **Install VS Code**
3. **Open VS Code**
4. **Install Extensions** (click Extensions icon on left sidebar):
   - Search and install: **"C#"** (by Microsoft)
   - Search and install: **"C# Dev Kit"** (by Microsoft)
   - Search and install: **"Unity Code Snippets"**
   - Search and install: **"Debugger for Unity"** (optional)

#### Step 3: Install .NET SDK (Required for C#)

1. **Download:** https://dotnet.microsoft.com/download
2. **Install .NET 6.0 SDK or newer**
3. **Verify installation:**
   ```bash
   dotnet --version
   ```
   Should show: `6.x.x` or higher

**OR using Homebrew:**
```bash
# Install Homebrew if you don't have it
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install .NET SDK
brew install --cask dotnet-sdk

# Verify
dotnet --version
```

#### Step 4: Connect Unity to VS Code

1. **Open Unity**
2. **Go to:** Unity → Preferences → External Tools
3. **External Script Editor:** Browse → Select Visual Studio Code
4. **Click "Regenerate project files"**

#### Step 5: Install Git

**Option A - Using Homebrew (Recommended):**
```bash
# Install Homebrew if you don't have it
/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"

# Install Git
brew install git

# Verify
git --version
```

**Option B - Download Installer:**
1. **Download:** https://git-scm.com/download/mac
2. **Run installer**
3. **Verify:** Open Terminal and type `git --version`

#### Step 6: Configure Git

Open Terminal and run:

```bash
# Set your name and email (use your GitHub email)
git config --global user.name "Your Name"
git config --global user.email "your-email@example.com"

# Set default branch name
git config --global init.defaultBranch main

# Set line ending handling (important for cross-platform)
git config --global core.autocrlf input
```

---

## 📦 PART 2: Clone and Setup Project

### Step 1: Clone Repository

**Windows (PowerShell):**
```powershell
# Navigate to where you want the project
cd "C:\Users\YourName\Documents"

# Create Unity Projects folder
mkdir "Unity Projects"
cd "Unity Projects"

# Clone the repository
git clone https://github.com/Shivom2110/AI-NPC-Unity-Game.git

# Navigate into project
cd AI-NPC-Unity-Game
```

**Mac (Terminal):**
```bash
# Navigate to where you want the project
cd ~

# Create Unity Projects folder
mkdir -p "Unity Projects"
cd "Unity Projects"

# Clone the repository
git clone https://github.com/Shivom2110/AI-NPC-Unity-Game.git

# Navigate into project
cd AI-NPC-Unity-Game
```

---

### Step 2: Open Project in Unity

1. **Open Unity Hub**
2. **Click "Projects" tab**
3. **Click "Add" button**
4. **Navigate to** and select: `Unity Projects/AI-NPC-Unity-Game` folder
5. **Double-click the project** to open it
6. **Wait 5-10 minutes** for Unity to import everything (first time only)

**You should see:**
- Assets folder with Scenes, Scripts, etc.
- Console window (bottom) - should have no errors
- Hierarchy window showing scene contents

---

### Step 3: Verify Setup

**Test 1: Open a Script**
1. In Unity, go to: `Assets/Scripts/Core/`
2. Double-click `NPCMemoryManager.cs`
3. **Windows:** Visual Studio should open
4. **Mac:** VS Code should open
5. You should see syntax highlighting and IntelliSense

**Test 2: Check Git**
```bash
# In Terminal/PowerShell, inside project folder
git status

# Should show: "On branch main" with clean working tree
```

---

## 🔧 PART 3: Firebase Setup

### Step 1: Get Firebase Config File from Shivom

⚠️ **Important:** Shivom will share the `google-services.json` file with you privately (NOT through Git)

### Step 2: Place Firebase File

1. Once you receive `google-services.json`
2. **Place it in:** `Assets/` folder (root of Assets, not in any subfolder)
3. **Do NOT commit this file to Git** (it's in .gitignore)

### Step 3: Install Firebase SDK

1. **Download Firebase Unity SDK:** https://firebase.google.com/download/unity
2. **Import into Unity:**
   - Assets → Import Package → Custom Package
   - Select `FirebaseFirestore.unitypackage`
   - Click "Import"
3. **Wait for import** (takes 2-3 minutes)

---

## 🔑 PART 4: OpenAI API Setup (Ayush Only)

### Step 1: Get OpenAI API Key

1. **Create account:** https://platform.openai.com
2. **Go to:** API Keys section
3. **Click:** "Create new secret key"
4. **Copy the key** (starts with `sk-proj-...`)
5. **⚠️ SAVE IT SECURELY** - you can't see it again!

### Step 2: Add Credits

1. **Go to:** Billing section
2. **Add payment method**
3. **Add $5-10 credits** for testing

### Step 3: Create APIKeys.cs File

1. **Navigate to:** `Assets/Scripts/Config/`
2. **Copy** `APIKeysTemplate.cs` to `APIKeys.cs` (same folder)
3. **Open** `APIKeys.cs` in your code editor
4. **Replace** `"sk-proj-YOUR-OPENAI-KEY-HERE"` with your actual key
5. **Save the file**
6. **⚠️ NEVER commit this file** - it's in .gitignore

**The file should look like:**
```csharp
public static class APIKeys
{
    public const string OPENAI_API_KEY = "sk-proj-abc123xyz..."; // Your real key
}
```

---

## 🌿 PART 5: Your Development Branch

### Step 1: Create Your Branch

**Shaan (UI Development):**
```bash
git checkout -b shaan/ui-development
git push -u origin shaan/ui-development
```

**Ayush (AI Logic):**
```bash
git checkout -b ayush/ai-logic
git push -u origin ayush/ai-logic
```

### Step 2: Verify Your Branch

```bash
# Check current branch
git branch

# Should show * next to your branch name
```

---

## 📚 PART 6: Read the Documentation

### Essential Reading (Before Starting)

1. **README.md** - Project overview
2. **Assets/Documentation/SETUP_GUIDE.md** - Detailed setup guide
3. **Assets/Documentation/QUICK_REFERENCE.md** - Common tasks cheat sheet
4. **Assets/Documentation/PROJECT_TIMELINE.md** - What we're building when
5. **CONTRIBUTING.md** - How to work with Git and submit code

**Where to find:** All in the repository root and `Assets/Documentation/`

---

## ⚠️ IMPORTANT RULES

### 🚫 Never Do This:

1. **❌ Never commit to `main` branch directly**
   - Always work on your branch
   - Merge via Pull Requests only

2. **❌ Never commit API keys or secrets**
   - `APIKeys.cs` (your actual keys)
   - `google-services.json`
   - Any file with passwords/tokens

3. **❌ Never force push**
   - `git push --force` can delete teammates' work
   - If you need help, ask first!

4. **❌ Never commit large binary files**
   - Videos, large models, builds
   - Use Git LFS if needed

---

### ✅ Always Do This:

1. **✅ Pull before starting work each day**
   ```bash
   git checkout main
   git pull origin main
   git checkout your-branch
   git merge main
   ```

2. **✅ Commit frequently with clear messages**
   ```bash
   git add .
   git commit -m "Added dialogue panel UI with 3 button choices"
   ```

3. **✅ Test before committing**
   - Press Play in Unity
   - Make sure no errors
   - Test your feature works

4. **✅ Create Pull Requests when feature is done**
   - Go to GitHub → Pull Requests → New PR
   - Select your branch → main
   - Add description of changes
   - Request review from team

5. **✅ Communicate with team**
   - Before working on a file someone else might be editing
   - When you push breaking changes
   - When you're stuck

---

## 🔄 Daily Workflow

### Every Morning:

```bash
# 1. Get latest changes from main
git checkout main
git pull origin main

# 2. Switch to your branch
git checkout your-branch-name

# 3. Merge main into your branch
git merge main

# 4. If conflicts, resolve them and commit
# 5. Open Unity and start working
```

### During the Day:

- Work in Unity Editor
- Edit code in Visual Studio/VS Code
- Test frequently (press Play in Unity)
- Save often (Ctrl+S / Cmd+S)

### End of Day:

```bash
# 1. Save all Unity work (Ctrl+S / Cmd+S)

# 2. Stage your changes
git add .

# 3. Check what you're committing
git status

# 4. Commit with descriptive message
git commit -m "Descriptive message about what you did"

# 5. Push to your branch
git push origin your-branch-name
```

### When Feature is Complete:

1. **Push final changes**
2. **Go to GitHub repository**
3. **Click "Pull Requests" tab**
4. **Click "New pull request"**
5. **Select:** your-branch → main
6. **Add description** of what you built
7. **Request review** from team members
8. **Wait for approval**
9. **Merge** (after approval)
10. **Delete your branch** (optional, can reuse it)

---

## 🆘 Troubleshooting

### Issue: Unity shows errors on first open

**Solution:** Wait for import to complete (can take 10 minutes)

---

### Issue: Scripts won't open in editor

**Windows:**
```
Unity → Edit → Preferences → External Tools
→ Set External Script Editor to Visual Studio
→ Click "Regenerate project files"
```

**Mac:**
```
Unity → Preferences → External Tools
→ Set External Script Editor to Visual Studio Code
→ Click "Regenerate project files"
```

---

### Issue: Git says "permission denied"

**Solution:** You haven't been added as collaborator yet. Ask Shivom to add you!

---

### Issue: "Firebase not initialized"

**Solution:** 
1. Make sure `google-services.json` is in `Assets/` folder
2. Make sure Firebase SDK is imported
3. Check Unity Console for specific error

---

### Issue: "OpenAI API error 401"

**Solution:**
1. Verify your API key is correct in `APIKeys.cs`
2. Make sure you have credits in OpenAI account
3. Check key hasn't expired

---

### Issue: Merge conflicts

**Solution:**
```bash
# See which files have conflicts
git status

# Open conflicting files in your editor
# Look for <<<<<<< and >>>>>>>
# Keep the version you want
# Remove the conflict markers

# Stage resolved files
git add .

# Complete the merge
git commit -m "Resolved merge conflicts"
```

---

### Issue: IntelliSense not working (Mac)

**Solution:**
```bash
# Make sure .NET SDK is installed
dotnet --version

# If not installed:
brew install --cask dotnet-sdk

# Restart VS Code
# In VS Code: Cmd+Shift+P → "OmniSharp: Restart OmniSharp"
```

---

## 📞 Getting Help

### Documentation
- All documentation is in the repo
- Check `Assets/Documentation/` folder
- Read CONTRIBUTING.md for Git help

### Ask the Team
- Create GitHub Issue for bugs
- Ask in team chat for quick questions
- Schedule call if stuck for >30 minutes

### External Resources
- Unity Docs: https://docs.unity3d.com
- Git Tutorial: https://git-scm.com/book/en/v2
- C# Guide: https://learn.microsoft.com/en-us/dotnet/csharp/

---

## ✅ Setup Verification Checklist

Before starting development, verify:

- [ ] Unity 2021.3 LTS installed
- [ ] Code editor installed and working (VS or VS Code)
- [ ] Git installed and configured
- [ ] Repository cloned successfully
- [ ] Project opens in Unity without errors
- [ ] Can double-click script and it opens in editor
- [ ] Firebase config file in Assets/ folder
- [ ] OpenAI API key configured (Ayush only)
- [ ] On correct branch (not main)
- [ ] Read documentation
- [ ] Can run `git status` successfully
- [ ] Understand daily workflow
- [ ] Know how to create Pull Request

---

## 🎯 Week 1 Goals

### Shaan (UI):
- [ ] Create dialogue panel UI
- [ ] Add NPC name display
- [ ] Create 3-4 dialogue choice buttons
- [ ] Test with one NPC

### Ayush (AI):
- [ ] Set up OpenAI API
- [ ] Test basic API call
- [ ] Create first NPC personality
- [ ] Test AI-generated response

### All:
- [ ] Complete setup
- [ ] Test pushing to your branch
- [ ] Create your first Pull Request
- [ ] Review teammate's code

---

## 🎊 You're Ready!

Once you complete the setup checklist above, you're ready to start building! 

**First Team Meeting:** [Schedule This]
- Review timeline
- Discuss initial tasks
- Q&A session

**Questions?** 
- Create a GitHub Issue
- Message the team chat
- Check documentation

**Let's build something amazing! 🚀**

---

**Repository:** https://github.com/Shivom2110/AI-NPC-Unity-Game

**Team:**
- 🎨 Shaan - UI & Gameplay
- 🤖 Ayush - AI Logic
- 🔧 Shivom - Backend

---

## 🔐 Security Reminder

**Files you should NEVER commit:**
- `APIKeys.cs` (your actual API keys)
- `google-services.json` (Firebase config)
- Any file with passwords, tokens, or secrets

**These are already in `.gitignore`, but double-check before pushing!**

**Check before committing:**
```bash
git status
git diff

# Make sure no secrets are included
grep -r "sk-proj" .  # Search for OpenAI keys
```

---

**Good luck team! 🎮✨** 
