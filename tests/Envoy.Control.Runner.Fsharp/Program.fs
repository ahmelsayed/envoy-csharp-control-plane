open System
open System.Collections.Generic
open System.Linq
open System.Threading
open Envoy.Api.V2
open Envoy.Api.V2.Auth
open Envoy.Api.V2.Core
open Envoy.Api.V2.Endpoint
open Envoy.Api.V2.ListenerNS
open Envoy.Api.V2.Route
open Envoy.Config.Filter.Network.HttpConnectionManager.V2
open Envoy.Type.Matcher
open Google.Protobuf.WellKnownTypes

let createVirtuaHost =
    Cluster(
        Name = "",
        ConnectTimeout = Duration.FromTimeSpan(TimeSpan.FromSeconds 5.0),
        Type = Cluster.Types.DiscoveryType.LogicalDns,
        DnsLookupFamily = Cluster.Types.DnsLookupFamily.V4Only,
        LbPolicy = Cluster.Types.LbPolicy.RoundRobin)
        // LoadAssignment = CreateLoadAssignment(name, endpoints))

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    0 // return an integer exit code
