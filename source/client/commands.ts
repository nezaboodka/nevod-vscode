import {
  CancellationToken, commands, CompletionContext, CompletionItem, CompletionItemLabel, CompletionItemProvider,
  languages, Location, Position as VSCodePosition, TextDocument, Uri, DocumentFormattingEditProvider,
  DocumentRangeFormattingEditProvider, FormattingOptions, TextEdit, Range, EndOfLine, DocumentSelector
} from "vscode"
import {
  LanguageClient, Position as LspPosition, Range as LspRange,
  DocumentFormattingParams, DocumentRangeFormattingParams
} from "vscode-languageclient/node"

async function peekReferences(path: string, { line, character }: LspPosition): Promise<void> {
  const uri = Uri.parse(path)
  const position = new VSCodePosition(line, character)
  const references = await commands.executeCommand<Location[]>("vscode.executeReferenceProvider", uri, position)
  await commands.executeCommand("editor.action.peekLocations", uri, position, references, "peek")
}

export function registerCommands(): void {
  commands.registerCommand("nevod.peekReferences", peekReferences)
}

export function registerProviders(client: LanguageClient): void {
  const selector: DocumentSelector = { scheme: "file", language: "nevod" }
  languages.registerCompletionItemProvider(selector, new NevodCompletionItemProvider(client), ...NevodCompletionItemProvider.triggerCharacters)
  const formattingProvider = new NevodFormattingProvider(client)
  languages.registerDocumentFormattingEditProvider(selector, formattingProvider)
  languages.registerDocumentRangeFormattingEditProvider(selector, formattingProvider)
}

class NevodCompletionItemProvider implements CompletionItemProvider {
  public static readonly triggerCharacters = ['.', '"', '\'', '/'];

  public constructor(private client: LanguageClient) {
  }

  public async provideCompletionItems(document: TextDocument, position: VSCodePosition, token: CancellationToken, context: CompletionContext): Promise<CompletionItem[] | undefined> {
    const args = {
      context: context,
      textDocument: {
        uri: document.uri.toString()
      },
      position: position,
    }
    let response = await this.client.sendRequest<NevodCompletionItem[] | undefined>("textDocument/completion", args, token)
    return response?.map<CompletionItem>(nevodCompletionItem => {
      let label: string | CompletionItemLabel
      if (nevodCompletionItem.labelEx)
        label = {
          label: nevodCompletionItem.labelEx.label,
          description: nevodCompletionItem.labelEx.description,
          detail: nevodCompletionItem.labelEx.detail
        }
      else
        label = nevodCompletionItem.label
      return {
        ...nevodCompletionItem,
        // Subtract 1 because in language server protocol CompletionKind kinds start from 1, in VSCode - from 0.
        kind: nevodCompletionItem.kind ? nevodCompletionItem.kind - 1 : undefined,
        label: label
      }
    })
  }
}

class NevodCompletionItem extends CompletionItem {
  public labelEx?: CompletionItemLabelEx
}

class CompletionItemLabelEx {
  public constructor(
    public label: string,
    public detail?: string,
    public description?: string) {
  }
}

class NevodFormattingProvider implements DocumentFormattingEditProvider, DocumentRangeFormattingEditProvider {
  public constructor(private client: LanguageClient) {
  }

  public async provideDocumentFormattingEdits(document: TextDocument, options: FormattingOptions, token: CancellationToken): Promise<TextEdit[]> {
    const nevodFormattingOptions = this.createNevodFormattingOptions(document, options)
    const args: DocumentFormattingParams = {
      textDocument: {
        uri: document.uri.toString()
      },
      options: nevodFormattingOptions,
    }
    const response = await this.client.sendRequest<TextEdit[]>("textDocument/formatting", args, token)
    return response
  }

  public async provideDocumentRangeFormattingEdits(document: TextDocument, range: Range, options: FormattingOptions, token: CancellationToken): Promise<TextEdit[]> {
    const nevodFormattingOptions = this.createNevodFormattingOptions(document, options)
    const args: DocumentRangeFormattingParams = {
      textDocument: {
        uri: document.uri.toString()
      },
      range: this.rangeToLspRange(range),
      options: nevodFormattingOptions,
    }
    const response = await this.client.sendRequest<TextEdit[]>("textDocument/rangeFormatting", args, token)
    return response
  }

  private createNevodFormattingOptions(document: TextDocument, options: FormattingOptions): NevodFormattingOptions {
    return { newLine: document.eol == EndOfLine.LF ? "\n" : "\r\n", ...options }
  }

  private rangeToLspRange(range: Range): LspRange {
    return LspRange.create(range.start.line, range.start.character, range.end.line, range.end.character)
  }
}

interface NevodFormattingOptions extends FormattingOptions {
  newLine: string
}
