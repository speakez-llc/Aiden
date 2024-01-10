namespace AidenDesktop.Controls

open Avalonia
open Avalonia.Controls
open Avalonia.Layout
open FluentAvalonia.UI.Controls
open Avalonia.Data.Converters
open FluentIcons.FluentAvalonia


type SymbolIconToSymbolConverter() =
    interface IValueConverter with
        member _.Convert(value, targetType, parameter, culture) =
            match value with
            | :? SymbolIcon as symbolIcon -> symbolIcon.Symbol
            | _ -> AvaloniaProperty.UnsetValue

        member _.ConvertBack(value, targetType, parameter, culture) =
            AvaloniaProperty.UnsetValue


type BadgeIcon() as this =
    inherit Grid()

    static let symbolProperty =
        AvaloniaProperty.Register<BadgeIcon, FluentIcons.Common.Symbol>("Symbol")

    static let badgeValueProperty =
        AvaloniaProperty.Register<BadgeIcon, int>("BadgeValue")

    do
        let icon = SymbolIcon()
        icon.Bind(SymbolIcon.SymbolProperty, this.GetObservable(symbolProperty)) |> ignore
        this.Children.Add(icon)

        let badge = InfoBadge()
        badge.Bind(InfoBadge.ValueProperty, this.GetObservable(badgeValueProperty)) |> ignore
        badge.HorizontalAlignment <- HorizontalAlignment.Right
        badge.VerticalAlignment <- VerticalAlignment.Top
        this.Children.Add(badge)

    member this.Symbol
        with get() = this.GetValue(symbolProperty)
        and set(value) = this.SetValue(symbolProperty, value) |> ignore

    member this.BadgeValue
        with get() = this.GetValue(badgeValueProperty)
        and set(value) = this.SetValue(badgeValueProperty, value) |> ignore