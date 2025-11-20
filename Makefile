# ===== User configurable =====
# Unity Version
UNITY_VERSION ?= 6000.2.13f1

# Unity executable path
UNITY_PATH_MAC ?= /Applications/Unity/Hub/Editor/$(UNITY_VERSION)/Unity.app/Contents/MacOS/Unity
UNITY_PATH_WIN ?= C:\Program Files\Unity\Hub\Editor\$(UNITY_VERSION)\Editor\Unity.exe

# Project path
PROJECT_PATH ?= $(CURDIR)

# Solution path (for dotnet format)
SOLUTION_PATH ?= unity-mvvm.slnx

# Execute Tool Method
EXECUTE_METHOD ?= Refresh.Run

# (Optional) Additional arguments to pass to Unity
UNITY_EXTRA_ARGS ?=

# ===== OS detection & shell =====
ifeq ($(OS),Windows_NT)
  # Windows
  SHELL := pwsh.exe
  .SHELLFLAGS := -NoProfile -NonInteractive -ExecutionPolicy Bypass -Command
  UNITY_PATH := $(UNITY_PATH_WIN)

  REFRESH_CMD = & '$(UNITY_PATH)' `
    -batchmode -quit `
    -projectPath '$(PROJECT_PATH)' `
    -executeMethod $(EXECUTE_METHOD) `
    -nographics -logFile - $(UNITY_EXTRA_ARGS)
else
  # macOS / Linux
  SHELL := /bin/bash
  .SHELLFLAGS := -e -o pipefail -c
  UNITY_PATH := $(UNITY_PATH_MAC)

  REFRESH_CMD = $(UNITY_PATH) \
    -batchmode -quit \
    -projectPath "$(PROJECT_PATH)" \
    -executeMethod "$(EXECUTE_METHOD)" \
    -nographics -logFile - $(UNITY_EXTRA_ARGS)
endif

# ===== Targets =====
.PHONY: help setup fmt lint sync sync-quiet which-unity

help:
	@echo "Targets:"
	@echo "  make setup        Setup git hooks path to .githooks"
	@echo "  make fmt          Format C# code using dotnet format"
	@echo "  make lint         Check C# code formatting using dotnet format"
	@echo "  make sync         Run Unity headless and call $(EXECUTE_METHOD) to force .meta generation"
	@echo "  make sync-quiet   Same, but suppress Unity log output"
	@echo "  make which-unity     Print resolved Unity executable path"
	@echo ""
	@echo "Variables (override with make VAR=value):"
	@echo "  UNITY_VERSION   (default: $(UNITY_VERSION))"
	@echo "  UNITY_PATH_MAC  (default: $(UNITY_PATH_MAC))"
	@echo "  UNITY_PATH_WIN  (default: $(UNITY_PATH_WIN))"
	@echo "  PROJECT_PATH    (default: $(PROJECT_PATH))"
	@echo "  SOLUTION_PATH    (default: $(SOLUTION_PATH))"
	@echo "  EXECUTE_METHOD  (default: $(EXECUTE_METHOD))"
	@echo "  UNITY_EXTRA_ARGS (default: '$(UNITY_EXTRA_ARGS)')"

setup:
	@echo "Setting up git hooks path to .githooks"
	git config core.hooksPath .githooks
	@echo "Setup completed."

fmt:
	@echo "Formatting C# code..."
	dotnet format $(SOLUTION_PATH) --severity error
	@echo "Code formatting completed."

lint:
	@echo "Checking C# code formatting..."
	dotnet format $(SOLUTION_PATH) --severity warn --verify-no-changes
	@echo "Code formatting check completed."

which-unity:
	@echo "Using Unity: $(UNITY_PATH)"

sync: which-unity
	@echo "Refreshing Asset Database for: $(PROJECT_PATH)"
	@output=$$($(REFRESH_CMD) 2>&1); \
	echo "$$output"; \
	if echo "$$output" | grep -q "Multiple Unity instances cannot open the same project"; then \
		echo "Unity already running — treating as success."; \
		exit 0; \
	fi

# Quiet version of sync
sync-quiet: which-unity
	@echo "Refreshing (quiet) for: $(PROJECT_PATH)"
ifeq ($(OS),Windows_NT)
	$(REFRESH_CMD) | Out-Null
else
	$(REFRESH_CMD) >/dev/null 2>&1
endif
