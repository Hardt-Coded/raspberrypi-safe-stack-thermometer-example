module Index

open Elmish
open Thoth.Json
open Browser.WebSocket
open Feliz
open Shared


type Model = {
    TemperatureData: Temperature.Data option
}

type Msg = 
    | SetTemperatureData of Temperature.Data


module Commands =

    open Browser.Types
    open Browser.WebSocket

    type ChannelMessage = { Topic: string; Payload: string }

    let inline decode<'a> m = m |> unbox<string> |> Thoth.Json.Decode.Auto.unsafeFromString<'a>

    let connectWebSocketCmd =
        fun dispatch ->
            let onWebSocketMessage (msg:MessageEvent) =
                let msg = msg.data |> decode<ChannelMessage>
                match msg.Topic with
                | "temperature" ->
                    msg.Payload |> decode<Temperature.Data> |> SetTemperatureData |> dispatch
                | _ ->
                    ()

            let rec connect () =
                let host = Browser.Dom.window.location.host
                let url = sprintf "ws://%s/socket/temperature" host
                let ws = WebSocket.Create(url)

                ws.onopen <- (fun _ -> printfn "connection opened!")
                ws.onclose <- (fun _ ->
                    printfn "connection closed!"    
                    promise {
                        do! Promise.sleep 2000
                        connect()
                    }
                )
                ws.onmessage <- onWebSocketMessage

            connect()
        |> Cmd.ofSub


module GaugeControl =

    open Fable.Core.JsInterop
    open Fable.Core

    import "Gauge" "react-gauge-chart"

    module Interop =
        [<Emit("Object.assign({},$0,$1)")>]
        let objectAssign (x:obj) (y:obj) = jsNative

    type IGaugeProperty = interface end

    // id
    // nrOfLevels
    // percent
    // formatTextValue
    type gauge =
        static member inline id (value:string) = ("id",value) |> unbox<IGaugeProperty>
        static member inline nrOfLevels (value:int) = ("nrOfLevels",value) |> unbox<IGaugeProperty>
        static member inline percent (value:float) = ("percent",value) |> unbox<IGaugeProperty>
        static member inline value (value:string) = ("formatTextValue", fun _ -> value) |> unbox<IGaugeProperty>

    type Gauge =
        static member inline gauge (properties: IGaugeProperty list) =
            let defaults = createObj [ "body" ==> Html.none ]
            Interop.reactApi.createElement(importDefault "react-gauge-chart", Interop.objectAssign defaults (createObj !!properties))



let init () =
    { TemperatureData = None }, Commands.connectWebSocketCmd


let update msg model =
    match msg with
    | SetTemperatureData data ->
        { model with TemperatureData = Some data }, Cmd.none


open GaugeControl

let view model dispatch =
    Html.div [
        match model.TemperatureData with
        | None ->
            Html.p "No Data Yet!"
        | Some data ->
            Html.p (sprintf "Temperature: %.1f" data.Temperature)
            Gauge.gauge [
                gauge.id "temperature"
                gauge.nrOfLevels 25
                gauge.percent (data.Temperature / 50.0)
                gauge.value (sprintf "%.1f °C" data.Temperature)
            ]
            Html.p (sprintf "Humidity: %.1f" data.Humidity)

            Gauge.gauge [
                gauge.id "humidity"
                gauge.nrOfLevels 5
                gauge.percent (data.Humidity / 100.0)
            ]

            Html.p (sprintf "Last Update: %s" <| data.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"))

    ]
