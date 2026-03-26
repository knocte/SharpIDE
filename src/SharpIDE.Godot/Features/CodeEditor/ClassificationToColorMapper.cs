using Godot;

namespace SharpIDE.Godot.Features.CodeEditor;

public static class ClassificationToColorMapper
{
    public static Color GetColorForClassification(EditorThemeColorSet editorThemeColorSet, string classificationType)
    {
        var colour = classificationType switch
        {
            // C# Keywords
            "keyword" => editorThemeColorSet.KeywordBlue,
            "keyword - control" => editorThemeColorSet.KeywordBlue,
            "preprocessor keyword" => editorThemeColorSet.KeywordBlue,

            // F# Keywords
            "fsharp.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.preprocessor.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.control.keyword" => editorThemeColorSet.KeywordBlue,

            // Literals & comments (both C# and F#)
            "string" => editorThemeColorSet.LightOrangeBrown,
            "string - verbatim" => editorThemeColorSet.LightOrangeBrown,
            "string - escape character" => editorThemeColorSet.Orange,
            "comment" => editorThemeColorSet.CommentGreen,
            "fsharp.comment" => editorThemeColorSet.CommentGreen,
            "fsharp.xml.doc.comment" => editorThemeColorSet.CommentGreen,
            "number" => editorThemeColorSet.NumberGreen,
            "fsharp.numeric.literal" => editorThemeColorSet.NumberGreen,

            // Types (User Types)
            "class name" => editorThemeColorSet.ClassGreen,
            "record class name" => editorThemeColorSet.ClassGreen,
            "struct name" => editorThemeColorSet.ClassGreen,
            "record struct name" => editorThemeColorSet.ClassGreen,
            "interface name" => editorThemeColorSet.InterfaceGreen,
            "enum name" => editorThemeColorSet.InterfaceGreen,
            "namespace name" => editorThemeColorSet.White,

            // F# Types
            "fsharp.type" => editorThemeColorSet.ClassGreen,
            "fsharp.type.declaration" => editorThemeColorSet.ClassGreen,
            "fsharp.record.declaration" => editorThemeColorSet.ClassGreen,
            "fsharp.union.declaration" => editorThemeColorSet.ClassGreen,
            "fsharp.discriminated.union.declaration" => editorThemeColorSet.ClassGreen,
            "fsharp.enum.declaration" => editorThemeColorSet.InterfaceGreen,
            "fsharp.interface.declaration" => editorThemeColorSet.InterfaceGreen,
            "fsharp.struct.declaration" => editorThemeColorSet.ClassGreen,

            // Identifiers & members
            "identifier" => editorThemeColorSet.White,
            "constant name" => editorThemeColorSet.White,
            "enum member name" => editorThemeColorSet.White,
            "method name" => editorThemeColorSet.Yellow,
            "extension method name" => editorThemeColorSet.Yellow,
            "property name" => editorThemeColorSet.White,
            "field name" => editorThemeColorSet.White,
            "static symbol" => editorThemeColorSet.Yellow, // ??
            "parameter name" => editorThemeColorSet.VariableBlue,
            "local name" => editorThemeColorSet.VariableBlue,
            "type parameter name" => editorThemeColorSet.ClassGreen,
            "delegate name" => editorThemeColorSet.ClassGreen,
            "event name" => editorThemeColorSet.White,
            "label name" => editorThemeColorSet.White,

            // F# Identifiers & members
            "fsharp.identifier" => editorThemeColorSet.White,
            "fsharp.method.or.property.name" => editorThemeColorSet.Yellow,
            "fsharp.function.name" => editorThemeColorSet.Yellow,
            "fsharp.property.name" => editorThemeColorSet.White,
            "fsharp.field.name" => editorThemeColorSet.White,
            "fsharp.event.name" => editorThemeColorSet.White,
            "fsharp.record.field.name" => editorThemeColorSet.White,
            "fsharp.union.case.name" => editorThemeColorSet.White,
            "fsharp.union.case.field" => editorThemeColorSet.White,
            "fsharp.parameter.name" => editorThemeColorSet.VariableBlue,
            "fsharp.lambda.variable.name" => editorThemeColorSet.VariableBlue,
            "fsharp.pattern.identifier" => editorThemeColorSet.White,

            // Punctuation & operators
            "operator" => editorThemeColorSet.White,
            "operator - overloaded" => editorThemeColorSet.Yellow,
            "punctuation" => editorThemeColorSet.White,

            // F# Operators & punctuation
            "fsharp.operator" => editorThemeColorSet.White,
            "fsharp.punctuation" => editorThemeColorSet.White,
            "fsharp.infix-operator" => editorThemeColorSet.Yellow,

            // Preprocessor
            "preprocessor text" => editorThemeColorSet.White,
            
            // Xml comments
            "xml doc comment - delimiter" => editorThemeColorSet.CommentGreen,
            "xml doc comment - name" => editorThemeColorSet.White,
            "xml doc comment - text" => editorThemeColorSet.CommentGreen,
            "xml doc comment - attribute name" => editorThemeColorSet.Gray,
            "xml doc comment - attribute quotes" => editorThemeColorSet.LightOrangeBrown,
            "xml doc comment - attribute value" => editorThemeColorSet.LightOrangeBrown,

            // F# Strings
            "fsharp.string" => editorThemeColorSet.LightOrangeBrown,
            "fsharp.verbatim.string" => editorThemeColorSet.LightOrangeBrown,
            "fsharp.triple-quoted.string" => editorThemeColorSet.LightOrangeBrown,
            "fsharp.script.string" => editorThemeColorSet.LightOrangeBrown,

            // F# Additional keywords
            "fsharp.accessibility.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.active.pattern.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.and.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.as.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.assert.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.base.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.begin.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.class.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.default.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.delegate.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.do.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.done.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.downto.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.else.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.end.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.exception.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.external.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.finally.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.for.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.fun.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.function.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.global.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.if.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.in.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.inherit.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.inline.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.lazy.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.let.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.match.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.member.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.module.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.mutable.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.namespace.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.new.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.not.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.of.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.open.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.or.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.override.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.private.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.public.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.rec.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.return.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.select.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.static.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.struct.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.then.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.to.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.true.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.try.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.type.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.upcast.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.use.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.val.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.void.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.when.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.with.keyword" => editorThemeColorSet.KeywordBlue,
            "fsharp.yield.keyword" => editorThemeColorSet.KeywordBlue,

            // F# Special types
            "fsharp.unit" => editorThemeColorSet.ClassGreen,
            "fsharp.namespace" => editorThemeColorSet.White,
            "fsharp.module" => editorThemeColorSet.ClassGreen,

            // Misc
            "excluded code" => editorThemeColorSet.Gray,
            "text" => editorThemeColorSet.White,
            "whitespace" => editorThemeColorSet.White,

            _ => editorThemeColorSet.Pink // pink, warning color for unhandled classifications
        };
        if (colour == editorThemeColorSet.Pink)
        {
            GD.PrintErr($"Unhandled classification type: '{classificationType}'");
        }
        return colour;
    }
}