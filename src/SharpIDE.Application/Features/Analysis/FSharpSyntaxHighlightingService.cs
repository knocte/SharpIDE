using System.Collections.Immutable;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Text;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using SharpIDE.Application.Features.SolutionDiscovery;
using SharpIDE.Application.Features.SolutionDiscovery.VsPersistence;
using SharpIDE.Application.Features.Analysis;

namespace SharpIDE.Application.Features.Analysis;

/// <summary>
/// Provides F# syntax highlighting using the F# Compiler Service.
/// </summary>
public class FSharpSyntaxHighlightingService(ILogger<FSharpSyntaxHighlightingService> logger)
{
    private readonly ILogger<FSharpSyntaxHighlightingService> _logger = logger;
    
    // Cache of checker instances by project directory
    private readonly Dictionary<string, FSharpChecker> _checkersByProject = new();
    
    private FSharpChecker GetOrCreateChecker(string projectDirectory)
    {
        if (!_checkersByProject.TryGetValue(projectDirectory, out var checker))
        {
            checker = FSharpChecker.Create(projectCacheSize: 1);
            _checkersByProject[projectDirectory] = checker;
        }
        return checker;
    }

    /// <summary>
    /// Gets syntax highlighting classified spans for an F# file.
    /// </summary>
    public async Task<ImmutableArray<SharpIdeFSharpClassifiedSpan>> GetFSharpDocumentSyntaxHighlightingAsync(
        SharpIdeFile fileModel,
        CancellationToken cancellationToken = default)
    {
        using var _ = SharpIdeOtel.Source.StartActivity($"{nameof(FSharpSyntaxHighlightingService)}.{nameof(GetFSharpDocumentSyntaxHighlightingAsync)}");
        
        if (!fileModel.IsFsharpFile)
        {
            return [];
        }

        var timer = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            var project = ((IChildSharpIdeNode)fileModel).GetNearestProjectNode();
            var projectDirectory = project?.FilePath != null 
                ? System.IO.Path.GetDirectoryName(project.FilePath)! 
                : System.IO.Path.GetDirectoryName(fileModel.Path)!;
            
            var checker = GetOrCreateChecker(projectDirectory);
            
            // Get source text
            var sourceText = await System.IO.File.ReadAllTextAsync(fileModel.Path, cancellationToken);
            var filePath = fileModel.Path;
            
            // Parse the file to get token information
            var parsingOptions = FSharpParsingOptions.Default;
            var fullSourceText = new FSharpSourceText(sourceText);
            
            var parseResults = await checker.ParseFile(
                filePath,
                fullSourceText,
                parsingOptions,
                cancellationToken: cancellationToken);
            
            if (parseResults.ParseTree == null)
            {
                _logger.LogWarning("F# parse tree was null for file: {FilePath}", fileModel.Path);
                return [];
            }

            // Get classified spans from the parsed tokens
            var classifiedSpans = ConvertFSharpTokensToClassifiedSpans(
                sourceText,
                filePath);
            
            timer.Stop();
            _logger.LogInformation(
                "F# syntax highlighting for {FileName} took {ElapsedMilliseconds}ms with {TokenCount} tokens",
                fileModel.Name.Value,
                timer.ElapsedMilliseconds,
                classifiedSpans.Length);
            
            return classifiedSpans.ToImmutableArray();
        }
        catch (Exception ex)
        {
            timer.Stop();
            _logger.LogError(ex, "Error getting F# syntax highlighting for file: {FilePath}", fileModel.Path);
            return [];
        }
    }

    private ImmutableArray<SharpIdeFSharpClassifiedSpan> ConvertFSharpTokensToClassifiedSpans(
        string sourceText,
        string filePath)
    {
        var spans = new List<SharpIdeFSharpClassifiedSpan>();
        var lines = sourceText.Split('\n');
        
        // Use FSharpLineTokenizer for tokenization
        var tokenizer = new FSharpLineTokenizer();
        
        for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
        {
            var line = lines[lineNumber];
            
            // Calculate line start offset
            var lineStartOffset = 0;
            for (int i = 0; i < lineNumber; i++)
            {
                lineStartOffset += lines[i].Length + 1; // +1 for newline
            }
            
            var tokens = tokenizer.TokenizeLine(line, filePath, lineNumber);
            
            foreach (var token in tokens)
            {
                var classificationType = GetClassificationType(token.Tag);
                if (classificationType == null)
                    continue;
                
                var columnIndex = token.StartIndex;
                var length = token.Length;
                
                // Create a file span with line and column info
                var fileSpan = new LinePositionSpan(
                    new LinePosition(lineNumber, columnIndex),
                    new LinePosition(lineNumber, columnIndex + length));
                
                var classifiedSpan = new ClassifiedSpan(classificationType, new TextSpan(columnIndex, length));
                
                spans.Add(new SharpIdeFSharpClassifiedSpan(fileSpan, classifiedSpan, lineNumber));
            }
        }
        
        return spans.ToImmutableArray();
    }

    private static string? GetClassificationType(int tag)
    {
        // Map F# token tags to classification types using raw int values
        // Based on FSharpTokenTag enum values from FSharp.Compiler.Tokenization
        return tag switch
        {
            // Keywords (roughly 1-200 range)
            >= 1 and <= 200 when IsKeywordTag(tag) => "fsharp.keyword",
            
            // Identifiers
            305 or 306 or 307 or 308 => "fsharp.identifier",
            309 => "fsharp.type", // UPPER_CASE_IDENT
            
            // Strings
            >= 350 and <= 360 => "fsharp.string",
            
            // Numbers
            >= 400 and <= 450 => "fsharp.numeric.literal",
            
            // Comments
            260 or 261 => "fsharp.comment",
            262 or 263 => "fsharp.xml.doc.comment",
            
            // Operators
            >= 500 and <= 600 => "fsharp.operator",
            
            // Punctuation
            >= 600 and <= 700 => "fsharp.punctuation",
            
            _ => null
        };
    }
    
    private static bool IsKeywordTag(int tag) =>
        tag switch
        {
            1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20
            or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or 30 or 31 or 32 or 33 or 34 or 35 or 36 or 37 or 38 or 39 or 40
            or 41 or 42 or 43 or 44 or 45 or 46 or 47 or 48 or 49 or 50 or 51 or 52 or 53 or 54 or 55 or 56 or 57 or 58 or 59 or 60
            or 61 or 62 or 63 or 64 or 65 or 66 or 67 or 68 or 69 or 70 or 71 or 72 or 73 or 74 or 75 or 76 or 77 or 78 or 79 or 80
            or 81 or 82 or 83 or 84 or 85 or 86 or 87 or 88 or 89 or 90 or 91 or 92 or 93 or 94 or 95 or 96 or 97 or 98 or 99 or 100
            or 101 or 102 or 103 or 104 or 105 or 106 or 107 or 108 or 109 or 110 or 111 or 112 or 113 or 114 or 115 or 116 or 117 or 118 or 119 or 120
            or 121 or 122 or 123 or 124 or 125 or 126 or 127 or 128 or 129 or 130 or 131 or 132 or 133 or 134 or 135 or 136 or 137 or 138 or 139 or 140
            or 141 or 142 or 143 or 144 or 145 or 146 or 147 or 148 or 149 or 150 or 151 or 152 or 153 or 154 or 155 or 156 or 157 or 158 or 159 or 160
            or 161 or 162 or 163 or 164 or 165 or 166 or 167 or 168 or 169 or 170 or 171 or 172 or 173 or 174 or 175 or 176 or 177 or 178 or 179 or 180
            or 181 or 182 or 183 or 184 or 185 or 186 or 187 or 188 or 189 or 190 or 191 or 192 or 193 or 194 or 195 or 196 or 197 or 198 or 199 or 200 => true,
            _ => false
        };
}

