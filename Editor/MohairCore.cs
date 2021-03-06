using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Yarn.Unity;

using Markdig;

public class MohairCore
{
    static string filePath = "/Dialogue/YarnDocumentation";
    static System.Text.Encoding encoding = System.Text.Encoding.UTF8;

    [MenuItem("Assets/Mohair/Force Regenerate Yarn Documentation")]
    public static void MenuGenerateYarnDocumentation() {
        GenerateYarnDocumentation( forceRegenerate:true );
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    public static void AutoGenerateYarnDocumentation() {
        GenerateYarnDocumentation();
    }

    public static void GenerateYarnDocumentation(bool forceRegenerate=false) {
        // var targets = Selection.GetFiltered<MonoScript>(SelectionMode.Assets);
        var targets = FindAssetsByType<MonoScript>("Script", "editor", "test", "package");
        
        var newEntries = new List<MohairEntry>();
        var methods = new Dictionary<string, List<MohairMethod>>();

        foreach ( var target in targets ) {
        //    Debug.Log( target.name );
            newEntries.AddRange( GetEntriesFromCode(target.text, methods) );
        }
        // newEntries.AddRange( GetYarnCommandsFromAttributes() );

        // foreach ( var entry in newEntries ) {
        //     // Debug.Log( entry.ToString() );
        //     // Debug.Log("ENTRY: " + entry.yarnName + "... " + entry.cSharpName);
        // }

        // foreach ( var kvp in methods ) {
        //     // Debug.Log( string.Join("\n", kvp.Value.Select( method => method.ToString() ) ) );
        //     // Debug.Log("METHOD: " + kvp.Key);
        // }

        // now try to pair up Yarn entries with method invocation data
        foreach ( var entry in newEntries ) { 
            if ( methods.ContainsKey( entry.cSharpName ) ) {
                // for now, just select the first one
                var method = methods[entry.cSharpName][0];
                entry.cSharpClass = method.cSharpClass;
                entry.comment = method.comment;
                entry.parameterTypes = method.parameterTypes;
                entry.parameters = method.parameters;
                entry.region = method.region;
            }
        }

        var commands = newEntries.Where( entry => entry.entryType != MohairEntry.EntryType.Function ).OrderBy(entry => entry.region).ThenBy( entry => entry.yarnName ).ToList();
        var functions = newEntries.Where( entry => entry.entryType == MohairEntry.EntryType.Function ).OrderBy(entry => entry.region).ThenBy(entry => entry.yarnName ).ToList();

        // generate TOC

        string commandTOC = "";
        foreach ( var entry in commands ) {
            commandTOC += entry.ToStringTOC();
        }
        
        string functionTOC = "";
        foreach ( var entry in functions ) {
            functionTOC += entry.ToStringTOC();
        }

        // generate full command list

        newEntries = newEntries.OrderBy( entry => entry.yarnName ).ToList();
        string entries = "";
        foreach ( var entry in newEntries ) {
            entries += entry.ToString();
        }

        // grab template
        const string templateFilePath = "Packages/com.radiatoryang.mohair/Editor/MohairTemplate.md"; // TODO: make user configurable
        var templateFile = AssetDatabase.LoadAssetAtPath<TextAsset>(templateFilePath);
        var templateText = "";
        if ( templateFile == null ) {
            Debug.LogError($"Mohair couldn't load the Markdown template at {templateFilePath}. Sometimes this happens when you freshly install Mohair, but Unity hasn't imported the template file yet. In the Unity menu bar, use Assets > Mohair > Force Regenerate Documentation to try again. If this error persists, then the file is definitely missing or something.");
        } else {
            templateText = templateFile.text;
        }

        var templateWebFile = AssetDatabase.LoadAssetAtPath<TextAsset>("Packages/com.radiatoryang.mohair/Editor/MohairTemplateWeb.html");
        var templateWeb = templateWebFile.text.Split(new string[] {"<!--CONTENT-->"}, System.StringSplitOptions.RemoveEmptyEntries );

        // output the finished reference
        string fullPath = Application.dataPath + filePath;
        string markdown = string.Format(templateText, commandTOC, functionTOC, entries);

        // but only follow through if the MD5 checksum of the old file is different from the checksum of the new file
        if ( forceRegenerate || !File.Exists( fullPath + ".md" ) || Md5Sum(File.ReadAllText(fullPath + ".md", encoding)) != Md5Sum(markdown) ) {
            File.WriteAllText( fullPath + ".md", markdown, encoding );
            var pipeline = new MarkdownPipelineBuilder().UsePipeTables().Build();
            var html = templateWeb[0] + Markdown.ToHtml(markdown, pipeline) + templateWeb[1];
            File.WriteAllText( fullPath + "Web.html", html, encoding );
            if ( fullPath.StartsWith(Application.dataPath) ) {
                AssetDatabase.ImportAsset("Assets" + filePath + ".md");
                AssetDatabase.ImportAsset("Assets" + filePath + "Web.html");
            }
            Debug.Log($"Mohair successfully regenerated Yarn Documentation ({fullPath})");
        }
        
    }
    public static List<MohairEntry> GetEntriesFromCode(string sourceCode, Dictionary<string, List<MohairMethod>> methods) {
        var tree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = tree.GetRoot();

        var visitor = new MohairVisitor();
        visitor.Visit(root);

        // entries.AddRange( visitor.entries );
       //  Debug.Log(visitor.entries.Count);

        foreach ( var kvp in visitor.methods ) {
            if ( methods.ContainsKey(kvp.Key) ) {
                methods[kvp.Key].AddRange( kvp.Value );
            } else {
                methods.Add( kvp.Key, kvp.Value );
            }
        }

        return visitor.entries;
    }

    public class MohairVisitor : CSharpSyntaxWalker {
        public List<MohairEntry> entries = new List<MohairEntry>();
        public Dictionary<string, List<MohairMethod>> methods = new Dictionary<string, List<MohairMethod>>();

        // string inClass = "";
        string inRegion = "";

        public MohairVisitor() : base(SyntaxWalkerDepth.StructuredTrivia)
        {
        }

        // public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        // {
        //     inClass = node.Identifier.Text;
        //     base.VisitClassDeclaration(node);
        // }

        public override void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
        {
            inRegion = node.ToString();
            // Debug.Log(inRegion);
            base.VisitRegionDirectiveTrivia(node);
        }

        public override void VisitEndRegionDirectiveTrivia(EndRegionDirectiveTriviaSyntax node)
        {
            inRegion = "";
            base.VisitEndRegionDirectiveTrivia(node);
        }

        public override void VisitAttributeList(AttributeListSyntax node)
        {
            var yarnCommandAttr = node.Attributes.Where( currentAttribute => currentAttribute.Name.GetText().ToString() == "YarnCommand").FirstOrDefault();

            // If the parent is a MethodDeclaration
            if (node.Parent is MethodDeclarationSyntax && yarnCommandAttr != null)
            {
                var entry = new MohairEntry(
                    MohairEntry.EntryType.CommandAttribute,
                    yarnCommandAttr.ArgumentList.Arguments[0].ToString().Replace("\"", "").Replace("'", "").Replace("@", ""),
                    ((MethodDeclarationSyntax)node.Parent).Identifier.Text,
                    new List<string>()
                );
            }
            base.VisitAttributeList(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node) {
            string expression = node.ToString();
            if ( node.Expression.GetType() == typeof(IdentifierNameSyntax) ) {
                expression = ((IdentifierNameSyntax)node.Expression).ToString();
            }
            // Debug.Log(node.ToString());

            if ( expression.Contains("AddCommandHandler") ) {
                // Debug.Log( node.ToString() + "..." + node.ArgumentList.Arguments[0].ToString() );
                try {
                    var entry = new MohairEntry(
                        MohairEntry.EntryType.Command,
                        node.ArgumentList.Arguments[0].ToString().Replace("\"", "").Replace("'", "").Replace("@", ""),
                        node.ArgumentList.Arguments[1].ToString().Split(new string[] { "=>", ".", "return" }, System.StringSplitOptions.RemoveEmptyEntries).Last(),
                        new List<string>()
                    );
                    entries.Add( entry );
                } catch {
                    // Debug.LogWarning( node.ToString() );
                }
                // TODO: try to parse the parameter types, at least
                
            }

            if ( expression.Contains("AddFunction") ) {
                // Debug.Log( node.ToString() + "..." + node.ArgumentList.Arguments[0].ToString() );
                var entry = new MohairEntry(
                    MohairEntry.EntryType.Function,
                    node.ArgumentList.Arguments[0].ToString().Replace("\"", "").Replace("'", "").Replace("@", ""),
                    node.ArgumentList.Arguments[1].ToString().Split(new string[] { "=>", ".", "return" }, System.StringSplitOptions.RemoveEmptyEntries).Last().Split('(')[0],
                    new List<string>()
                );
                // TODO: try to parse the parameter types, at least, so we can have more confidence in a method match later on
                entries.Add( entry );
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var comment = "";
            var trivias = node.GetLeadingTrivia();
            foreach ( var trivia in trivias) {
                var kind = trivia.Kind();
                if(kind == SyntaxKind.SingleLineCommentTrivia || kind == SyntaxKind.SingleLineDocumentationCommentTrivia || kind == SyntaxKind.MultiLineCommentTrivia || kind == SyntaxKind.MultiLineDocumentationCommentTrivia)
                {
                    // var xml = trivia.GetStructure();
                    comment += trivia.ToString();
                }
            }

            var paramList = new List<string>();
            var paramTypeList = new List<string>();
            var parameters = node.ParameterList.ChildNodes();
            foreach ( var param in parameters ) {
                var paramSplit = param.ToString().Split(' ');
                paramTypeList.Add( paramSplit[0] );
                paramList.Add( paramSplit.Length > 1 ? paramSplit[1] : "" );
            }
          
            var parentClass = node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var newMethodEntry = new MohairMethod(
                node.Identifier.Text,
                (parentClass != null ? parentClass.Identifier.Text : ""),
                StripTagsCharArray(comment).Replace("///", " ").Replace("//", ""),
                paramList,
                paramTypeList
            );
            if ( string.IsNullOrEmpty(inRegion) == false ) {
                newMethodEntry.region = inRegion.Substring("#region ".Length);
            }

            if ( methods.ContainsKey( newMethodEntry.cSharpName) ) {
                methods[newMethodEntry.cSharpName].Add( newMethodEntry );
            } else {
                methods.Add( newMethodEntry.cSharpName, new List<MohairMethod>() { newMethodEntry } );
            }

            base.VisitMethodDeclaration(node);
        }
    }

    [System.Serializable]
    public class MohairMethod {
        public string cSharpName, cSharpClass, comment, region;
        public List<string> parameters;
        public List<string> parameterTypes;

        public MohairMethod(string cSharpName, string cSharpClass, string comment, List<string> parameters, List<string> parameterTypes)
        {
            this.cSharpName = cSharpName;
            this.cSharpClass = cSharpClass;
            this.comment = comment;
            this.parameters = parameters;
            this.parameterTypes = parameterTypes;
        }

        public override string ToString() {
            return $"{cSharpName}: {string.Join(", ", parameterTypes)} ... {string.Join(", ", parameters)} \n {comment}";
        }
    }


    [System.Serializable]
    public class MohairEntry {
        public string yarnName, cSharpName, cSharpClass, comment, region;
        public List<string> parameters = new List<string>();
        public List<string> parameterTypes = new List<string>();

        const string paramStringTemplateRaw = "{0} {1}";
        const string paramStringTemplateFormatted = "<i><u>{0}</u></i> {1}";
        
        string ParamsToString(string template) {
            var combined = parameterTypes.Count > 0 ? " " : "";
            for( int i=0; i<parameterTypes.Count; i++) {
                combined += string.Format(template, parameterTypes[i], parameters.Count > i ? parameters[i] : "") + (i < parameterTypes.Count-1 ? ", " : "");
            }
            return combined;
        }

        public EntryType entryType;
        public enum EntryType { Command, CommandAttribute, Function };
        string prefix { get { return entryType != EntryType.Function ? "<<" : ""; } }
        string suffix { get { return entryType != EntryType.Function ? ">>" : "()"; } }

        string GetParamsAndSuffix(string template) {
            if ( entryType == EntryType.Function ) {
                return $"({ ParamsToString(template) } )";
            } else {
                return $"{ParamsToString(template)}>>";
            }
        }

        string callstack { get { return $"`{cSharpClass}.{cSharpName}() ` { (string.IsNullOrEmpty(region) ? "" : $" in ` #region {region}`") }"; } }
        string signature { get { return $"<!-- {prefix}{yarnName}{GetParamsAndSuffix(paramStringTemplateRaw)} -->  <pre>{prefix}<b>{yarnName}</b>{GetParamsAndSuffix(paramStringTemplateFormatted)}</pre>"; } }

        public MohairEntry(EntryType type, string yarnName, string cSharpName, List<string> parameterTypes)
        {
            this.entryType = type;
            this.yarnName = yarnName;
            this.cSharpName = cSharpName;
            this.parameterTypes = parameterTypes;
        }

        public override string ToString()
        {
            return $"---\n<a name=\"{yarnName}\"></a>\n### `{prefix}{yarnName}{suffix}`\n{comment}\n{signature}\n\n{callstack}\n\n";
        }

        public string ToStringTOC() {
            return $"|[`{prefix}{yarnName}{suffix}`](#{yarnName})     |{ (string.IsNullOrEmpty(comment) ? "" : comment.Substring(0, Mathf.Min(70, comment.Length)).Replace('\n', ' ').Replace('\r', ' ') ) }     |\n";
        }
    }

    /// <summary>takes a generic type and will return all objects in your project regardless of if they have been loaded yet, from https://answers.unity.com/questions/373459/how-do-you-get-a-list-of-code-classes-from-an-edit.html</summary>
    public static List<T> FindAssetsByType<T>(string searchTypeOverride = "", params string[] exclude) where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        if ( string.IsNullOrEmpty(searchTypeOverride) ) {
            searchTypeOverride = typeof(T).ToString();
        }
        string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", searchTypeOverride));
        for( int i = 0; i < guids.Length; i++ )
        {
            string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );

            if ( exclude.Length > 0) {
                string path = assetPath.ToLowerInvariant();
                bool stop = false;
                for(int x=0; x<exclude.Length; x++) {
                    if ( path.Contains(exclude[x])) {
                        stop = true;
                        break;
                    }
                }
                if ( stop ) {
                    continue;
                }
            }
            
            T asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );
            if( asset != null )
            {
                assets.Add(asset);
            }
        }
        return assets;
    }

    /// <summary>
    /// Remove HTML tags from string using char array. From https://www.dotnetperls.com/remove-html-tags
    /// </summary>
    public static string StripTagsCharArray(string source)
    {
        char[] array = new char[source.Length];
        int arrayIndex = 0;
        bool inside = false;

        for (int i = 0; i < source.Length; i++)
        {
            char let = source[i];
            if (let == '<')
            {
                inside = true;
                continue;
            }
            if (let == '>')
            {
                inside = false;
                continue;
            }
            if (!inside)
            {
                array[arrayIndex] = let;
                arrayIndex++;
            }
        }
        return new string(array, 0, arrayIndex);
    }

    /// from https://github.com/MartinSchultz/unity3d/blob/master/CryptographyHelper.cs
    /// <summary>
    /// used to calculate file checksums so we can avoid unnecessary ImportAsset operations
    /// </summary>
    public static string Md5Sum(string fileText)
    {
        var bytes = encoding.GetBytes(fileText);

        // encrypt bytes
        System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);

        // Convert the encrypted bytes back to a string (base 16)
        string hashString = "";
        for (int i = 0; i < hashBytes.Length; i++)
        {
            hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, "0"[0]);
        }
        return hashString.PadLeft(32, "0"[0]);
    }
}

