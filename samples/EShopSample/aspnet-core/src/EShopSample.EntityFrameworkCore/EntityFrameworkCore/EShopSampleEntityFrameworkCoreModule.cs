﻿using EasyAbp.BookingService.EntityFrameworkCore;
using EasyAbp.EShop.EntityFrameworkCore;
using EasyAbp.EShop.Plugins.Baskets.EntityFrameworkCore;
using EasyAbp.EShop.Plugins.Booking.EntityFrameworkCore;
using EasyAbp.EShop.Plugins.Coupons.EntityFrameworkCore;
using EasyAbp.EShop.Plugins.FlashSales.EntityFrameworkCore;
using EasyAbp.PaymentService.EntityFrameworkCore;
using EasyAbp.PaymentService.Prepayment.EntityFrameworkCore;
using EasyAbp.PaymentService.WeChatPay.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.SqlServer;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.IdentityServer.EntityFrameworkCore;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace EShopSample.EntityFrameworkCore
{
    [DependsOn(
        typeof(EShopSampleDomainModule),
        typeof(AbpIdentityEntityFrameworkCoreModule),
        typeof(AbpIdentityServerEntityFrameworkCoreModule),
        typeof(AbpPermissionManagementEntityFrameworkCoreModule),
        typeof(AbpSettingManagementEntityFrameworkCoreModule),
        typeof(AbpEntityFrameworkCoreSqlServerModule),
        typeof(AbpBackgroundJobsEntityFrameworkCoreModule),
        typeof(AbpAuditLoggingEntityFrameworkCoreModule),
        typeof(AbpTenantManagementEntityFrameworkCoreModule),
        typeof(AbpFeatureManagementEntityFrameworkCoreModule),
        typeof(EShopEntityFrameworkCoreModule),
        typeof(EShopPluginsBasketsEntityFrameworkCoreModule),
        typeof(EShopPluginsBookingEntityFrameworkCoreModule),
        typeof(EShopPluginsCouponsEntityFrameworkCoreModule),
        typeof(EShopPluginsFlashSalesEntityFrameworkCoreModule),
        typeof(PaymentServiceEntityFrameworkCoreModule),
        typeof(PaymentServiceWeChatPayEntityFrameworkCoreModule),
        typeof(PaymentServicePrepaymentEntityFrameworkCoreModule),
        typeof(BookingServiceEntityFrameworkCoreModule)
    )]
    public class EShopSampleEntityFrameworkCoreModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            EShopSampleEfCoreEntityExtensionMappings.Configure();
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAbpDbContext<EShopSampleDbContext>(options =>
            {
                /* Remove "includeAllEntities: true" to create
                 * default repositories only for aggregate roots */
                options.AddDefaultRepositories(includeAllEntities: true);
            });

            Configure<AbpDbContextOptions>(options =>
            {
                /* The main point to change your DBMS.
                 * See also EShopSampleDbContextFactory for EF Core tooling. */
                options.UseSqlServer();
            });
        }
    }
}
