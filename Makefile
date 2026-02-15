# --- Configuration ---

PROJECT_DIR := WinHUD
PROJECT_FILE := $(PROJECT_DIR)/WinHUD.csproj

# 1. STRICT: Get CURRENT version from the .csproj file
# This is now the ONLY source of truth.
# If <Version> is missing, exit 1 (Hard Failure)
v := $(shell powershell -NoProfile -Command "& { [xml]$$xml = Get-Content '$(PROJECT_FILE)'; if ($$xml.Project.PropertyGroup.Version) { $$xml.Project.PropertyGroup.Version } else { exit 1 } }")

# Check validity
ifeq ($(v),)
$(error [FATAL] Could not detect <Version> in '$(PROJECT_FILE)'. Please manually update the .csproj file with the desired version before releasing.)
endif

# --- Professional Naming Conventions ---
ARCH := win-x64
DIST_DIR := dist

# Artifact Naming
# The Portable folder (e.g., dist/WinHUD-v0.1.0-win-x64)
PORTABLE_DIR := $(DIST_DIR)/WinHUD-v$(v)-$(ARCH)

# The Single File temp folder
SINGLE_TEMP_DIR := $(DIST_DIR)/tmp_single

# Final Artifact Files
ARTIFACT_EXE := $(DIST_DIR)/WinHUD-v$(v)-$(ARCH).exe
ARTIFACT_ZIP := $(DIST_DIR)/WinHUD-v$(v)-$(ARCH).zip
ARTIFACT_TAR := $(DIST_DIR)/WinHUD-v$(v)-$(ARCH).tar.gz

.PHONY: all clean build-single build-portable package release help

# Default target
all: clean build-single package

# --- Help ---
help:
	@echo "WinHUD Manual Builder"
	@echo "Detected Version: $(v)"
	@echo "Arch:             $(ARCH)"
	@echo ""
	@echo "Usage:"
	@echo "  1. Manually edit $(PROJECT_FILE) to set <Version>X.Y.Z</Version>"
	@echo "  2. Run 'make release' to build and tag that version."

# --- Tasks ---

clean:
	@echo "[+] Cleaning artifacts..."
	@if exist "$(DIST_DIR)" rmdir /s /q "$(DIST_DIR)"
	@dotnet clean $(PROJECT_FILE) -v q

build-single:
	@echo "[+] Building Single File Executable (Version: $(v))..."
	@dotnet publish $(PROJECT_FILE) -c Release -r $(ARCH) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:PublishReadyToRun=true \
		-p:Version=$(v) \
		-o $(SINGLE_TEMP_DIR)
	
	@echo "[+] Renaming and moving executable..."
	@if not exist "$(DIST_DIR)" mkdir "$(DIST_DIR)"
	@copy "$(SINGLE_TEMP_DIR)\WinHUD.exe" "$(ARTIFACT_EXE)" > nul
	@echo "    -> Created: $(ARTIFACT_EXE)"
	@rmdir /s /q "$(SINGLE_TEMP_DIR)"

build-portable:
	@echo "[+] Building Portable Directory (Version: $(v))..."
	@dotnet publish $(PROJECT_FILE) -c Release -r $(ARCH) \
		--self-contained true \
		-p:Version=$(v) \
		-o $(PORTABLE_DIR)

package: build-portable
	@echo "[+] Packaging Archives..."
	
	@echo "    - Creating ZIP..."
	@powershell -Command "Compress-Archive -Path '$(PORTABLE_DIR)' -DestinationPath '$(ARTIFACT_ZIP)' -Force"
	@echo "      -> Created: $(ARTIFACT_ZIP)"
	
	@echo "    - Creating TAR.GZ..."
	@tar -czvf $(ARTIFACT_TAR) -C $(DIST_DIR) WinHUD-v$(v)-$(ARCH)
	@echo "      -> Created: $(ARTIFACT_TAR)"

release: clean all
	@echo "[+] Publishing Release v$(v) to Git..."
	@git add $(PROJECT_FILE)
	@git commit -m "chore: release v$(v)" || echo "Nothing to commit (version might be already committed)"
	@git tag -a v$(v) -m "Release v$(v)"
	@git push origin v$(v)
	@git push
	@echo "---------------------------------------------------"
	@echo "SUCCESS! Release v$(v) is ready."
	@echo "Artifacts:"
	@echo " 1. $(ARTIFACT_EXE)"
	@echo " 2. $(ARTIFACT_ZIP)"
	@echo " 3. $(ARTIFACT_TAR)"
	@echo "---------------------------------------------------"
