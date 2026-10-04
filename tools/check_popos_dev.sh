#!/usr/bin/env bash
set -u

ok=0
warn=0
fail=0

check_cmd() {
  local name="$1"
  local cmd="$2"
  if command -v "$cmd" >/dev/null 2>&1; then
    printf "OK   %-18s %s\n" "$name" "$(command -v "$cmd")"
    ok=$((ok+1))
  else
    printf "MISS %-18s %s\n" "$name" "$cmd"
    fail=$((fail+1))
  fi
}

printf "TERRE ZÉRO // Pop!_OS environment check\n"
printf "======================================\n"

check_cmd "git" git
check_cmd "dotnet" dotnet
check_cmd "go" go
check_cmd "python3" python3

if command -v dotnet >/dev/null 2>&1; then
  printf "INFO dotnet             %s\n" "$(dotnet --version 2>/dev/null || true)"
fi

if command -v go >/dev/null 2>&1; then
  printf "INFO go                 %s\n" "$(go version 2>/dev/null || true)"
fi

geoclue="/usr/libexec/geoclue-2.0/demos/where-am-i"
if [[ -x "$geoclue" ]]; then
  printf "OK   %-18s %s\n" "GeoClue demo" "$geoclue"
  ok=$((ok+1))
else
  printf "WARN %-18s %s\n" "GeoClue demo" "where-am-i absent; desktop location will use fallback"
  warn=$((warn+1))
fi

godot=""
for candidate in godot-mono godot4-mono godot4 godot; do
  if command -v "$candidate" >/dev/null 2>&1; then
    godot="$(command -v "$candidate")"
    break
  fi
done

if [[ -n "$godot" ]]; then
  printf "OK   %-18s %s\n" "Godot" "$godot"
  "$godot" --version 2>/dev/null | head -n 1 | sed 's/^/INFO Godot              /'
  ok=$((ok+1))
else
  printf "WARN %-18s %s\n" "Godot" "install the .NET/Mono build of Godot 4.x"
  warn=$((warn+1))
fi

if [[ -f "godot_project/TerreZero.csproj" ]]; then
  printf "OK   %-18s %s\n" "project" "godot_project/TerreZero.csproj"
  ok=$((ok+1))
else
  printf "MISS %-18s %s\n" "project" "run this script from the Terre-Z-ro repository root"
  fail=$((fail+1))
fi

printf "\nResult: %d OK, %d warning(s), %d missing requirement(s).\n" "$ok" "$warn" "$fail"

if [[ "$fail" -gt 0 ]]; then
  exit 1
fi