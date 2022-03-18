import {
  CancellationToken, commands, CompletionContext, CompletionItem, CompletionItemLabel, CompletionItemProvider,
  languages, Location, Position as VSCodePosition, TextDocument, Uri
} from "vscode"
import { LanguageClient, Position as LspPosition } from "vscode-languageclient/node"

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
  languages.registerCompletionItemProvider({ scheme: "file", language: "nevod" }, new NevodCompletionItemProvider(client), ...NevodCompletionItemProvider.triggerCharacters)
}

class NevodCompletionItemProvider implements CompletionItemProvider {
  public static readonly triggerCharacters = ['.', '"', '\'', '/', ' '];

  public constructor(private client: LanguageClient) {
  }

  async provideCompletionItems(document: TextDocument, position: VSCodePosition, token: CancellationToken, context: CompletionContext): Promise<CompletionItem[] | undefined> {
    let args = {
      context: context,
      textDocument: {
        uri: document.uri.toString()
      },
      position: position,
    }
    let response = await this.client.sendRequest<NevodCompletionItem[] | undefined>("textDocument/completion", args, token)
    return response?.map<CompletionItem>(nevodCompletionItem => {
      let label: string | CompletionItemLabel;
      if (nevodCompletionItem.labelEx)
        label = {
          label: nevodCompletionItem.labelEx.label,
          description: nevodCompletionItem.labelEx.description,
          detail: nevodCompletionItem.labelEx.detail
        }
      else
        label = nevodCompletionItem.label;
      return {
        ...nevodCompletionItem,
        // Subtract 1 because in language server protocol CompletionKind kinds start from 1, in VSCode - from 0.
        kind: nevodCompletionItem.kind ? nevodCompletionItem.kind - 1 : undefined,
        label: label
      }
    });
  }
}

class NevodCompletionItem extends CompletionItem {
  public labelEx?: CompletionItemLabelEx;
}

class CompletionItemLabelEx {
  public constructor(
    public label: string,
    public detail?: string,
    public description?: string) {
  }
}
