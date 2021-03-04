# mohair

Mohair is a Unity Editor tool that uses Roslyn code analysis (only syntax, not semantic) to automatically scan your C# code for Yarn Spinner commands and functions, and produce Markdown + HTML documentation. 

It takes your Yarn Command or Yarn Function binding, then looks at the underlying C# method and its associated code comments and 

(TODO: show example documentation)

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

By default, Mohair ignores C# scripts in any Packages, Editor, and or Tests folder. (TODO: let you customize what gets ignored)

Keep in mind, the code analysis isn't very smart. It isn't compiling your code, so it can't find references or generate symbols. If your code isn't "flat" enough, this tool won't work very well. When adding command handlers or functions to the Dialogue Runner in Yarn Spinner Unity, do it in the simplest possible way. 

**Call `AddCommandHandler` or `AddFunction` once per binding, once per line. Avoid loops, recursion, delegates, etc.**

Mohair can read a fairly flat code style like this:

```csharp
runner.AddCommandHandler<string>("lookAt", YarnLookAt); 

/// <summary>make the character look at the named gameObject</summary>
public void YarnLookAt(string gameObjectName) {
    Camera.main.transform.LookAt( GameObject.Find(gameObjectName) );
}
```

... but Mohair will NOT understand something like this
:
```csharp
void AddCmd(string yarnName, System.Action<string> handler) {
    runner.AddCommandHandler<string>(yarnName, handler);
}
for(int i=0; i<commandList.Length; i++) {
    AddCmd(commandList.yarn, commandList.handler);
}
```

You can edit the documentation templates in the /Editor/ folder
- `MohairTemplate.md` is the Markdown template
- `MohairTemplateWeb.html` is an HTML template with Bootstrap

## license

- core Mohair code is MIT
- third-party library [MarkDig](https://github.com/xoofx/markdig) is BSD-2-Clause
