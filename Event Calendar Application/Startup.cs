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
            // Veritaban� ba�lant�s� yap�land�rmas�
            services.AddDbContext<MyContext>(options => options.UseMySql(
                Configuration["DBInfo:ConnectionString"],
                new MySqlServerVersion(new Version(8, 0, 21)) // MySQL s�r�m�n� belirtin
            ));

            // MVC ve endpoint routing ayarlar�
            services.AddControllersWithViews(); // MVC'yi modern yakla��mla kullanmak i�in g�ncelledim.

            // Oturum y�netimi yap�land�rmas�
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(2); // Oturum s�resi: 2 saat
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always; // HTTPS g�venli�i
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Hata i�leme
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection(); // T�m HTTP isteklerini HTTPS'e y�nlendirin
            app.UseStaticFiles(); // Statik dosyalara eri�im

            app.UseRouting(); // Route yap�land�rmas�

            app.UseSession(); // Oturum y�netimi middleware'i

            app.UseAuthentication(); // Kimlik do�rulama middleware'i
            app.UseAuthorization(); // Yetkilendirme middleware'i

            // Endpoint routing kullan�m�
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
