import { Disposable, ExtensionContext } from "vscode"
import { registerCommands, registerProviders } from "./commands"
import { startLanguageClient } from "./language-client"

const disposables: Disposable[] = []

export function activate(context: ExtensionContext): void {
  registerCommands()
  const client = startLanguageClient(context)
  registerProviders(client);
  disposables.push(new Disposable(client.stop));
}

export async function deactivate(): Promise<void> {
  await Promise.all(disposables.map(d => d.dispose()))
}
