using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using EventPlanner.Models;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace EventPlanner
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Veritabaný baðlantýsý yapýlandýrmasý
            services.AddDbContext<MyContext>(options => options.UseMySql(
                Configuration["DBInfo:ConnectionString"],
                new MySqlServerVersion(new Version(8, 0, 21)) // MySQL sürümünü belirtin
            ));

            // MVC ve endpoint routing ayarlarý
            services.AddControllersWithViews(); // MVC'yi modern yaklaþýmla kullanmak için güncelledim.

            // Oturum yönetimi yapýlandýrmasý
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2); // Oturum süresi: 2 saat
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // HTTPS güvenliði
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Hata iþleme
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection(); // Tüm HTTP isteklerini HTTPS'e yönlendirin
            app.UseStaticFiles(); // Statik dosyalara eriþim

            app.UseRouting(); // Route yapýlandýrmasý

            app.UseSession(); // Oturum yönetimi middleware'i

            app.UseAuthentication(); // Kimlik doðrulama middleware'i
            app.UseAuthorization(); // Yetkilendirme middleware'i

            // Endpoint routing kullanýmý
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
