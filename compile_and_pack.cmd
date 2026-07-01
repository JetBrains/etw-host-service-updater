:<<"::CMDLITERAL"
@ECHO off
SETLOCAL EnableExtensions

CALL ".\dotnet-sdk.cmd" run --file compile_and_pack.cs %*
EXIT /B %ERRORLEVEL%
::CMDLITERAL

set -eu

exec ./dotnet-sdk.cmd run --file compile_and_pack.cs "$@"
