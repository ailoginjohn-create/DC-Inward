# Cross-publish the WPF app as a self-contained single-file Windows executable.
# Run from the repository root. Requires .NET SDK 8.0 (any platform).

set -e
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 TERM=xterm LC_ALL=C
export PATH="$PATH:/root/.dotnet/tools"

OUT="${1:-/tmp/inwarddc-publish}"
RID="${2:-win-x64}"

echo "Publishing InwardDC ($RID) to $OUT ..."
dotnet publish src/InwardDC.App/InwardDC.App.csproj \
  -c Release -r "$RID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o "$OUT"

echo "Done. Output:"
ls -la "$OUT" | grep -E "InwardDC\.exe|\.dll$"
