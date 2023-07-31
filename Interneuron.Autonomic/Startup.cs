//BEGIN LICENSE BLOCK 
//Interneuron Autonomic

//Copyright(C) 2023  Interneuron Holdings Ltd

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.

//See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.If not, see<http://www.gnu.org/licenses/>.
//END LICENSE BLOCK 


using System;
using InterneuronAutonomic.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Interneuron.Autonomic
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllHeaders",
                      builder =>
                      {
                          builder.AllowAnyOrigin()
                                 .AllowAnyHeader()
                                 .AllowAnyMethod();
                      });
            });

            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            });

            services.AddHttpContextAccessor();

            string SynapseDynamicAPIURI = Configuration.GetSection("DynamicAPISettings").GetSection("uri").Value;
            services.AddHttpClient();
            services.AddHttpClient<DynamicAPIClient>(clientConfig =>
            {
                clientConfig.BaseAddress = new Uri(SynapseDynamicAPIURI);
            });

            //Authorization

            services.AddAuthentication("Bearer")
              .AddIdentityServerAuthentication(options =>
              {
                  options.Authority = Configuration["Settings:AuthorizationAuthority"];
                  options.RequireHttpsMetadata = false;
                  options.ApiName = Configuration["Settings:AuthorizationAudience"];
                  options.EnableCaching = false;

              });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("DynamicAPIWriters", builder =>
                {
                    builder.RequireClaim(Configuration["Settings:SynapseRolesClaimType"], Configuration["Settings:DynamicAPIWriteAccessRole"]);
                    builder.RequireScope(Configuration["Settings:WriteAccessAPIScope"]);
                });
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("DynamicAPIReaders", builder =>
                {
                    builder.RequireClaim(Configuration["Settings:SynapseRolesClaimType"], Configuration["Settings:DynamicAPIReadAccessRole"]);
                    builder.RequireScope(Configuration["Settings:ReadAccessAPIScope"]);
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseAuthentication();

            app.UseCors("AllowAllHeaders");
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
