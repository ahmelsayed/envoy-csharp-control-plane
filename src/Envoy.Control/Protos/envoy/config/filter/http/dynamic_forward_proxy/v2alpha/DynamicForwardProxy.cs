// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: envoy/config/filter/http/dynamic_forward_proxy/v2alpha/dynamic_forward_proxy.proto
// </auto-generated>
#pragma warning disable 1591, 0612, 3021
#region Designer generated code

using pb = global::Google.Protobuf;
using pbc = global::Google.Protobuf.Collections;
using pbr = global::Google.Protobuf.Reflection;
using scg = global::System.Collections.Generic;
namespace Envoy.Config.Filter.Http.DynamicForwardProxy.V2Alpha {

  /// <summary>Holder for reflection information generated from envoy/config/filter/http/dynamic_forward_proxy/v2alpha/dynamic_forward_proxy.proto</summary>
  public static partial class DynamicForwardProxyReflection {

    #region Descriptor
    /// <summary>File descriptor for envoy/config/filter/http/dynamic_forward_proxy/v2alpha/dynamic_forward_proxy.proto</summary>
    public static pbr::FileDescriptor Descriptor {
      get { return descriptor; }
    }
    private static pbr::FileDescriptor descriptor;

    static DynamicForwardProxyReflection() {
      byte[] descriptorData = global::System.Convert.FromBase64String(
          string.Concat(
            "ClJlbnZveS9jb25maWcvZmlsdGVyL2h0dHAvZHluYW1pY19mb3J3YXJkX3By",
            "b3h5L3YyYWxwaGEvZHluYW1pY19mb3J3YXJkX3Byb3h5LnByb3RvEjZlbnZv",
            "eS5jb25maWcuZmlsdGVyLmh0dHAuZHluYW1pY19mb3J3YXJkX3Byb3h5LnYy",
            "YWxwaGEaQWVudm95L2NvbmZpZy9jb21tb24vZHluYW1pY19mb3J3YXJkX3By",
            "b3h5L3YyYWxwaGEvZG5zX2NhY2hlLnByb3RvGhd2YWxpZGF0ZS92YWxpZGF0",
            "ZS5wcm90byJ3CgxGaWx0ZXJDb25maWcSZwoQZG5zX2NhY2hlX2NvbmZpZxgB",
            "IAEoCzJBLmVudm95LmNvbmZpZy5jb21tb24uZHluYW1pY19mb3J3YXJkX3By",
            "b3h5LnYyYWxwaGEuRG5zQ2FjaGVDb25maWdCCrrpwAMFigECEAFCYgpEaW8u",
            "ZW52b3lwcm94eS5lbnZveS5jb25maWcuZmlsdGVyLmh0dHAuZHluYW1pY19m",
            "b3J3YXJkX3Byb3h5LnYyYWxwaGFCGER5bmFtaWNGb3J3YXJkUHJveHlQcm90",
            "b1ABYgZwcm90bzM="));
      descriptor = pbr::FileDescriptor.FromGeneratedCode(descriptorData,
          new pbr::FileDescriptor[] { global::Envoy.Config.Common.DynamicForwardProxy.V2Alpha.DnsCacheReflection.Descriptor, global::Validate.ValidateReflection.Descriptor, },
          new pbr::GeneratedClrTypeInfo(null, null, new pbr::GeneratedClrTypeInfo[] {
            new pbr::GeneratedClrTypeInfo(typeof(global::Envoy.Config.Filter.Http.DynamicForwardProxy.V2Alpha.FilterConfig), global::Envoy.Config.Filter.Http.DynamicForwardProxy.V2Alpha.FilterConfig.Parser, new[]{ "DnsCacheConfig" }, null, null, null, null)
          }));
    }
    #endregion

  }
  #region Messages
  /// <summary>
  /// Configuration for the dynamic forward proxy HTTP filter. See the :ref:`architecture overview
  /// &lt;arch_overview_http_dynamic_forward_proxy>` for more information.
  /// </summary>
  public sealed partial class FilterConfig : pb::IMessage<FilterConfig> {
    private static readonly pb::MessageParser<FilterConfig> _parser = new pb::MessageParser<FilterConfig>(() => new FilterConfig());
    private pb::UnknownFieldSet _unknownFields;
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pb::MessageParser<FilterConfig> Parser { get { return _parser; } }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public static pbr::MessageDescriptor Descriptor {
      get { return global::Envoy.Config.Filter.Http.DynamicForwardProxy.V2Alpha.DynamicForwardProxyReflection.Descriptor.MessageTypes[0]; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    pbr::MessageDescriptor pb::IMessage.Descriptor {
      get { return Descriptor; }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public FilterConfig() {
      OnConstruction();
    }

    partial void OnConstruction();

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public FilterConfig(FilterConfig other) : this() {
      dnsCacheConfig_ = other.dnsCacheConfig_ != null ? other.dnsCacheConfig_.Clone() : null;
      _unknownFields = pb::UnknownFieldSet.Clone(other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public FilterConfig Clone() {
      return new FilterConfig(this);
    }

    /// <summary>Field number for the "dns_cache_config" field.</summary>
    public const int DnsCacheConfigFieldNumber = 1;
    private global::Envoy.Config.Common.DynamicForwardProxy.V2Alpha.DnsCacheConfig dnsCacheConfig_;
    /// <summary>
    /// The DNS cache configuration that the filter will attach to. Note this configuration must
    /// match that of associated :ref:`dynamic forward proxy cluster configuration
    /// &lt;envoy_api_field_config.cluster.dynamic_forward_proxy.v2alpha.ClusterConfig.dns_cache_config>`.
    /// </summary>
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public global::Envoy.Config.Common.DynamicForwardProxy.V2Alpha.DnsCacheConfig DnsCacheConfig {
      get { return dnsCacheConfig_; }
      set {
        dnsCacheConfig_ = value;
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override bool Equals(object other) {
      return Equals(other as FilterConfig);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public bool Equals(FilterConfig other) {
      if (ReferenceEquals(other, null)) {
        return false;
      }
      if (ReferenceEquals(other, this)) {
        return true;
      }
      if (!object.Equals(DnsCacheConfig, other.DnsCacheConfig)) return false;
      return Equals(_unknownFields, other._unknownFields);
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public override int GetHashCode() {
      int hash = 1;
      if (dnsCacheConfig_ != null) hash ^= DnsCacheConfig.GetHashCode();
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
      if (dnsCacheConfig_ != null) {
        output.WriteRawTag(10);
        output.WriteMessage(DnsCacheConfig);
      }
      if (_unknownFields != null) {
        _unknownFields.WriteTo(output);
      }
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public int CalculateSize() {
      int size = 0;
      if (dnsCacheConfig_ != null) {
        size += 1 + pb::CodedOutputStream.ComputeMessageSize(DnsCacheConfig);
      }
      if (_unknownFields != null) {
        size += _unknownFields.CalculateSize();
      }
      return size;
    }

    [global::System.Diagnostics.DebuggerNonUserCodeAttribute]
    public void MergeFrom(FilterConfig other) {
      if (other == null) {
        return;
      }
      if (other.dnsCacheConfig_ != null) {
        if (dnsCacheConfig_ == null) {
          DnsCacheConfig = new global::Envoy.Config.Common.DynamicForwardProxy.V2Alpha.DnsCacheConfig();
        }
        DnsCacheConfig.MergeFrom(other.DnsCacheConfig);
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
          case 10: {
            if (dnsCacheConfig_ == null) {
              DnsCacheConfig = new global::Envoy.Config.Common.DynamicForwardProxy.V2Alpha.DnsCacheConfig();
            }
            input.ReadMessage(DnsCacheConfig);
            break;
          }
        }
      }
    }

  }

  #endregion

}

#endregion Designer generated code
