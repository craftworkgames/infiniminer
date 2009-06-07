Name "Infiniminer"

OutFile "infiniminer-installer.exe"

Icon "game.ico"

InstallDir "$PROGRAMFILES\Zachtronics Industries\Infiniminer"
InstallDirRegKey HKLM "Software\Zachtronics Industries\Infiniminer" "Install_Dir"

Page components
Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

Section "Infiniminer (required)"
	
	SectionIn RO
	SetOutPath $INSTDIR
	File "Infiniminer.exe"
	File "InfiniminerServer.exe"
	File "game.ico"
    File "*.txt"
    File "*.dll"
	
	SetOutPath $INSTDIR\Content
	File "Content\*.xnb"
	File "Content\*.wma"
	
	SetOutPath $INSTDIR\Content\blocks
	File "Content\blocks\*.xnb"
	
	SetOutPath $INSTDIR\Content\menus
	File "Content\menus\*.xnb"
	
	SetOutPath $INSTDIR\Content\sounds
	File "Content\sounds\*.xnb"
	
	SetOutPath $INSTDIR\Content\sprites
	File "Content\sprites\*.xnb"
	
	SetOutPath $INSTDIR\Content\icons
	File "Content\icons\*.xnb"
	
	SetOutPath $INSTDIR\Content\ui
	File "Content\ui\*.xnb"
	
	SetOutPath $INSTDIR\Content\tools
	File "Content\tools\*.xnb"
	
	SetOutPath $INSTDIR

	WriteRegStr HKLM "Software\Zachtronics Industries\Infiniminer" "Install_Dir" "$INSTDIR"

	; Write Uninstall keys for Windows
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Zachtronics Industries\Infiniminer" "DisplayName" "Infiniminer"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Zachtronics Industries\Infiniminer" "UninstallString" "$INSTDIR\uninstall.exe"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Zachtronics Industries\Infiniminer" "NoModify" 1
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Zachtronics Industries\Infiniminer" "NoRepair" 1

	WriteUninstaller "uninstall.exe"

SectionEnd

Section "Start Menu Shortcuts"
	CreateDirectory "$SMPROGRAMS\Zachtronics Industries\Infiniminer"
	CreateShortCut "$SMPROGRAMS\Zachtronics Industries\Infiniminer\Infiniminer Client.lnk" "$INSTDIR\Infiniminer.exe" "" "$INSTDIR\game.ico" 0
	CreateShortCut "$SMPROGRAMS\Zachtronics Industries\Infiniminer\Edit Client Configuration.lnk" "notepad.exe" "$INSTDIR\client.config.txt"
	CreateShortCut "$SMPROGRAMS\Zachtronics Industries\Infiniminer\Infiniminer Server.lnk" "$INSTDIR\InfiniminerServer.exe" ""
	CreateShortCut "$SMPROGRAMS\Zachtronics Industries\Infiniminer\Edit Server Configuration.lnk" "notepad.exe" "$INSTDIR\server.config.txt" 
	CreateShortCut "$SMPROGRAMS\Zachtronics Industries\Infiniminer\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
	CreateShortCut "$SMPROGRAMS\Zachtronics Industries\Infiniminer\Infiniminer README.lnk" "notepad.exe" "$INSTDIR\README.txt"
SectionEnd

Section "Uninstall"
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Zachtronics Industries\Infiniminer"
	DeleteRegKey HKLM "Software\Zachtronics Industries\Infiniminer"
	RMDir /r "$INSTDIR"
	RMDir /r "$SMPROGRAMS\Zachtronics Industries\Infiniminer"
SectionEnd
