@echo off
rem Opens the Art Pipeline window. Set GODOT to your Godot 4 .NET executable if it is not on PATH.
if "%GODOT%"=="" set GODOT=godot
cd /d "%~dp0.."
"%GODOT%" --path . tools/art_pipeline.tscn