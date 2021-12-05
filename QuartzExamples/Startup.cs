﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using QuartzExamples.Jobs;
using QuartzExamples.Services;

namespace QuartzExamples
{
    public class Startup
    {
        private IScheduler _quartzScheduler;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            _quartzScheduler = ConfigureQuartz();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services.AddTransient<SimpleJob>();
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton(provider => _quartzScheduler);
            services.AddControllersWithViews();

            services.AddRazorPages();
        }
        //this code is called when the application stops
        private void OnShutdown()
        {
            //shutdown quartz is not shutdown already
            if (!_quartzScheduler.IsShutdown) _quartzScheduler.Shutdown();
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
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            _quartzScheduler.JobFactory = new AspnetCoreJobFactory(app.ApplicationServices);
            _quartzScheduler.Start().Wait();
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();


            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }

        public IScheduler ConfigureQuartz()
        {

            NameValueCollection props = new NameValueCollection
             {
              { "quartz.serializer.type", "json" },
               { "quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz" },
                 { "quartz.jobStore.dataSource", "default" },
                 { "quartz.dataSource.default.provider", "SqlServer" },
                  { "quartz.dataSource.default.connectionString", "Server=.;Integrated Security=true;Initial Catalog = Quartz" },
                  {"quartz.jobStore.clustered","true" },
                  { "quartz.jobStore.driverDelegateType", "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz" }
              };
            StdSchedulerFactory factory = new StdSchedulerFactory(props);
            var scheduler = factory.GetScheduler().Result;

            scheduler.ListenerManager.AddTriggerListener(new TriggerListener());
            scheduler.ListenerManager.AddJobListener(new JobListener());
            scheduler.ListenerManager.AddSchedulerListener(new SchedulerListener());
            return scheduler;

        }
    }
}
