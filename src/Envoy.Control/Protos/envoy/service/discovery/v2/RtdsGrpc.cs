// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: envoy/service/discovery/v2/rtds.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace Envoy.Service.Discovery.V2 {
  /// <summary>
  /// Discovery service for Runtime resources.
  /// </summary>
  public static partial class RuntimeDiscoveryService
  {
    static readonly string __ServiceName = "envoy.service.discovery.v2.RuntimeDiscoveryService";

    static readonly grpc::Marshaller<global::Envoy.Api.V2.DiscoveryRequest> __Marshaller_envoy_api_v2_DiscoveryRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Envoy.Api.V2.DiscoveryRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Envoy.Api.V2.DiscoveryResponse> __Marshaller_envoy_api_v2_DiscoveryResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Envoy.Api.V2.DiscoveryResponse.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Envoy.Api.V2.DeltaDiscoveryRequest> __Marshaller_envoy_api_v2_DeltaDiscoveryRequest = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Envoy.Api.V2.DeltaDiscoveryRequest.Parser.ParseFrom);
    static readonly grpc::Marshaller<global::Envoy.Api.V2.DeltaDiscoveryResponse> __Marshaller_envoy_api_v2_DeltaDiscoveryResponse = grpc::Marshallers.Create((arg) => global::Google.Protobuf.MessageExtensions.ToByteArray(arg), global::Envoy.Api.V2.DeltaDiscoveryResponse.Parser.ParseFrom);

    static readonly grpc::Method<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse> __Method_StreamRuntime = new grpc::Method<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse>(
        grpc::MethodType.DuplexStreaming,
        __ServiceName,
        "StreamRuntime",
        __Marshaller_envoy_api_v2_DiscoveryRequest,
        __Marshaller_envoy_api_v2_DiscoveryResponse);

    static readonly grpc::Method<global::Envoy.Api.V2.DeltaDiscoveryRequest, global::Envoy.Api.V2.DeltaDiscoveryResponse> __Method_DeltaRuntime = new grpc::Method<global::Envoy.Api.V2.DeltaDiscoveryRequest, global::Envoy.Api.V2.DeltaDiscoveryResponse>(
        grpc::MethodType.DuplexStreaming,
        __ServiceName,
        "DeltaRuntime",
        __Marshaller_envoy_api_v2_DeltaDiscoveryRequest,
        __Marshaller_envoy_api_v2_DeltaDiscoveryResponse);

    static readonly grpc::Method<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse> __Method_FetchRuntime = new grpc::Method<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "FetchRuntime",
        __Marshaller_envoy_api_v2_DiscoveryRequest,
        __Marshaller_envoy_api_v2_DiscoveryResponse);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Envoy.Service.Discovery.V2.RtdsReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of RuntimeDiscoveryService</summary>
    [grpc::BindServiceMethod(typeof(RuntimeDiscoveryService), "BindService")]
    public abstract partial class RuntimeDiscoveryServiceBase
    {
      public virtual global::System.Threading.Tasks.Task StreamRuntime(grpc::IAsyncStreamReader<global::Envoy.Api.V2.DiscoveryRequest> requestStream, grpc::IServerStreamWriter<global::Envoy.Api.V2.DiscoveryResponse> responseStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task DeltaRuntime(grpc::IAsyncStreamReader<global::Envoy.Api.V2.DeltaDiscoveryRequest> requestStream, grpc::IServerStreamWriter<global::Envoy.Api.V2.DeltaDiscoveryResponse> responseStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Envoy.Api.V2.DiscoveryResponse> FetchRuntime(global::Envoy.Api.V2.DiscoveryRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for RuntimeDiscoveryService</summary>
    public partial class RuntimeDiscoveryServiceClient : grpc::ClientBase<RuntimeDiscoveryServiceClient>
    {
      /// <summary>Creates a new client for RuntimeDiscoveryService</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public RuntimeDiscoveryServiceClient(grpc::ChannelBase channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for RuntimeDiscoveryService that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public RuntimeDiscoveryServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected RuntimeDiscoveryServiceClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected RuntimeDiscoveryServiceClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual grpc::AsyncDuplexStreamingCall<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse> StreamRuntime(grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return StreamRuntime(new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncDuplexStreamingCall<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse> StreamRuntime(grpc::CallOptions options)
      {
        return CallInvoker.AsyncDuplexStreamingCall(__Method_StreamRuntime, null, options);
      }
      public virtual grpc::AsyncDuplexStreamingCall<global::Envoy.Api.V2.DeltaDiscoveryRequest, global::Envoy.Api.V2.DeltaDiscoveryResponse> DeltaRuntime(grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return DeltaRuntime(new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncDuplexStreamingCall<global::Envoy.Api.V2.DeltaDiscoveryRequest, global::Envoy.Api.V2.DeltaDiscoveryResponse> DeltaRuntime(grpc::CallOptions options)
      {
        return CallInvoker.AsyncDuplexStreamingCall(__Method_DeltaRuntime, null, options);
      }
      public virtual global::Envoy.Api.V2.DiscoveryResponse FetchRuntime(global::Envoy.Api.V2.DiscoveryRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return FetchRuntime(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Envoy.Api.V2.DiscoveryResponse FetchRuntime(global::Envoy.Api.V2.DiscoveryRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_FetchRuntime, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Envoy.Api.V2.DiscoveryResponse> FetchRuntimeAsync(global::Envoy.Api.V2.DiscoveryRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return FetchRuntimeAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Envoy.Api.V2.DiscoveryResponse> FetchRuntimeAsync(global::Envoy.Api.V2.DiscoveryRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_FetchRuntime, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override RuntimeDiscoveryServiceClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new RuntimeDiscoveryServiceClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(RuntimeDiscoveryServiceBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_StreamRuntime, serviceImpl.StreamRuntime)
          .AddMethod(__Method_DeltaRuntime, serviceImpl.DeltaRuntime)
          .AddMethod(__Method_FetchRuntime, serviceImpl.FetchRuntime).Build();
    }

    /// <summary>Register service method with a service binder with or without implementation. Useful when customizing the  service binding logic.
    /// Note: this method is part of an experimental API that can change or be removed without any prior notice.</summary>
    /// <param name="serviceBinder">Service methods will be bound by calling <c>AddMethod</c> on this object.</param>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static void BindService(grpc::ServiceBinderBase serviceBinder, RuntimeDiscoveryServiceBase serviceImpl)
    {
      serviceBinder.AddMethod(__Method_StreamRuntime, serviceImpl == null ? null : new grpc::DuplexStreamingServerMethod<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse>(serviceImpl.StreamRuntime));
      serviceBinder.AddMethod(__Method_DeltaRuntime, serviceImpl == null ? null : new grpc::DuplexStreamingServerMethod<global::Envoy.Api.V2.DeltaDiscoveryRequest, global::Envoy.Api.V2.DeltaDiscoveryResponse>(serviceImpl.DeltaRuntime));
      serviceBinder.AddMethod(__Method_FetchRuntime, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::Envoy.Api.V2.DiscoveryRequest, global::Envoy.Api.V2.DiscoveryResponse>(serviceImpl.FetchRuntime));
    }

  }
}
#endregion