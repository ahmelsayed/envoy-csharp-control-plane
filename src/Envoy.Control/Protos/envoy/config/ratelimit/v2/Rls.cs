// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: envoy/config/ratelimit/v2/rls.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Envoy.Config.Ratelimit.V2 {

  /// <summary>Holder for reflection information generated from envoy/config/ratelimit/v2/rls.proto</summary>
  public static partial class RlsReflection {

    #region Descriptor
    /// <summary>File descriptor for envoy/config/ratelimit/v2/rls.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static RlsReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "CiNlbnZveS9jb25maWcvcmF0ZWxpbWl0L3YyL3Jscy5wcm90bxIZZW52b3ku",
            "Y29uZmlnLnJhdGVsaW1pdC52MhokZW52b3kvYXBpL3YyL2NvcmUvZ3JwY19z",
            "ZXJ2aWNlLnByb3RvGhd2YWxpZGF0ZS92YWxpZGF0ZS5wcm90byJmChZSYXRl",
            "TGltaXRTZXJ2aWNlQ29uZmlnEkAKDGdycGNfc2VydmljZRgCIAEoCzIeLmVu",
            "dm95LmFwaS52Mi5jb3JlLkdycGNTZXJ2aWNlQgq66cADBYoBAhABSgQIARAC",
            "SgQIAxAEQjUKJ2lvLmVudm95cHJveHkuZW52b3kuY29uZmlnLnJhdGVsaW1p",
            "dC52MkIIUmxzUHJvdG9QAWIGcHJvdG8z"));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Envoy.Api.V2.Core.GrpcServiceReflection.Descriptor, global::Validate.ValidateReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Envoy.Config.Ratelimit.V2.RateLimitServiceConfig), global::Envoy.Config.Ratelimit.V2.RateLimitServiceConfig.Parser, new[]{ "GrpcService" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  /// Rate limit :ref:`configuration overview &lt;config_rate_limit_service>`.
  /// </summary>
  public sealed partial class RateLimitServiceConfig : pb::IMessage<RateLimitServiceConfig> {
    private static readonly pb::MessageParser<RateLimitServiceConfig> _parser = new pb::MessageParser<RateLimitServiceConfig>(() => new RateLimitServiceConfig());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<RateLimitServiceConfig> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Envoy.Config.Ratelimit.V2.RlsReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RateLimitServiceConfig() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RateLimitServiceConfig(RateLimitServiceConfig other) : this() {
      grpcService_ = other.grpcService_ != null ? other.grpcService_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public RateLimitServiceConfig Clone() {
      return new RateLimitServiceConfig(this);
    }

    /// <summary>Field number for the "grpc_service" field.</summary>
    public const int GrpcServiceFieldNumber = 2;
    private global::Envoy.Api.V2.Core.GrpcService grpcService_;
    /// <summary>
    /// Specifies the gRPC service that hosts the rate limit service. The client
    /// will connect to this cluster when it needs to make rate limit service
    /// requests.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Envoy.Api.V2.Core.GrpcService GrpcService {
      get { return grpcService_; }
      set {
        grpcService_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as RateLimitServiceConfig);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(RateLimitServiceConfig other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(GrpcService, other.GrpcService)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (grpcService_ != null) hash ^= GrpcService.GetHashCode();
      if (_unknownFields != null) {
        hash ^= _unknownFields.GetHashCode();
      }
      return hash;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override string ToString() {
      return pb::JsonFormatter.ToDiagnosticString(this);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void WriteTo(pb::CodedOutputStream output) {
      if (grpcService_ != null) {
        output.WriteRawTag(18);
        output.WriteMessage(GrpcService);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (grpcService_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(GrpcService);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(RateLimitServiceConfig other) {
      if (other == null) {
        return;
      }
      if (other.grpcService_ != null) {
        if (grpcService_ == null) {
          GrpcService = new global::Envoy.Api.V2.Core.GrpcService();
        }
        GrpcService.MergeFrom(other.GrpcService);
      }
      _unknownFields = pb::UnknownFieldSet.MergeFrom(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(pb::CodedInputStream input) {
      uint tag;
      while ((tag = input.ReadTag()) != 0) {
        switch(tag) {
          default:
            _unknownFields = pb::UnknownFieldSet.MergeFieldFrom(_unknownFields, input);
            break;
          case 18: {
            if (grpcService_ == null) {
              GrpcService = new global::Envoy.Api.V2.Core.GrpcService();
            }
            input.ReadMessage(GrpcService);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
