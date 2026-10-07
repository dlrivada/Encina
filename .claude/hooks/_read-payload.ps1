# Shared by every hook: reads the hook payload (JSON) from stdin as UTF-8 (#1926).
#
# `[Console]::In.ReadToEnd()` decodes with [Console]::InputEncoding, which is the OEM code page (for example 850)
# when Claude Code runs the hook on Windows, so a non-ASCII value such as the em dash of a milestone title
# ("v0.22.0 — Release Engineering") reached the checks garbled. Claude Code writes the payload as UTF-8, so the
# bytes are read from the raw stdin stream and decoded explicitly, whatever the console code page is.

function Read-HookStdin {
    $stream = [Console]::OpenStandardInput()
    $reader = [System.IO.StreamReader]::new($stream, [System.Text.UTF8Encoding]::new($false), $true)
    try { return $reader.ReadToEnd() }
    finally { $reader.Dispose() }
}
