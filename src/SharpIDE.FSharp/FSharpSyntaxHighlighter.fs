namespace SharpIDE.FSharp

open FSharp.Compiler.Tokenization

type FSharpSyntaxHighlighter() =
    let sourceTok = FSharpSourceTokenizer(List.Empty, None, None, None)
    let mutable sourceLines: array<string> = Array.empty

    member public this.Source
        with set(newValue: string) =
            // Syntax highlighting is requested on per-line basis, so split source code into lines.
            let windowsEol = "\r\n"
            let linuxEol = '\n'
            let macEol = '\r'
            sourceLines <- newValue.Replace(windowsEol, string linuxEol).Split(linuxEol, macEol)

    member self.GetLineSyntaxHighlighting(line: int): seq<FSharpTokenInfo> =
        let tokenizer = sourceTok.CreateLineTokenizer sourceLines.[line]

        let rec tokenizeLine (tokenizer: FSharpLineTokenizer) previousTokens state =
            match tokenizer.ScanToken state with
            | Some tok, state ->
                // Tokenize the rest, in the new state
                tok :: (tokenizeLine tokenizer previousTokens state)
            | None, _state -> previousTokens

        let tokens = tokenizeLine tokenizer List.Empty FSharpTokenizerLexState.Initial

        tokens
