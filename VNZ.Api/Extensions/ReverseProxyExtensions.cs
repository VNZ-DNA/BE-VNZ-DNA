using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace VNZ.Api.Extensions;

public static class ReverseProxyExtensions
{
    public static WebApplication UseReverseProxyForwardedHeaders(
        this WebApplication app)
    {
        var reverseProxyEnabled = app.Configuration.GetValue<bool>("ReverseProxy:Enabled");

        if (!reverseProxyEnabled)
        {
            return app;
        }

        var forwardLimit = app.Configuration.GetValue<int?>("ReverseProxy:ForwardLimit");
        var trustForwardedHeaders = app.Configuration.GetValue<bool>("ReverseProxy:TrustForwardedHeaders");

        if (!forwardLimit.HasValue || forwardLimit.Value < 1)
        {
            throw new InvalidOperationException(
                "ReverseProxy:ForwardLimit phải là số nguyên dương khi ReverseProxy:Enabled=true.");
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = forwardLimit.Value
        };

        if (trustForwardedHeaders)
        {
            // Render chỉ cho public traffic đi qua ingress của nền tảng. Khi cờ này bật,
            // service tin header do ingress chuyển tiếp để lấy IP khách thật cho rate limit.
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        }
        else
        {
            var knownProxies = app.Configuration["ReverseProxy:KnownProxies"];

            if (string.IsNullOrWhiteSpace(knownProxies))
            {
                throw new InvalidOperationException(
                    "ReverseProxy:KnownProxies phải được cấu hình khi không tin tất cả forwarded headers.");
            }

            var proxyAddresses = knownProxies.Split(
                ',',
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var proxyAddress in proxyAddresses)
            {
                if (!IPAddress.TryParse(proxyAddress, out var ipAddress))
                {
                    throw new InvalidOperationException(
                        "ReverseProxy:KnownProxies chứa địa chỉ IP không hợp lệ.");
                }

                options.KnownProxies.Add(ipAddress);
            }
        }

        app.UseForwardedHeaders(options);

        return app;
    }
}
