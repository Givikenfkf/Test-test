
WinlatorXRBridge - BepInEx plugin (build by GitHub Actions)

How to build:
1) Put UnityEngine.dll, Assembly-CSharp.dll and BepInEx.dll into repo lib/ OR set DOWNLOAD_GAME_DLLS=true and configure private release assets + secret GAME_DLL_PAT and secrets ASSETS_OWNER/ASSETS_REPO/ASSETS_TAG.
2) Push to main.
3) Go to Actions -> Build WinlatorXRBridge (Windows) -> run or view a run.
4) Download the artifact WinlatorXRBridge-full (zip) and extract DLL from artifacts/.
5) Place the DLL into Gorilla Tag/BepInEx/plugins/

Notes:
- If you keep lib/ in a public repo, make the repo private to avoid distributing game DLLs.
- If you need help setting secrets or fetching the artifact from the container, follow the instructions in the earlier chat.
