#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#load "packages/FsLab/FsLab.fsx"
#I "packages/Suave/lib/net40"
#I "packages/FSharp.Formatting/lib/net40"
#I "packages/FsLab.Runner/lib/net40"
#r "FsLab.Runner.dll"
#r "Suave.dll"
#r @"packages\Argu\lib\net35\Argu.dll"

open FsLab
open System
open System.Text
open System.IO
open System.Diagnostics
open FSharp.Literate
open Argu

type Arguments =
    | [<Mandatory; AltCommandLine("-i"); MainCommand; ExactlyOnce; >] Input of ``script file``: string
    | [<AltCommandLine("-m")>] Monitor
    | No_Preview
with
    interface IArgParserTemplate with
        member arg.Usage = match arg with _ -> ""

let parser = ArgumentParser.Create<Arguments>(programName="fslab", errorHandler=ProcessExiter())

module Utils =
    let traceImportant = printfn "%s"
    let (</>) x y = Path.Combine(x, y)
    
    let handleError(err:FsiEvaluationFailedInfo) =
        sprintf "Evaluating F# snippet failed:\n%s\nThe snippet evaluated:\n%s" err.StdErr err.Text
        |> traceImportant
        
    let generateJournals ctx =
      let builtFiles = Journal.processJournals ctx
      traceImportant "All journals updated."
      Journal.getIndexJournal ctx builtFiles

    let processHtml (source:string) =
        let root = Path.GetDirectoryName source
        let ctx = 
          { ProcessingContext.Create(root) with
              OutputKind = OutputKind.Html
              Output = root </> "output";
              TemplateLocation = Some(__SOURCE_DIRECTORY__ </> "packages/FsLab.Runner")
              FailedHandler = handleError 
              FileWhitelist = Some [Path.GetFileName source]
              Standalone = true }

        let index = generateJournals ctx
        printfn "Processed: index: %s ctx: %A" index ctx
        index, ctx

    let postProcess (file:string) =
        let file = Path.GetFullPath(file)
        printfn "Launching %s" (file)
        Process.Start(file)
    
    let handleWatcherEvents (ctx) (e:IO.FileSystemEventArgs) =
        let fi = FileInfo(e.FullPath)
        traceImportant <| sprintf "%s was changed." fi.Name
        if fi.Attributes.HasFlag IO.FileAttributes.Hidden ||
           fi.Attributes.HasFlag IO.FileAttributes.Directory then ()
        else Journal.updateJournals ctx |> ignore

    // I don't want to use a FS watcher
    let AddMonitor ctx filename =
        use watcher = new FileSystemWatcher(ctx.Root, filename)
        watcher.EnableRaisingEvents <- true
        watcher.Changed.Add(handleWatcherEvents ctx)

    let rec monitor (file:string) (lastWrite:DateTime) =
        async {
            let ts = FileInfo(file).LastWriteTimeUtc
            if (ts > lastWrite) then
                printfn "File changed: %s" file
                processHtml file |> ignore

            do! Async.Sleep 500
            return! monitor file ts
        }

open Utils

let main argv =
    let results = parser.ParseCommandLine argv
    let source : string = results.GetResult(<@ Arguments.Input @>) |> Path.GetFullPath
    printfn "Source is %s" source
    let (index, ctx) = processHtml source
    if not (results.Contains <@ No_Preview @>) then
        postProcess (ctx.Output </> index) |> ignore
    if results.Contains(<@ Arguments.Monitor @>) then
        printfn "Starting monitoring file change: %s" source
        monitor source DateTime.MaxValue |> Async.RunSynchronously

    0

main (fsi.CommandLineArgs |> Array.skip 1)