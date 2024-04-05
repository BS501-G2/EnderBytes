// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// @ts-ignore
import { dotnet } from './dotnet/wwwroot/_framework/dotnet.js'

export async function init(imports) {
  const { setModuleImports, getAssemblyExports, getConfig, runMain } = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationArgumentsFromQuery()
    .create();

  setModuleImports('main.js', imports);

  const config = getConfig();
  const exports = await getAssemblyExports(config.mainAssemblyName);

  // run the C# Main() method and keep the runtime process running and executing further API calls
  // await runMain();

  // await runMain()

  return exports
}


Object.assign(window, {
  "__DOTNET__INIT__": init
})
