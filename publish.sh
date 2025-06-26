#!/bin/bash

set -e

APP_NAME="AvaloniaDummyApp"
CONFIGURATION="Release"
FRAMEWORK="net8.0"
BASE_OUTPUT_DIR="publish"
RUNTIMES=("osx-x64" "osx-arm64")

echo "Building Dummy App for macOS (x64 & arm64)..."

for RUNTIME in "${RUNTIMES[@]}"; do
    OUTPUT_DIR="${BASE_OUTPUT_DIR}/${RUNTIME}"

    echo "Build $RUNTIME..."

    dotnet publish -c $CONFIGURATION \
        -r $RUNTIME \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        -o "$OUTPUT_DIR"

    chmod +x "$OUTPUT_DIR/$APP_NAME" || true

    ZIP_NAME="${BASE_OUTPUT_DIR}/${APP_NAME}-${RUNTIME}.zip"

    echo "Zip to $ZIP_NAME..."
    cd "$OUTPUT_DIR"
    zip -r -q "../${APP_NAME}-${RUNTIME}.zip" ./*
    cd - > /dev/null
done

echo "Done"
echo "Output directory: ${BASE_OUTPUT_DIR}"
ls -lh ${BASE_OUTPUT_DIR}/*.zip