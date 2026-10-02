using System.Globalization;
using System.Net;

using Encina.Validation;

using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace Encina.PropertyTests.Validation.Endpoints;

/// <summary>
/// Property-based tests for <see cref="EndpointValidator"/> (#852): host classification is stable
/// across every textual form of an address, and the strict policy holds for whole ranges.
/// </summary>
[Trait("Category", "Property")]
public sealed class EndpointValidatorPropertyTests
{
    private static readonly EndpointPolicy Lenient = EndpointPolicy.ForHttps(allowInsecureHttp: true, allowLocalEndpoints: true);
    private static readonly EndpointPolicy Strict = EndpointPolicy.ForHttps(allowInsecureHttp: false, allowLocalEndpoints: false);

    /// <summary>
    /// Property: an IPv4 address, its IPv4-mapped IPv6 form, its trailing-dot form and its decimal
    /// integer form always classify the same.
    /// </summary>
    [Property(MaxTest = 500)]
    public Property Property_IPv4Forms_ClassifyIdentically()
    {
        return Prop.ForAll(
            Arb.From(GenIPv4()),
            address =>
            {
                var dotted = address.ToString();
                var expected = EndpointValidator.ClassifyHost(dotted);
                var decimalForm = ToUInt32(address).ToString(CultureInfo.InvariantCulture);

                return EndpointValidator.ClassifyHost("::ffff:" + dotted) == expected
                    && EndpointValidator.ClassifyHost($"[{address.MapToIPv6()}]") == expected
                    && EndpointValidator.ClassifyHost(dotted + ".") == expected
                    && EndpointValidator.ClassifyHost(decimalForm) == expected
                    && EndpointValidator.ClassifyAddress(address.MapToIPv6()) == expected;
            });
    }

    /// <summary>
    /// Property: the IPv4 address carried by IPv4-translated, 6to4 and Teredo IPv6 addresses
    /// classifies the same as the IPv4 address itself.
    /// </summary>
    [Property(MaxTest = 500)]
    public Property Property_TunnelledIPv4Forms_ClassifyLikeTheirIPv4()
    {
        return Prop.ForAll(
            Arb.From(GenIPv4()),
            Arb.From(Gen.ArrayOf(Gen.Choose(0, 255), 8)),
            (address, noise) =>
            {
                var v4 = address.GetAddressBytes();
                var expected = EndpointValidator.ClassifyAddress(address);

                var translated = new byte[16];
                translated[8] = 0xFF;
                translated[9] = 0xFF;
                v4.CopyTo(translated, 12);

                var sixToFour = new byte[16];
                sixToFour[0] = 0x20;
                sixToFour[1] = 0x02;
                v4.CopyTo(sixToFour, 2);
                for (var i = 0; i < 8; i++)
                {
                    sixToFour[i + 6] = (byte)noise[i];
                }

                var teredo = new byte[16];
                teredo[0] = 0x20;
                teredo[1] = 0x01;
                for (var i = 0; i < 8; i++)
                {
                    teredo[i + 4] = (byte)noise[i];
                }

                for (var i = 0; i < 4; i++)
                {
                    teredo[i + 12] = (byte)~v4[i];
                }

                return EndpointValidator.ClassifyAddress(new IPAddress(translated)) == expected
                    && EndpointValidator.ClassifyAddress(new IPAddress(sixToFour)) == expected
                    && EndpointValidator.ClassifyAddress(new IPAddress(teredo)) == expected;
            });
    }

    /// <summary>
    /// Property: every address in 100.64.0.0/10 (carrier-grade NAT) and fec0::/10 (site-local) is
    /// private, so it is accepted by default and rejected only by RejectPrivateNetworks without the
    /// local opt-out.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Property_CgnatAndSiteLocal_ArePrivate()
    {
        var cgnat = Gen.Choose(64, 127).SelectMany(second =>
            Gen.Choose(0, 255).SelectMany(c =>
                Gen.Choose(0, 255).Select(d => new IPAddress(new[] { (byte)100, (byte)second, (byte)c, (byte)d }))));
        var rejecting = new EndpointPolicy { AllowedSchemes = ["https"], RejectPrivateNetworks = true };
        var rejectingWithOptOut = new EndpointPolicy { AllowedSchemes = ["https"], RejectPrivateNetworks = true, AllowLocalEndpoints = true };

        return Prop.ForAll(
            Arb.From(cgnat),
            Arb.From(GenIPv6WithFirstBytes(0xFE, 0xC0, 0xFF)),
            (carrierNat, siteLocal) =>
            {
                var uri = new Uri($"https://{carrierNat}/");

                return EndpointValidator.ClassifyAddress(carrierNat) == EndpointHostKind.Private
                    && EndpointValidator.ClassifyAddress(siteLocal) == EndpointHostKind.Private
                    && EndpointValidator.ValidateUri(uri, "Endpoint", Strict) is null
                    && EndpointValidator.ValidateUri(uri, "Endpoint", rejecting) is not null
                    && EndpointValidator.ValidateUri(uri, "Endpoint", rejectingWithOptOut) is null;
            });
    }

