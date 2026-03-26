using System.Collections.Immutable;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Text;
using FSharp.Compiler.Tokenization;
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
            checker = FSharpChecker.Create();
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
            var project = fileModel.GetNearestProjectNode();
            var projectDirectory = project?.FilePath != null 
                ? System.IO.Path.GetDirectoryName(project.FilePath)! 
                : System.IO.Path.GetDirectoryName(fileModel.Path)!;
            
            var checker = GetOrCreateChecker(projectDirectory);
            
            // Get source text
            var sourceText = await System.IO.File.ReadAllTextAsync(fileModel.Path, cancellationToken);
            var filePath = fileModel.Path;
            
            // Parse the file to get token information
            var parseOptions = new FSharpParsingOptions(
                sourceFiles: [filePath],
                isInteractive: fileModel.Path.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase));
            
            var parseResults = await checker.ParseFile(
                filePath,
                FSharp.Compiler.Text.SourceText.of(sourceText),
                parseOptions,
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
        
        // Create a tokenizer for tokenization
        var tokenizer = FSharpTokenizationLanguage.Create(sourceText);
        
        foreach (var tokenizedLine in tokenizer.Lines)
        {
            var lineNumber = tokenizedLine.LineNumber;
            if (lineNumber < 0 || lineNumber >= lines.Length)
                continue;
            
            var line = lines[lineNumber];
            
            // Calculate line start offset
            var lineStartOffset = 0;
            for (int i = 0; i < lineNumber; i++)
            {
                lineStartOffset += lines[i].Length + 1; // +1 for newline
            }
            
            foreach (var token in tokenizedLine.Tokens)
            {
                if (token.Tag <= FSharpTokenTag.NUMERIC_LITERAL)
                    continue; // Skip trivia/special tokens
                
                var classificationType = GetClassificationType(token.Tag);
                if (classificationType == null)
                    continue;
                
                // Get absolute positions
                var absoluteStart = lineStartOffset + token.Span.narrowingStart;
                var absoluteEnd = lineStartOffset + token.Span.narrowingEnd + 1;
                
                if (absoluteStart >= sourceText.Length || absoluteEnd > sourceText.Length)
                    continue;
                
                var columnIndex = token.Span.narrowingStart;
                var length = token.Span.narrowingEnd - token.Span.narrowingStart + 1;
                
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
        // Map F# token tags to classification types
        return tag switch
        {
            // Keywords
            FSharpTokenTag.AND => "fsharp.keyword",
            FSharpTokenTag.AS => "fsharp.keyword",
            FSharpTokenTag.ASSERT => "fsharp.keyword",
            FSharpTokenTag.BASE => "fsharp.keyword",
            FSharpTokenTag.BEGIN => "fsharp.keyword",
            FSharpTokenTag.CLASS => "fsharp.keyword",
            FSharpTokenTag.DEFAULT => "fsharp.keyword",
            FSharpTokenTag.DELEGATE => "fsharp.keyword",
            FSharpTokenTag.DO => "fsharp.keyword",
            FSharpTokenTag.DONE => "fsharp.keyword",
            FSharpTokenTag.DOWNTO => "fsharp.keyword",
            FSharpTokenTag.ELIF => "fsharp.keyword",
            FSharpTokenTag.ELSE => "fsharp.keyword",
            FSharpTokenTag.END => "fsharp.keyword",
            FSharpTokenTag.EXCEPTION => "fsharp.keyword",
            FSharpTokenTag.EXTERN => "fsharp.keyword",
            FSharpTokenTag.FALSE => "fsharp.keyword",
            FSharpTokenTag.FINALLY => "fsharp.keyword",
            FSharpTokenTag.FOR => "fsharp.keyword",
            FSharpTokenTag.FUN => "fsharp.keyword",
            FSharpTokenTag.FUNCTION => "fsharp.keyword",
            FSharpTokenTag.GLOBAL => "fsharp.keyword",
            FSharpTokenTag.IF => "fsharp.keyword",
            FSharpTokenTag.IN => "fsharp.keyword",
            FSharpTokenTag.INHERIT => "fsharp.keyword",
            FSharpTokenTag.INLINE => "fsharp.keyword",
            FSharpTokenTag.INTERFACE => "fsharp.keyword",
            FSharpTokenTag.INTERNAL => "fsharp.keyword",
            FSharpTokenTag.LAZY => "fsharp.keyword",
            FSharpTokenTag.LET => "fsharp.keyword",
            FSharpTokenTag.MATCH => "fsharp.keyword",
            FSharpTokenTag.MEMBER => "fsharp.keyword",
            FSharpTokenTag.MODULE => "fsharp.keyword",
            FSharpTokenTag.MUTABLE => "fsharp.keyword",
            FSharpTokenTag.NAMESPACE => "fsharp.keyword",
            FSharpTokenTag.NEW => "fsharp.keyword",
            FSharpTokenTag.NULL => "fsharp.keyword",
            FSharpTokenTag.OPEN => "fsharp.keyword",
            FSharpTokenTag.OR => "fsharp.keyword",
            FSharpTokenTag.OVERRIDE => "fsharp.keyword",
            FSharpTokenTag.PRIVATE => "fsharp.keyword",
            FSharpTokenTag.PUBLIC => "fsharp.keyword",
            FSharpTokenTag.REC => "fsharp.keyword",
            FSharpTokenTag.RETURN => "fsharp.keyword",
            FSharpTokenTag.SELECT => "fsharp.keyword",
            FSharpTokenTag.STATIC => "fsharp.keyword",
            FSharpTokenTag.STRUCT => "fsharp.keyword",
            FSharpTokenTag.THEN => "fsharp.keyword",
            FSharpTokenTag.TO => "fsharp.keyword",
            FSharpTokenTag.TRUE => "fsharp.keyword",
            FSharpTokenTag.TRYY => "fsharp.keyword",
            FSharpTokenTag.TYPE => "fsharp.keyword",
            FSharpTokenTag.UPCAST => "fsharp.keyword",
            FSharpTokenTag.USE => "fsharp.keyword",
            FSharpTokenTag.VAL => "fsharp.keyword",
            FSharpTokenTag.VOID => "fsharp.keyword",
            FSharpTokenTag.WHEN => "fsharp.keyword",
            FSharpTokenTag.WHILE => "fsharp.keyword",
            FSharpTokenTag.WITH => "fsharp.keyword",
            FSharpTokenTag.YIELD => "fsharp.keyword",
            FSharpTokenTag.AND_BANG => "fsharp.keyword",
            FSharpTokenTag.DOT_DOT_DOT => "fsharp.keyword",
            FSharpTokenTag.RARROW => "fsharp.keyword",
            FSharpTokenTag.COLON_GREATER => "fsharp.keyword",
            FSharpTokenTag.DOT_DOT => "fsharp.keyword",
            FSharpTokenTag.INFIX_AT_OR_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_BAR_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_COMPARE_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_STAR_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_AMPERSAND_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_OR_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_ADJACENT_OPS => "fsharp.keyword",
            FSharpTokenTag.INFIX_APPLY_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_FUN_OP => "fsharp.keyword",
            FSharpTokenTag.INFIX_ID => "fsharp.keyword",
            FSharpTokenTag.COLON_QMARK => "fsharp.keyword",
            FSharpTokenTag.PREFIX_OP => "fsharp.keyword",
            FSharpTokenTag.COLON_COLON => "fsharp.keyword",
            FSharpTokenTag.LPAREN_STAR_RPAREN => "fsharp.keyword",
            FSharpTokenTag.DOT => "fsharp.keyword",
            
            // Identifiers and types
            FSharpTokenTag.IDENT => "fsharp.identifier",
            FSharpTokenTag.BACKQUOTE_IDENT => "fsharp.identifier",
            FSharpTokenTag.LOWER_CASE_IDENT => "fsharp.identifier",
            FSharpTokenTag.UPPER_CASE_IDENT => "fsharp.type",
            FSharpTokenTag.QUOTED_IDENT => "fsharp.identifier",
            
            // Strings
            FSharpTokenTag.STRING_TEXT => "fsharp.string",
            FSharpTokenTag.STRING => "fsharp.string",
            FSharpTokenTag.VERBATIM_STRING => "fsharp.verbatim.string",
            FSharpTokenTag.TRIPLE_QUOTED_STRING => "fsharp.triple-quoted.string",
            FSharpTokenTag.CHARACTER => "fsharp.string",
            
            // Numbers
            FSharpTokenTag.IEEE32 => "fsharp.numeric.literal",
            FSharpTokenTag.IEEE64 => "fsharp.numeric.literal",
            FSharpTokenTag.DECIMAL => "fsharp.numeric.literal",
            FSharpTokenTag.INTEGER32 => "fsharp.numeric.literal",
            FSharpTokenTag.INTEGER8 => "fsharp.numeric.literal",
            FSharpTokenTag.INTEGER16 => "fsharp.numeric.literal",
            FSharpTokenTag.INTEGER64 => "fsharp.numeric.literal",
            FSharpTokenTag.UNSIGNED_INTEGER8 => "fsharp.numeric.literal",
            FSharpTokenTag.UNSIGNED_INTEGER16 => "fsharp.numeric.literal",
            FSharpTokenTag.UNSIGNED_INTEGER32 => "fsharp.numeric.literal",
            FSharpTokenTag.UNSIGNED_INTEGER64 => "fsharp.numeric.literal",
            FSharpTokenTag.BIGNUMBER => "fsharp.numeric.literal",
            FSharpTokenTag.NATIVEINT => "fsharp.numeric.literal",
            FSharpTokenTag.UNATIVEINT => "fsharp.numeric.literal",
            FSharpTokenTag.NEGATIVE_SIZEMARKER => "fsharp.numeric.literal",
            FSharpTokenTag.POSITIVE_SIZEMARKER => "fsharp.numeric.literal",
            
            // Comments
            FSharpTokenTag.LINE_COMMENT => "fsharp.comment",
            FSharpTokenTag.BLOCK_COMMENT => "fsharp.comment",
            FSharpTokenTag.DOCCOMMENT => "fsharp.xml.doc.comment",
            FSharpTokenTag.DOCCOMMENT_BLOCK => "fsharp.xml.doc.comment",
            
            // Operators and punctuation
            FSharpTokenTag.AMPERSAND => "fsharp.operator",
            FSharpTokenTag.AMPERSAND_AMPERSAND => "fsharp.operator",
            FSharpTokenTag.BAR => "fsharp.operator",
            FSharpTokenTag.BAR_BAR => "fsharp.operator",
            FSharpTokenTag.COLON => "fsharp.punctuation",
            FSharpTokenTag.COLON_COLON => "fsharp.operator",
            FSharpTokenTag.COMMA => "fsharp.punctuation",
            FSharpTokenTag.DOT => "fsharp.punctuation",
            FSharpTokenTag.MINUS => "fsharp.operator",
            FSharpTokenTag.PERCENT => "fsharp.operator",
            FSharpTokenTag.PLUS => "fsharp.operator",
            FSharpTokenTag.PLUS_MINUS => "fsharp.operator",
            FSharpTokenTag.SLASH => "fsharp.operator",
            FSharpTokenTag.STAR => "fsharp.operator",
            FSharpTokenTag.EQUALS => "fsharp.operator",
            FSharpTokenTag.LESS => "fsharp.operator",
            FSharpTokenTag.GREATER => "fsharp.operator",
            FSharpTokenTag.QUESTION_MARK => "fsharp.operator",
            FSharpTokenTag.AT => "fsharp.operator",
            FSharpTokenTag.TILDE => "fsharp.operator",
            FSharpTokenTag.CARET => "fsharp.operator",
            FSharpTokenTag.EXCLAIM => "fsharp.operator",
            FSharpTokenTag.EXCLAIM_EQUAL => "fsharp.operator",
            FSharpTokenTag.DOT_LESS => "fsharp.operator",
            FSharpTokenTag.GREATER_DOT => "fsharp.operator",
            FSharpTokenTag.DOT_DOT_DOT => "fsharp.punctuation",
            FSharpTokenTag.LPAREN => "fsharp.punctuation",
            FSharpTokenTag.RPAREN => "fsharp.punctuation",
            FSharpTokenTag.LBRACKET => "fsharp.punctuation",
            FSharpTokenTag.RBRACKET => "fsharp.punctuation",
            FSharpTokenTag.LBRACE => "fsharp.punctuation",
            FSharpTokenTag.RBRACE => "fsharp.punctuation",
            FSharpTokenTag.LBRACKET_BAR => "fsharp.punctuation",
            FSharpTokenTag.BAR_RBRACKET => "fsharp.punctuation",
            FSharpTokenTag.LESS_DOT_DOT_DOT => "fsharp.punctuation",
            FSharpTokenTag.DOT_DOT_DOT_GREATER => "fsharp.punctuation",
            FSharpTokenTag.SEMICOLON => "fsharp.punctuation",
            FSharpTokenTag.SEMICOLON_SEMICOLON => "fsharp.punctuation",
            FSharpTokenTag.COMPLEMENT => "fsharp.operator",
            FSharpTokenTag.INFIX_CARET_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_STAR_STAR_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_AMPERSAND_AMPERSAND_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_BAR_BAR_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_EQUAL_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_GREATER_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_LESS_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_AMPERSAND_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_OR_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_PLUS_MINUS_OP => "fsharp.infix-operator",
            FSharpTokenTag.INFIX_STAR_DIV_MOD_OP => "fsharp.infix-operator",
            FSharpTokenTag.PREFIX_MINUS_OP => "fsharp.operator",
            FSharpTokenTag.PREFIX_PLUS_OP => "fsharp.operator",
            FSharpTokenTag.INFIX_COMPARISON_OP => "fsharp.infix-operator",
            
            _ => null
        };
    }
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
