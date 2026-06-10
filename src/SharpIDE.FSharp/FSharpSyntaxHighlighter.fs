namespace SharpIDE.FSharp

open FSharp.Compiler.Tokenization

type FSharpSyntaxHighlighter() =
    let sourceTok = FSharpSourceTokenizer(List.Empty, None, None, None)
    let mutable sourceLines: array<string> = Array.empty
    let mutable cache: array<Option<FSharpTokenizerLexState * List<FSharpTokenInfo>>> = Array.empty

    let rec tokenizeLine (tokenizer: FSharpLineTokenizer) previousTokens state =
        match tokenizer.ScanToken state with
        | Some tok, state ->
            let newState, newTokens = tokenizeLine tokenizer previousTokens state
            newState, tok :: newTokens
        | None, state -> state, previousTokens

    let rec getLexerStateAndTokensForLine (lineNumber: int) =
        match cache.[lineNumber] with
        | Some(stateAtTheEnd, tokens) -> stateAtTheEnd, tokens
        | None ->
            let tokenizer = sourceTok.CreateLineTokenizer sourceLines.[lineNumber]
            let initialState =
                if lineNumber = 0 then
                    FSharpTokenizerLexState.Initial
                else
                    match cache.[lineNumber - 1] with
                    | Some (stateAtTheEnd, _) -> stateAtTheEnd
                    | None ->
                        let state, _tokens = getLexerStateAndTokensForLine (lineNumber - 1)
                        state

            let stateAtTheEnd, tokens = tokenizeLine tokenizer List.Empty initialState
            cache.[lineNumber] <- Some(stateAtTheEnd, tokens)
            stateAtTheEnd, tokens

    member public self.Source
        with set(newValue: string) =
            // Syntax highlighting is requested on per-line basis, so split source code into lines.
            let windowsEol = "\r\n"
            let linuxEol = '\n'
            let macEol = '\r'
            let newSourceLines = newValue.Replace(windowsEol, string linuxEol).Split(linuxEol, macEol)
            if newSourceLines <> sourceLines then
                sourceLines <- newSourceLines
                cache <- Array.create sourceLines.Length None

    member self.GetLineSyntaxHighlighting(line: int): seq<FSharpTokenInfo> =
        let _, tokens = getLexerStateAndTokensForLine line
        tokens
