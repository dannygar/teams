REM --- Move to this scripts location ---
pushd "%~dp0"

REM --- Print out environment variables for debugging ---
set

REM --- Ensure the VC_redist is installed for the Microsoft.Skype.Bots.Media Library ---
.\VC_redist.x64.exe /quiet /norestart

REM --- Delete existing certificate bindings and URL ACL registrations ---
netsh http delete sslcert ipport=0.0.0.0:9441
netsh http delete sslcert ipport=0.0.0.0:8445
netsh http delete urlacl url=https://+:8445/
netsh http delete urlacl url=https://+:9441/

REM --- Add new URL ACLs and certificate bindings ---
netsh http add urlacl url=https://+:8445/ sddl=D:(A;;GX;;;S-1-1-0)
netsh http add urlacl url=https://+:9441/ sddl=D:(A;;GX;;;S-1-1-0)
netsh http add sslcert ipport=0.0.0.0:9441 certhash=f301c896f6c2fd344ea7cdff88c86b593ef21dc2 appid={91933591-da15-4c8d-9ad6-b77a47c9ba3c}
netsh http add sslcert ipport=0.0.0.0:8445 certhash=f301c896f6c2fd344ea7cdff88c86b593ef21dc2 appid={91933591-da15-4c8d-9ad6-b77a47c9ba3c}

REM --- Run this command in PowerShell Admin Windows Consule
.\configure_cloud.ps1 -p .\PolicyRecordingBot\ -dns <domain> -cn <domain> -thumb <thumbprint> -bid <bot display name> -aid <app client id> -as <app secret>

popd
exit /b 0