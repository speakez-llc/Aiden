namespace AidenDesktop.Converters

open System
open Avalonia.Data.Converters
open System.Globalization
open Avalonia

type BooleanNegationConverter() =
    interface IValueConverter with
        member this.Convert(value, targetType, parameter, culture) =
            match value with
            | :? bool as b -> box (not b)
            | _ -> AvaloniaProperty.UnsetValue

        member this.ConvertBack(value, targetType, parameter, culture) =
            match value with
            | :? bool as b -> box (not b)
            | _ -> AvaloniaProperty.UnsetValue
