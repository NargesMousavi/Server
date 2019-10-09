using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text;

namespace server
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILogger<Startup> logger)
        {
            app.UseWebSockets();
            app.Use(async (context, next) =>
         {
             if (context.Request.Path == "/ws")
             {
                 if (context.WebSockets.IsWebSocketRequest)
                 {
                     WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                     await StartCommunication(webSocket, logger); //Note: use await to keep active connection.
                 }
                 else
                 {
                     context.Response.StatusCode = 400;
                 }
             }
             else
             {
                 await next();
             }

         });
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!");
            });
        }
        private async Task StartCommunication(WebSocket webSocket, ILogger<Startup> logger)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            var resBuffer = new ArraySegment<byte>(new byte[1024]);
            while (!result.CloseStatus.HasValue)
            {
                var reqMes = Encoding.Default.GetString(buffer.Array).TrimEnd((char)0);//.TrimEnd((char)0
                switch (reqMes)
                {
                    case "Hello":
                        resBuffer = Encoding.Default.GetBytes("Hi");
                        await Task.Delay(1000);//one second delay.
                        await webSocket.SendAsync(resBuffer, result.MessageType, result.EndOfMessage, CancellationToken.None);
                        break;
                    case "Bye":
                        resBuffer = Encoding.Default.GetBytes("Bye");
                        await webSocket.SendAsync(resBuffer, result.MessageType, result.EndOfMessage, CancellationToken.None);
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye for client", CancellationToken.None);
                        return;
                    case "Ping":
                        resBuffer = Encoding.Default.GetBytes("Pong");
                        await webSocket.SendAsync(resBuffer, result.MessageType, result.EndOfMessage, CancellationToken.None);
                        break;
                    default:
                        resBuffer = Encoding.Default.GetBytes("Invalid");
                        await webSocket.SendAsync(resBuffer, result.MessageType, result.EndOfMessage, CancellationToken.None);
                        break;
                }
                buffer = new ArraySegment<byte>(new byte[1024]);
                result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