/// <summary>
/// Simple token structure for F# syntax highlighting.
/// </summary>
internal readonly record struct FSharpToken(int Tag, int StartIndex, int Length);

/// <summary>
/// Simple line tokenizer for F# code.
/// </summary>
internal class FSharpLineTokenizer
{
    public IEnumerable<FSharpToken> TokenizeLine(string line, string filePath, int lineNumber)
    {
        var tokens = new List<FSharpToken>();
        int i = 0;
        
        while (i < line.Length)
        {
            char c = line[i];
            
            // Skip whitespace
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }
            
            // Comments
            if (c == '/' && i + 1 < line.Length && line[i + 1] == '/')
            {
                tokens.Add(new FSharpToken(260 /* LINE_COMMENT */, i, line.Length - i));
                break;
            }
            
            // Strings
            if (c == '"')
            {
                int start = i;
                i++;
                while (i < line.Length && line[i] != '"')
                {
                    if (line[i] == '\\' && i + 1 < line.Length)
                        i++;
                    i++;
                }
                if (i < line.Length) i++; // closing quote
                tokens.Add(new FSharpToken(350 /* STRING */, start, i - start));
                continue;
            }
            
            // Identifiers and keywords
            if (char.IsLetter(c) || c == '_')
            {
                int start = i;
                while (i < line.Length && (char.IsLetterOrDigit(line[i]) || line[i] == '_'))
                    i++;
                int tag = GetIdentifierTag(line[start..i]);
                tokens.Add(new FSharpToken(tag, start, i - start));
                continue;
            }
            
            // Numbers
            if (char.IsDigit(c) || (c == '.' && i + 1 < line.Length && char.IsDigit(line[i + 1])))
            {
                int start = i;
                while (i < line.Length && (char.IsDigit(line[i]) || line[i] == '.' || line[i] == 'e' || line[i] == 'E' || line[i] == 'f' || line[i] == 'F' || line[i] == 'u' || line[i] == 'U' || line[i] == 'l' || line[i] == 'L' || line[i] == 'n'))
                    i++;
                tokens.Add(new FSharpToken(400 /* INTEGER */, start, i - start));
                continue;
            }
            
            // Operators and punctuation
            int opStart = i;
            // Handle multi-char operators
            if (i + 1 < line.Length)
            {
                string twoChar = line.Substring(i, 2);
                if (twoChar is "->" or "<-" or "||" or "&&" or "::" or "<>" or "<=" or ">=" or "!=" or "**" or "??" or "+=" or "-=" or "*=" or "/=" or ">>" or "<<")
                {
                    tokens.Add(new FSharpToken(500 /* OPERATOR */, opStart, 2));
                    i += 2;
                    continue;
                }
            }
            tokens.Add(new FSharpToken(500 /* OPERATOR */, i, 1));
            i++;
        }
        
