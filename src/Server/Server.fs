module Server

open Giraffe
open Saturn
open Shared
open FSharp.Control.Tasks.V2
open Microsoft.AspNetCore.Builder

let channel =
    channel {
        join (fun ctx clientInfo ->
            task {
                printfn "Someone has connected!"
                return Channels.Ok
            }
            
        )
    }


let sendMessage (hub:Channels.ISocketHub) topic payload =
    task {
        let message = Thoth.Json.Net.Encode.Auto.toString(0, payload)
        do! hub.SendMessageToClients "/socket/temperature" topic message
    }


module ApplicationExtension =

    open System.Threading.Tasks

    type Saturn.Application.ApplicationBuilder with

        [<CustomOperation("start_custom_process")>]
        member __.StartCustomProcess(state:ApplicationState, startup: IApplicationBuilder -> Task<unit>) =
            
            let appBuilderConfig (app:IApplicationBuilder) =
                Task.Run<Task<unit>>((fun () -> startup app)) |> ignore
                app

            {
                state with
                    AppConfigs = appBuilderConfig::state.AppConfigs
            }

open ApplicationExtension
open Microsoft.Extensions.DependencyInjection
open Saturn.Channels

let app =
    application {
        url "http://0.0.0.0:8085"
        no_router
        memory_cache
        use_static "public"
        use_json_serializer (Thoth.Json.Giraffe.ThothSerializer())
        use_gzip
        add_channel "/socket/temperature" channel
        start_custom_process (fun app ->
            task {
                let socketHub = app.ApplicationServices.GetService<ISocketHub>()
                let temperatureStream, startTempTask = TemperatureService.createTemperatureService ()

                temperatureStream
                |> Observable.subscribe (fun temp ->
                    sendMessage socketHub "temperature" temp |> ignore
                )
                |> ignore

                startTempTask () |> ignore
            }
            
        )
    }

run app