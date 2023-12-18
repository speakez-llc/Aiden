namespace AidenDesktop.Converters

open System
open System.Globalization
open Avalonia.Data.Converters

type HalfValueConverter() =
    interface IValueConverter with
        member this.Convert(value, targetType, parameter, culture) =
            match value with
            | :? double as d -> box (d / 2.0)
            | _ -> value
        
        member this.ConvertBack(value, targetType, parameter, culture) =
            raise (NotImplementedException())