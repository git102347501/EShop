using System.Collections.Generic;
using System.Threading.Tasks;
using EasyAbp.EShop.Plugins.FlashSales.Localization;
using EasyAbp.EShop.Plugins.FlashSales.Permissions;
using Volo.Abp.UI.Navigation;

namespace EasyAbp.EShop.Plugins.FlashSales.Web.Menus;

public class FlashSalesMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var l = context.GetLocalizer<FlashSalesResource>();
        //Add main menu items.
        var flashSalesManagementMenuItem = new ApplicationMenuItem(FlashSalesMenus.Prefix, l["Menu:FlashSalesManagement"]);

        if (await context.IsGrantedAsync(FlashSalesPermissions.FlashSalesPlan.Default))
        {
            flashSalesManagementMenuItem.AddItem(
                new ApplicationMenuItem(
                    FlashSalesMenus.FlashSalesPlan,
                    l["Menu:FlashSalesPlan"],
                    "/EShop/Plugins/FlashSales/FlashSalesPlans/FlashSalesPlan"
                )
            );
        }

        if (await context.IsGrantedAsync(FlashSalesPermissions.FlashSalesResult.Default))
        {
            flashSalesManagementMenuItem.AddItem(
                new ApplicationMenuItem(
                    FlashSalesMenus.FlashSalesResult,
                    l["Menu:FlashSalesResult"],
                    "/EShop/Plugins/FlashSales/FlashSalesResults/FlashSalesResult"
                )
            );
        }

        if (!flashSalesManagementMenuItem.Items.IsNullOrEmpty())
        {
            var eShopMenuItem = context.Menu.Items.GetOrAdd(i => i.Name == FlashSalesMenus.ModuleGroupPrefix,
                () => new ApplicationMenuItem(FlashSalesMenus.ModuleGroupPrefix, l["Menu:EasyAbpEShop"], icon: "fa fa-shopping-bag"));

            eShopMenuItem.Items.Add(flashSalesManagementMenuItem);
        }
    }
}
