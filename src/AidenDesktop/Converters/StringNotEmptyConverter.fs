namespace AidenDesktop.Converters

open Avalonia.Data.Converters
open System
open System.Globalization

type StringNotEmptyConverter() =
    interface IValueConverter with
        member _.Convert(value, targetType, parameter, culture) =
            match value with
            | :? string as str -> not (String.IsNullOrEmpty(str))
            | _ -> false

        member this.ConvertBack(value, targetType, parameter, culture) =
            raise (NotImplementedException())