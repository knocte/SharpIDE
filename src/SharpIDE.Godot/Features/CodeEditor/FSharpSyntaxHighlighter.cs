using Godot;
using Godot.Collections;

using FSharp.Compiler.Tokenization;
using SharpIDE.Godot.Features.IdeSettings;

namespace SharpIDE.Godot.Features.CodeEditor;
public partial class FSharpSyntaxHighlighter(string source) : SyntaxHighlighter
{
	private SharpIDE.FSharp.FSharpSyntaxHighlighter _highlighter = new() { Source = source };

	private EditorThemeColorSet ColorSetForTheme = EditorThemeColours.Dark;

	public void UpdateThemeColors(LightOrDarkTheme themeType)
	{
		ColorSetForTheme = themeType switch
		{
			LightOrDarkTheme.Light => EditorThemeColours.Light,
			LightOrDarkTheme.Dark => EditorThemeColours.Dark,
			_ => throw new NotImplementedException("Unknown theme type")
		};
	}

	public void SetSource(string source)
	{
		_highlighter.Source = source;
	}

	public override Dictionary _GetLineSyntaxHighlighting(int line)
	{
		var tokens = _highlighter.GetLineSyntaxHighlighting(line);
		var highlights = new Dictionary();
		foreach (var token in tokens)
		{
			var color = ColorSetForTheme.White;
			// Color choices are mostly based on color names, e.g. KeywordBlue for keywords, and also
			// on existing color classification for C# in src\SharpIDE.Godot\Features\CodeEditor\ClassificationToColorMapper.cs.
			switch (token.ColorClass)
			{
				case FSharpTokenColorKind.Keyword: color = ColorSetForTheme.KeywordBlue; break;
				case FSharpTokenColorKind.Comment: color = ColorSetForTheme.CommentGreen; break;
				case FSharpTokenColorKind.Identifier: color = ColorSetForTheme.White; break;
				case FSharpTokenColorKind.InactiveCode: color = ColorSetForTheme.Gray; break;
				case FSharpTokenColorKind.Number: color = ColorSetForTheme.NumberGreen; break;
				case FSharpTokenColorKind.Operator: color = ColorSetForTheme.White; break;
				case FSharpTokenColorKind.PreprocessorKeyword: color = ColorSetForTheme.White; break;
				case FSharpTokenColorKind.Punctuation: color = ColorSetForTheme.White; break;
				case FSharpTokenColorKind.String: color = ColorSetForTheme.LightOrangeBrown; break;
				case FSharpTokenColorKind.UpperIdentifier: color = ColorSetForTheme.ClassGreen; break;
				case FSharpTokenColorKind.Default: break;
			};
			highlights[token.LeftColumn] = new Dictionary() { { "color", color } };
		}

		return highlights;
	}
}
