# mohair

Mohair is a Unity Editor tool that uses Roslyn code analysis (only syntax, not semantic) to automatically scan your C# code for Yarn Spinner commands and functions, and produce Markdown + HTML documentation. It takes your Yarn Command or Yarn Function binding, and then pairs it with the underlying C# method and its associated code comments.

This tool is intended for game developers who need to work with game writers and narrative designers, since non-programmers are unlikely to delve into the C# code to figure out what's happening. This way, any documentation you apply to your code (e.g. XML comments) is automatically paired with the equivalent Yarn command in an accessible document. It all just works in the background.

<img src="https://user-images.githubusercontent.com/2285943/110420410-368c0700-8069-11eb-85fd-2e665f439e41.png" />

## installation

Make sure you have the pre-reqs installed:
- dependency: com.unity.code-analysis (automatically installed if you import via UPM)
- Unity 2018.4+ or newer
- Yarn Spinner v2.0+ or newer

Then install Mohair:
- install via Git URL in Unity Package Manager (recommended)
- or clone the submodule in a subfolder of /Assets/ or /Packages/ in your Unity project, e.g. /Assets/Mohair/

## usage

Mohair runs automatically every time your code recompiles. You can also force it to regenerate documentation manually, via the Unity menu bar: Assets > Mohair > Force Regenerate Documentation.

Keep in mind, the code analysis isn't very smart. It isn't compiling your code, so it can't find references or generate symbols. If your code isn't "flat" enough, this tool won't work very well. When adding command handlers or functions to the Dialogue Runner in Yarn Spinner Unity, do it in the simplest possible way... **Call `AddCommandHandler` or `AddFunction` once per binding, once per line. Avoid loops, recursion, delegates, etc.**

Mohair can read a fairly flat code style like this:

```csharp
runner.AddCommandHandler<string>("lookAt", YarnLookAt); 

/// <summary>make the character look at the named gameObject</summary>
public void YarnLookAt(string gameObjectName) {
    Camera.main.transform.LookAt( GameObject.Find(gameObjectName) );
}
```

... but Mohair will NOT understand if you wrap around AddCommandHandler or if the C# function name isn't directly in the invocation. So if you want Mohair to work, then DO NOT do this:

```csharp
void AddCmd(string yarnName, System.Action<string> handler) {
    runner.AddCommandHandler<string>(yarnName, handler);
}
for(int i=0; i<commandList.Length; i++) {
    AddCmd(commandList.yarn, commandList.handler);
}
```

## customization

By default, Mohair ignores C# scripts in any Packages, Editor, and or Tests folder. You can change this ignore list in Project Settings > Mohair.

You can edit the default documentation templates in the /Editor/ folder
- `MohairTemplate.md` is the Markdown template, good for viewing on GitHub
- `MohairTemplateWeb.html` is an HTML template with Bootstrap in dark theme, with content exported from the Markdown

You can set Mohair to save the .md and .html files anywhere in your project folder (e.g. /Assets/, /Docs/ )

## license

- core Mohair code is MIT
- third-party library [MarkDig](https://github.com/xoofx/markdig) is BSD-2-Clause