        return tokens;
    }
    
    private static int GetIdentifierTag(string identifier) =>
        identifier.ToLowerInvariant() switch
        {
            "and" => 1,
            "as" => 2,
            "assert" => 3,
            "base" => 4,
            "begin" => 5,
            "class" => 6,
            "default" => 7,
            "delegate" => 8,
            "do" => 9,
            "done" => 10,
            "downto" => 11,
            "elif" => 12,
            "else" => 13,
            "end" => 14,
            "exception" => 15,
            "extern" => 16,
            "false" => 17,
            "finally" => 18,
            "for" => 19,
            "fun" => 20,
            "function" => 21,
            "global" => 22,
            "if" => 23,
            "in" => 24,
            "inherit" => 25,
            "inline" => 26,
            "interface" => 27,
            "internal" => 28,
            "lazy" => 29,
            "let" => 30,
            "match" => 31,
            "member" => 32,
            "module" => 33,
            "mutable" => 34,
            "namespace" => 35,
            "new" => 36,
            "null" => 37,
            "open" => 38,
            "or" => 39,
            "override" => 40,
            "private" => 41,
            "public" => 42,
            "rec" => 43,
            "return" => 44,
            "select" => 45,
            "static" => 46,
            "struct" => 47,
            "then" => 48,
            "to" => 49,
            "true" => 50,
            "try" => 51,
            "type" => 52,
            "upcast" => 53,
            "use" => 54,
            "val" => 55,
            "void" => 56,
            "when" => 57,
            "where" => 58,
            "while" => 59,
            "with" => 60,
            "yield" => 61,
            _ => char.IsUpper(identifier[0]) ? 309 : 305 // UPPER_CASE_IDENT or LOWER_CASE_IDENT
        };
}

/// <summary>
/// Represents an F# classified span for syntax highlighting, with line information.
/// </summary>
/// <param name="FileSpan">The line position span in the file</param>
/// <param name="ClassifiedSpan">The Roslyn classified span with classification type</param>
/// <param name="LineIndex">The zero-based line index</param>
public readonly record struct SharpIdeFSharpClassifiedSpan(
    LinePositionSpan FileSpan, 
    ClassifiedSpan ClassifiedSpan,
    int LineIndex);
