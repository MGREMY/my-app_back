#!/bin/bash

if [ $# -ne 2 ]; then
  echo "Usage: $0 <camelCase_replacement> <PascalCase_replacement>"
  exit 1
fi

CAMEL_CASE=$1
PASCAL_CASE=$2

# Replace in file contents
echo "Replace in file contents"
find . -type f -not -path './.git/*' -exec sed -i "s/myApp/$CAMEL_CASE/g; s/MyApp/$PASCAL_CASE/g" {} +

# Rename files and directories containing MyApp (deepest first)
echo "Rename files and directories containing MyApp (deepest first)"
find . -depth -not -path './.git/*' -name "*MyApp*" -exec bash -c '
  dir=$(dirname "$0")
  base=$(basename "$0")
  newbase=${base//MyApp/$1}
  mv "$0" "$dir/$newbase"
' {} "$PASCAL_CASE" \;

# Rename files and directories containing myApp (deepest first)
echo "Rename files and directories containing myApp (deepest first)"
find . -depth -not -path './.git/*' -name "*myApp*" -exec bash -c '
  dir=$(dirname "$0")
  base=$(basename "$0")
  newbase=${base//myApp/$1}
  mv "$0" "$dir/$newbase"
' {} "$CAMEL_CASE" \;
