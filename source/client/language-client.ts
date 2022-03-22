import { platform } from "os"
import { existsSync } from "fs"
import { ExtensionContext, workspace } from "vscode"
import { LanguageClient, LanguageClientOptions, ServerOptions, TransportKind } from "vscode-languageclient/node"

export function startLanguageClient(context: ExtensionContext): LanguageClient {
  const serverOptions = createServerOptions(context)
  const clientOptions: LanguageClientOptions = {
    documentSelector: [{ scheme: "file", language: "nevod" }],
    synchronize: { fileEvents: workspace.createFileSystemWatcher("**/*.np", true, true, false) }
  }
  const client = new LanguageClient("nevodLanguageServer", "Nevod Language Server", serverOptions, clientOptions)
  client.start()
  return client;
}

function createServerOptions(context: ExtensionContext): ServerOptions {
  const transport = TransportKind.stdio
  let args: string[] = []
  let command = ""
  const windowsExecutablePath = context.asAbsolutePath("build/server/Nezaboodka.Nevod.LanguageServer.exe")
  const linuxExecutablePath = context.asAbsolutePath("build/server/Nezaboodka.Nevod.LanguageServer")
  const darwinExecutablePath = context.asAbsolutePath("build/server/Nezaboodka.Nevod.LanguageServer")
  const portableDllPath = context.asAbsolutePath("build/server/Nezaboodka.Nevod.LanguageServer.dll")
  if (platform() === "win32" && existsSync(windowsExecutablePath)) {
    command = windowsExecutablePath
  } else if (platform() === "linux" && existsSync(linuxExecutablePath)) {
    command = linuxExecutablePath
  } else if (platform() === "darwin" && existsSync(darwinExecutablePath)) {
    command = darwinExecutablePath
  } else {
    command = "dotnet"
    args.push(portableDllPath)
  }
  return {
    command,
    args,
    transport
  }
}