    /// <summary>
    /// Property: every address in 127.0.0.0/8 is loopback, rejected by the strict policy and
    /// accepted once local endpoints are allowed.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Property_Loopback8_IsLoopbackAndNeedsOptOut()
    {
        return Prop.ForAll(
            Arb.From(GenIPv4WithFirstOctet(127)),
            address =>
            {
                var uri = new Uri($"https://{address}/");

                return EndpointValidator.ClassifyAddress(address) == EndpointHostKind.Loopback
                    && EndpointValidator.ValidateUri(uri, "Endpoint", Strict) is not null
                    && EndpointValidator.ValidateUri(uri, "Endpoint", Lenient) is null;
            });
    }

    /// <summary>
    /// Property: every address in 169.254.0.0/16 and 0.0.0.0/8 is rejected even with both opt-outs.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Property_LinkLocalAndUnspecified_AreAlwaysRejected()
    {
        var gen = Gen.OneOf(GenIPv4WithPrefix(169, 254), GenIPv4WithFirstOctet(0));

        return Prop.ForAll(
            Arb.From(gen),
            address =>
            {
                var kind = EndpointValidator.ClassifyAddress(address);
                var mapped = new Uri($"https://[{address.MapToIPv6()}]/");

                return kind is EndpointHostKind.LinkLocal or EndpointHostKind.CloudMetadata or EndpointHostKind.Unspecified
                    && EndpointValidator.ValidateUri(new Uri($"http://{address}/"), "Endpoint", Lenient) is not null
                    && EndpointValidator.ValidateUri(mapped, "Endpoint", Lenient) is not null;
            });
    }

    /// <summary>
    /// Property: fe80::/10 is link-local, while ff80::/10 (multicast) never is.
    /// </summary>
    [Property(MaxTest = 300)]
    public Property Property_Fe80IsLinkLocal_Ff80IsNot()
    {
        return Prop.ForAll(
            Arb.From(GenIPv6WithFirstBytes(0xFE, 0x80, 0xBF)),
            Arb.From(GenIPv6WithFirstBytes(0xFF, 0x80, 0xBF)),
            (linkLocal, multicast) =>
                EndpointValidator.ClassifyAddress(linkLocal) == EndpointHostKind.LinkLocal
                && EndpointValidator.ClassifyAddress(multicast) != EndpointHostKind.LinkLocal);
    }

    /// <summary>
    /// Property: the classification of a DNS name does not depend on letter case or a trailing dot.
    /// </summary>
    [Property(MaxTest = 200)]
    public Property Property_NameClassification_IgnoresCaseAndTrailingDot()
    {
        var gen = Gen.Elements("localhost", "api.localhost", "metadata.google.internal", "vault.example.com", "ip6-loopback");

        return Prop.ForAll(
            Arb.From(gen),
            name =>
            {
                var expected = EndpointValidator.ClassifyHost(name);

                return EndpointValidator.ClassifyHost(name.ToUpperInvariant()) == expected
                    && EndpointValidator.ClassifyHost(name + ".") == expected;
            });
    }

    private static Gen<IPAddress> GenIPv4() =>
        Gen.Choose(0, 255).SelectMany(a =>
            Gen.Choose(0, 255).SelectMany(b =>
                Gen.Choose(0, 255).SelectMany(c =>
                    Gen.Choose(0, 255).Select(d => new IPAddress(new[] { (byte)a, (byte)b, (byte)c, (byte)d })))));

    private static Gen<IPAddress> GenIPv4WithFirstOctet(int first) =>
        Gen.Choose(0, 255).SelectMany(b =>
            Gen.Choose(0, 255).SelectMany(c =>
                Gen.Choose(0, 255).Select(d => new IPAddress(new[] { (byte)first, (byte)b, (byte)c, (byte)d }))));

    private static Gen<IPAddress> GenIPv4WithPrefix(int first, int second) =>
        Gen.Choose(0, 255).SelectMany(c =>
            Gen.Choose(0, 255).Select(d => new IPAddress(new[] { (byte)first, (byte)second, (byte)c, (byte)d })));

    private static Gen<IPAddress> GenIPv6WithFirstBytes(byte first, int secondMin, int secondMax) =>
        Gen.Choose(secondMin, secondMax).SelectMany(second =>
            Gen.ArrayOf(Gen.Choose(0, 255), 14).Select(rest =>
            {
                var bytes = new byte[16];
                bytes[0] = first;
                bytes[1] = (byte)second;
                for (var i = 0; i < 14; i++)
                {
                    bytes[i + 2] = (byte)rest[i];
                }

                return new IPAddress(bytes);
            }));

    private static uint ToUInt32(IPAddress address)
    {
        var b = address.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }
}
