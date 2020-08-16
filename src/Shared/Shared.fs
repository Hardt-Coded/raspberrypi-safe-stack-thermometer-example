namespace Shared

open System

module Temperature =
    
    type Data = {
        Temperature:float
        Humidity:float
        Timestamp:DateTime
    }