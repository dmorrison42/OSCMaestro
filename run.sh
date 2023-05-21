#!/bin/sh

dotnet publish --property:Configuration=Release -r osx.11.0-x64 --self-contained true
./bin/Release/net7.0/osx.11.0-x64/publish/osctree --verbose