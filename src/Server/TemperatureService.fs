module TemperatureService

    open Shared
    open Shared.Temperature
    open FSharp.Control.Tasks.V2
    open System.Threading.Tasks
    open System
    open System.Device.I2c
    open Iot.Device.GrovePiDevice
    open Iot.Device.GrovePiDevice.Sensors
    open Iot.Device.GrovePiDevice.Models

    let createTemperatureService () =
        let temperatureChangedEvent = Event<Temperature.Data>()

        let tempRunner () =
            task {
                let ic2Settings = I2cConnectionSettings(1, GrovePi.DefaultI2cAddress |> int)
                let grovePi = new GrovePi(I2cDevice.Create(ic2Settings))
                let dhtSensor = DhtSensor(grovePi,GrovePort.DigitalPin7,DhtType.Dht22)

                while true do
                    do! Task.Delay 1000
                    dhtSensor.Read()

                    temperatureChangedEvent.Trigger <| {
                        Temperature = dhtSensor.LastTemperature
                        Humidity = dhtSensor.LastRelativeHumidity
                        Timestamp = DateTime.UtcNow
                    }
            }

        (temperatureChangedEvent.Publish,tempRunner)


