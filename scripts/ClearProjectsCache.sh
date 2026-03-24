#!/bin/bash
# MokaDocs — Clear build cache (bin, obj, node_modules, _site)
# Works from any directory — auto-detects repo root
# Usage: ./ClearProjectsCache.sh [--force] [--dry-run]

set -euo pipefail

FORCE=false
DRY_RUN=false

for arg in "$@"; do
    case "$arg" in
        --force|-f) FORCE=true ;;
        --dry-run|-n) DRY_RUN=true ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
REPO_ROOT="$(dirname "$SCRIPT_DIR")"

if [ ! -d "$REPO_ROOT/.git" ] && ! ls "$REPO_ROOT"/*.slnx &>/dev/null; then
    echo "Could not find repository root from $REPO_ROOT"
    exit 1
fi

echo ""
echo -e "\033[36mRepository: $REPO_ROOT\033[0m"
echo ""

folders=$(find "$REPO_ROOT" -type d \( -name "bin" -o -name "obj" -o -name "node_modules" -o -name "_site" -o -name "_sample-site" \) -not -path "*/.git/*" 2>/dev/null || true)

if [ -z "$folders" ]; then
    echo -e "\033[32mAlready clean — no cache folders found.\033[0m"
    exit 0
fi

byte_size=$(echo "$folders" | while IFS= read -r f; do du -sk "$f" 2>/dev/null || echo "0 $f"; done | awk '{sum+=$1} END {printf "%.0f", sum}')
if [ "$byte_size" -gt 1048576 ] 2>/dev/null; then
    size_str="$(awk "BEGIN {printf \"%.2f\", $byte_size / 1048576}") GB"
elif [ "$byte_size" -gt 1024 ] 2>/dev/null; then
    size_str="$(awk "BEGIN {printf \"%.1f\", $byte_size / 1024}") MB"
else
    size_str="${byte_size} KB"
fi

count=0
echo -e "\033[33mFolders to delete:\033[0m"
while IFS= read -r folder; do
    rel_path="${folder#$REPO_ROOT/}"
    echo -e "  \033[33m$rel_path\033[0m"
    count=$((count + 1))
done <<< "$folders"

echo ""
echo -e "\033[33mTotal: $count folders ($size_str)\033[0m"
echo ""

if [ "$DRY_RUN" = true ]; then
    echo -e "\033[36mDry run — no folders deleted.\033[0m"
    exit 0
fi

if [ "$FORCE" = false ]; then
    read -p "Delete all? (Y/N) " confirm
    confirm=$(echo "$confirm" | tr '[:lower:]' '[:upper:]')
    if [[ "$confirm" != "Y" ]]; then
        echo -e "\033[31mCancelled.\033[0m"
        exit 0
    fi
    echo ""
fi

while IFS= read -r folder; do
    rel_path="${folder#$REPO_ROOT/}"
    rm -rf "$folder"
    echo -e "  \033[32mDeleted: $rel_path\033[0m"
done <<< "$folders"

echo ""
echo -e "\033[32mDone — freed $size_str.\033[0m"
