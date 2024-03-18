MessageIdTypedef=DWORD

SeverityNames=(
  Success=0x0
  Informational=0x1
  Warning=0x2
  Error=0x3
)

FacilityNames=(
  System=0x0FF
  Application=0xFFF
)

LanguageNames=(English=0x409:MSG00409)


; // The following are message definitions.

MessageId=1000
Severity=Warning
Facility=Application
SymbolicName=ERR_SERIAL_PORT
Language=English
Serial port %1 (%2) encountered an error:
%3
.
