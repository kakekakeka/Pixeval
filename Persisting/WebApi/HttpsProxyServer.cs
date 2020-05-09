﻿// Pixeval - A Strong, Fast and Flexible Pixiv Client
// Copyright (C) 2019-2020 Dylech30th
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Pixeval.Persisting.WebApi
{
    /// <summary>
    ///     Thanks <a href="https://github.com/tobiichiamane">fish</a>, for login usage only,
    ///     <strong>USE AT YOUR OWN RISK</strong>
    /// </summary>
    public class HttpsProxyServer : IDisposable
    {
        private readonly X509Certificate2 certificate;
        private readonly string ip;
        private readonly TcpListener tcpListener;

        /// <summary>
        ///     Create an <see cref="HttpsProxyServer" /> with specified host, port, target and certificate
        /// </summary>
        /// <param name="host">proxy server host</param>
        /// <param name="port">proxy server port to listen</param>
        /// <param name="targetIP">the ip need to be forwarding to</param>
        /// <param name="x509Certificate2">server certificate</param>
        private HttpsProxyServer(string host, int port, string targetIP, X509Certificate2 x509Certificate2)
        {
            ip = targetIP;
            certificate = x509Certificate2;
            tcpListener = new TcpListener(IPAddress.Parse(host), port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(AcceptTcpClientCallback, tcpListener);
        }

        public void Dispose()
        {
            certificate?.Dispose();
            tcpListener.Stop();
        }

        public static HttpsProxyServer Create(string host, int port, string targetIP, X509Certificate2 x509Certificate2)
        {
            return new HttpsProxyServer(host, port, targetIP, x509Certificate2);
        }

        private async void AcceptTcpClientCallback(IAsyncResult result)
        {
            try
            {
                var listener = (TcpListener) result.AsyncState;
                var client = listener.EndAcceptTcpClient(result);
                listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);
                using (client)
                {
                    var clientStream = client.GetStream();
                    var content = await new StreamReader(clientStream).ReadLineAsync();
                    // content starts with "CONNECT" means it's trying to establish an HTTPS connection
                    if (!content.StartsWith("CONNECT")) return;
                    var writer = new StreamWriter(clientStream);
                    await writer.WriteLineAsync("HTTP/1.1 200 Connection established");
                    await writer.WriteLineAsync($"Timestamp: {DateTime.Now}");
                    await writer.WriteLineAsync("Proxy-agent: Pixeval");
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                    var clientSsl = new SslStream(clientStream, false);
                    // use specify certificate to establish the HTTPS connection
                    await clientSsl.AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls | SslProtocols.Tls13 | SslProtocols.Tls12 | SslProtocols.Tls11, false);
                    // create an HTTP connection to the target IP
                    var serverSsl = await CreateConnection(ip);

                    // forwarding the HTTPS connection without SNI
                    var request = Task.Run(() =>
                    {
                        try
                        {
                            clientSsl.CopyTo(serverSsl);
                        }
                        catch
                        {
                            // ignore
                        }
                    });
                    var response = Task.Run(() =>
                    {
                        try
                        {
                            serverSsl.CopyTo(clientSsl);
                        }
                        catch
                        {
                            // ignore
                        }
                    });
                    Task.WaitAny(request, response);
                    serverSsl.Close();
                }
            }
            catch
            {
                // ignore
            }
        }

        private static async Task<SslStream> CreateConnection(string ip)
        {
            var client = new TcpClient();
            client.Connect(ip, 443);
            var netStream = client.GetStream();
            var sslStream = new SslStream(netStream, false, (sender, certificate, chain, errors) => true);
            try
            {
                await sslStream.AuthenticateAsClientAsync("");
                return sslStream;
            }
            catch
            {
                await sslStream.DisposeAsync();
                throw;
            }
        }
    }
}