; Unshipped analyzer release
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category   | Severity | Notes
--------|------------|----------|----------------------------------------------------------------
PAG001  | Pagination | Warning  | Two properties normalize to the same filter/sort name
PAG002  | Pagination | Info     | Extensions class suffixed: two paginated entities share a name
