﻿using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(MyKatanaHost.MyStartup))]

namespace MyKatanaHost
{
    public class MyStartup
    {
        public void Configuration(IAppBuilder app)
        {
            // New code: Add the error page middleware to the pipeline. 
            app.UseErrorPage();

            app.Run(context =>
            {
                // New code: Throw an exception for this URI path.
                if (context.Request.Path.Value == "/fail")
                {
                    throw new Exception("Random exception");
                }

                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("Hello, world.");
            });
        }
    }
}
