using System.Collections.Immutable;
using FSharp.Compiler.CodeAnalysis;
using FSharp.Compiler.Text;
using FSharp.Compiler.Tokenization;
using Microsoft.FSharp.Collections;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using SharpIDE.Application.Features.SolutionDiscovery;
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
            checker = FSharpChecker.Create(
                keepAllBackgroundSymbolUses: false,
                keepAllBackgroundParseFileUses: false,
                suggestNamesForErrors: true,
                keepAssemblyContents: false);
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
            var parseOptions = new FSharpParsingOptions(
                sourceFiles: [filePath],
                options: [],
                isInteractive: fileModel.Path.EndsWith(".fsx", StringComparison.OrdinalIgnoreCase));
            
            var parseResults = await checker.ParseFile(
                filePath,
                SourceText.of(sourceText),
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
        
        // Create a lexer for tokenization
        var lexer = FSharpLexer.Create(sourceText, filePath);
        
        foreach (var tokenizedLine in lexer)
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
                if (token.Tag <= FSharpTokenTag.NewToken.NUMERIC_LITERAL)
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
            FSharpTokenTag.NewToken.AND => "fsharp.keyword",
            FSharpTokenTag.NewToken.AS => "fsharp.keyword",
            FSharpTokenTag.NewToken.ASSERT => "fsharp.keyword",
            FSharpTokenTag.NewToken.BASE => "fsharp.keyword",
            FSharpTokenTag.NewToken.BEGIN => "fsharp.keyword",
            FSharpTokenTag.NewToken.CLASS => "fsharp.keyword",
            FSharpTokenTag.NewToken.DEFAULT => "fsharp.keyword",
            FSharpTokenTag.NewToken.DELEGATE => "fsharp.keyword",
            FSharpTokenTag.NewToken.DO => "fsharp.keyword",
            FSharpTokenTag.NewToken.DONE => "fsharp.keyword",
            FSharpTokenTag.NewToken.DOWNTO => "fsharp.keyword",
            FSharpTokenTag.NewToken.ELIF => "fsharp.keyword",
            FSharpTokenTag.NewToken.ELSE => "fsharp.keyword",
            FSharpTokenTag.NewToken.END => "fsharp.keyword",
            FSharpTokenTag.NewToken.EXCEPTION => "fsharp.keyword",
            FSharpTokenTag.NewToken.EXTERN => "fsharp.keyword",
            FSharpTokenTag.NewToken.FALSE => "fsharp.keyword",
            FSharpTokenTag.NewToken.FINALLY => "fsharp.keyword",
            FSharpTokenTag.NewToken.FOR => "fsharp.keyword",
            FSharpTokenTag.NewToken.FUN => "fsharp.keyword",
            FSharpTokenTag.NewToken.FUNCTION => "fsharp.keyword",
            FSharpTokenTag.NewToken.GLOBAL => "fsharp.keyword",
            FSharpTokenTag.NewToken.IF => "fsharp.keyword",
            FSharpTokenTag.NewToken.IN => "fsharp.keyword",
            FSharpTokenTag.NewToken.INHERIT => "fsharp.keyword",
            FSharpTokenTag.NewToken.INLINE => "fsharp.keyword",
            FSharpTokenTag.NewToken.INTERFACE => "fsharp.keyword",
            FSharpTokenTag.NewToken.INTERNAL => "fsharp.keyword",
            FSharpTokenTag.NewToken.LAZY => "fsharp.keyword",
            FSharpTokenTag.NewToken.LET => "fsharp.keyword",
            FSharpTokenTag.NewToken.MATCH => "fsharp.keyword",
            FSharpTokenTag.NewToken.MEMBER => "fsharp.keyword",
            FSharpTokenTag.NewToken.MODULE => "fsharp.keyword",
            FSharpTokenTag.NewToken.MUTABLE => "fsharp.keyword",
            FSharpTokenTag.NewToken.NAMESPACE => "fsharp.keyword",
            FSharpTokenTag.NewToken.NEW => "fsharp.keyword",
            FSharpTokenTag.NewToken.NULL => "fsharp.keyword",
            FSharpTokenTag.NewToken.OPEN => "fsharp.keyword",
            FSharpTokenTag.NewToken.OR => "fsharp.keyword",
            FSharpTokenTag.NewToken.OVERRIDE => "fsharp.keyword",
            FSharpTokenTag.NewToken.PRIVATE => "fsharp.keyword",
            FSharpTokenTag.NewToken.PUBLIC => "fsharp.keyword",
            FSharpTokenTag.NewToken.REC => "fsharp.keyword",
            FSharpTokenTag.NewToken.RETURN => "fsharp.keyword",
            FSharpTokenTag.NewToken.SELECT => "fsharp.keyword",
            FSharpTokenTag.NewToken.STATIC => "fsharp.keyword",
            FSharpTokenTag.NewToken.STRUCT => "fsharp.keyword",
            FSharpTokenTag.NewToken.THEN => "fsharp.keyword",
            FSharpTokenTag.NewToken.TO => "fsharp.keyword",
            FSharpTokenTag.NewToken.TRUE => "fsharp.keyword",
            FSharpTokenTag.NewToken.TRYY => "fsharp.keyword",
            FSharpTokenTag.NewToken.TYPE => "fsharp.keyword",
            FSharpTokenTag.NewToken.UPCAST => "fsharp.keyword",
            FSharpTokenTag.NewToken.USE => "fsharp.keyword",
            FSharpTokenTag.NewToken.VAL => "fsharp.keyword",
            FSharpTokenTag.NewToken.VOID => "fsharp.keyword",
            FSharpTokenTag.NewToken.WHEN => "fsharp.keyword",
            FSharpTokenTag.NewToken.WHILE => "fsharp.keyword",
            FSharpTokenTag.NewToken.WITH => "fsharp.keyword",
            FSharpTokenTag.NewToken.YIELD => "fsharp.keyword",
            FSharpTokenTag.NewToken.AND_BANG => "fsharp.keyword",
            FSharpTokenTag.NewToken.DOT_DOT_DOT => "fsharp.keyword",
            FSharpTokenTag.NewToken.RARROW => "fsharp.keyword",
            FSharpTokenTag.NewToken.COLON_GREATER => "fsharp.keyword",
            FSharpTokenTag.NewToken.DOT_DOT => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_AT_OR_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_BAR_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_COMPARE_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_STAR_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_AMPERSAND_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_OR_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_ADJACENT_OPS => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_APPPLY_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_FUN_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.INFIX_ID => "fsharp.keyword",
            FSharpTokenTag.NewToken.COLON_QMARK => "fsharp.keyword",
            FSharpTokenTag.NewToken.PREFIX_OP => "fsharp.keyword",
            FSharpTokenTag.NewToken.COLON_COLON => "fsharp.keyword",
            FSharpTokenTag.NewToken.LPAREN_STAR_RPAREN => "fsharp.keyword",
            FSharpTokenTag.NewToken.DOT => "fsharp.keyword",
            
            // Identifiers and types
            FSharpTokenTag.NewToken.IDENT => "fsharp.identifier",
            FSharpTokenTag.NewToken.BACKQUOTE_IDENT => "fsharp.identifier",
            FSharpTokenTag.NewToken.LOWER_CASE_IDENT => "fsharp.identifier",
            FSharpTokenTag.NewToken.UPPER_CASE_IDENT => "fsharp.type",
            FSharpTokenTag.NewToken.QUOTED_IDENT => "fsharp.identifier",
            
            // Strings
            FSharpTokenTag.NewToken.STRING_TEXT => "fsharp.string",
            FSharpTokenTag.NewToken.STRING => "fsharp.string",
            FSharpTokenTag.NewToken.VERBATIM_STRING => "fsharp.verbatim.string",
            FSharpTokenTag.NewToken.TRIPLE_QUOTED_STRING => "fsharp.triple-quoted.string",
            FSharpTokenTag.NewToken.CHARACTER => "fsharp.string",
            
            // Numbers
            FSharpTokenTag.NewToken.IEEE32 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.IEEE64 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.DECIMAL => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.INTEGER32 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.INTEGER8 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.INTEGER16 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.INTEGER64 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.UNSIGNED_INTEGER8 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.UNSIGNED_INTEGER16 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.UNSIGNED_INTEGER32 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.UNSIGNED_INTEGER64 => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.BIGNUMBER => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.NATIVEINT => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.UNATIVEINT => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.NEGATIVE_SIZEMARKER => "fsharp.numeric.literal",
            FSharpTokenTag.NewToken.POSITIVE_SIZEMARKER => "fsharp.numeric.literal",
            
            // Comments
            FSharpTokenTag.NewToken.LINE_COMMENT => "fsharp.comment",
            FSharpTokenTag.NewToken.BLOCK_COMMENT => "fsharp.comment",
            FSharpTokenTag.NewToken.DOCCOMMENT => "fsharp.xml.doc.comment",
            FSharpTokenTag.NewToken.DOCCOMMENT_BLOCK => "fsharp.xml.doc.comment",
            
            // Operators and punctuation
            FSharpTokenTag.NewToken.AMPERSAND => "fsharp.operator",
            FSharpTokenTag.NewToken.AMPERSAND_AMPERSAND => "fsharp.operator",
            FSharpTokenTag.NewToken.BAR => "fsharp.operator",
            FSharpTokenTag.NewToken.BAR_BAR => "fsharp.operator",
            FSharpTokenTag.NewToken.COLON => "fsharp.punctuation",
            FSharpTokenTag.NewToken.COLON_COLON => "fsharp.operator",
            FSharpTokenTag.NewToken.COMMA => "fsharp.punctuation",
            FSharpTokenTag.NewToken.DOT => "fsharp.punctuation",
            FSharpTokenTag.NewToken.MINUS => "fsharp.operator",
            FSharpTokenTag.NewToken.PERCENT => "fsharp.operator",
            FSharpTokenTag.NewToken.PLUS => "fsharp.operator",
            FSharpTokenTag.NewToken.PLUS_MINUS => "fsharp.operator",
            FSharpTokenTag.NewToken.SLASH => "fsharp.operator",
            FSharpTokenTag.NewToken.STAR => "fsharp.operator",
            FSharpTokenTag.NewToken.EQUALS => "fsharp.operator",
            FSharpTokenTag.NewToken.LESS => "fsharp.operator",
            FSharpTokenTag.NewToken.GREATER => "fsharp.operator",
            FSharpTokenTag.NewToken.QUESTION_MARK => "fsharp.operator",
            FSharpTokenTag.NewToken.AT => "fsharp.operator",
            FSharpTokenTag.NewToken.TILDE => "fsharp.operator",
            FSharpTokenTag.NewToken.CARET => "fsharp.operator",
            FSharpTokenTag.NewToken.EXCLAIM => "fsharp.operator",
            FSharpTokenTag.NewToken.EXCLAIM_EQUAL => "fsharp.operator",
            FSharpTokenTag.NewToken.DOT_LESS => "fsharp.operator",
            FSharpTokenTag.NewToken.GREATER_DOT => "fsharp.operator",
            FSharpTokenTag.NewToken.DOT_DOT_DOT => "fsharp.punctuation",
            FSharpTokenTag.NewToken.LPAREN => "fsharp.punctuation",
            FSharpTokenTag.NewToken.RPAREN => "fsharp.punctuation",
            FSharpTokenTag.NewToken.LBRACKET => "fsharp.punctuation",
            FSharpTokenTag.NewToken.RBRACKET => "fsharp.punctuation",
            FSharpTokenTag.NewToken.LBRACE => "fsharp.punctuation",
            FSharpTokenTag.NewToken.RBRACE => "fsharp.punctuation",
            FSharpTokenTag.NewToken.LBRACKET_BAR => "fsharp.punctuation",
            FSharpTokenTag.NewToken.BAR_RBRACKET => "fsharp.punctuation",
            FSharpTokenTag.NewToken.LESS_DOT_DOT_DOT => "fsharp.punctuation",
            FSharpTokenTag.NewToken.DOT_DOT_DOT_GREATER => "fsharp.punctuation",
            FSharpTokenTag.NewToken.SEMICOLON => "fsharp.punctuation",
            FSharpTokenTag.NewToken.SEMICOLON_SEMICOLON => "fsharp.punctuation",
            FSharpTokenTag.NewToken.COMPLEMENT => "fsharp.operator",
            FSharpTokenTag.NewToken.INFIX_CARET_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_STAR_STAR_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_AMPERSAND_AMPERSAND_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_BAR_BAR_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_EQUAL_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_GREATER_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_LESS_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_AMPERSAND_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_OR_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_PLUS_MINUS_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.INFIX_STAR_DIV_MOD_OP => "fsharp.infix-operator",
            FSharpTokenTag.NewToken.PREFIX_MINUS_OP => "fsharp.operator",
            FSharpTokenTag.NewToken.PREFIX_PLUS_OP => "fsharp.operator",
            FSharpTokenTag.NewToken.INFIX_COMPARISON_OP => "fsharp.infix-operator",
            
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
