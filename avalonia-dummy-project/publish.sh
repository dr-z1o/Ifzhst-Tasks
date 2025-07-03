#!/bin/bash

set -e

APP_NAME="avalonia-dummy-project"
APP_BUNDLE_NAME="${APP_NAME}.app"
CONFIGURATION="Release"
FRAMEWORK="net8.0"
BASE_OUTPUT_DIR="publish"
RUNTIMES=("osx-x64" "osx-arm64")
CERT_NAME="Dmitry D"

echo "Building Dummy App for macOS (x64 & arm64)..."

for RUNTIME in "${RUNTIMES[@]}"; do
    echo "Build $RUNTIME..."

    PUBLISH_DIR="${BASE_OUTPUT_DIR}/${RUNTIME}/raw"
    APP_DIR="${BASE_OUTPUT_DIR}/${RUNTIME}/${APP_BUNDLE_NAME}"
    APP_BIN_DIR="${APP_DIR}/Contents/MacOS"
    APP_RESOURCES_DIR="${APP_DIR}/Contents/Resources"
    APP_BIN="${APP_BIN_DIR}/${APP_NAME}"
    APP_PLIST="${APP_DIR}/Contents/Info.plist"

     # Step 1: publishing the app
    dotnet publish -c $CONFIGURATION \
        -r $RUNTIME \
        --self-contained true \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:EnableCompressionInSingleFile=true \
        -o "$PUBLISH_DIR"
 
    mkdir -p "${APP_BIN_DIR}"
    mkdir -p "${APP_RESOURCES_DIR}"

    # Step 2: Copying the binary to .app
    cp -R -v "${PUBLISH_DIR}/" "${APP_BIN_DIR}/"
    chmod +x "${APP_BIN}"

    # Step 3: Generate Info.plist
    cat > "$APP_PLIST" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key>
    <string>${APP_NAME}</string>
    <key>CFBundleExecutable</key>
    <string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key>
    <string>com.example.${APP_NAME}</string>
    <key>CFBundleVersion</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
</dict>
</plist>
EOF

    # Step 4: Sign .app
    echo "Signing .app using cert: \"$CERT_NAME\"..."
    codesign --force --deep --timestamp --sign "$CERT_NAME" --options=runtime "$APP_DIR"

    # Step 5: Pack .zip
    ZIP_NAME="${BASE_OUTPUT_DIR}/${APP_NAME}-${RUNTIME}.zip"
    echo "Packing $ZIP_NAME..."
    cd "${BASE_OUTPUT_DIR}/${RUNTIME}"
    zip -r -q "../$(basename "$ZIP_NAME")" "${APP_BUNDLE_NAME}"
    cd - > /dev/null
done

echo "Done! The apps are located in ${BASE_OUTPUT_DIR}."
ls -lh ${BASE_OUTPUT_DIR}/*.zip