﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EasyAbp.EShop.Products.Options.ProductGroups;
using EasyAbp.EShop.Products.ProductCategories;
using EasyAbp.EShop.Products.ProductDetails;
using EasyAbp.EShop.Products.ProductInventories;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace EasyAbp.EShop.Products.Products
{
    public class ProductManager : DomainService, IProductManager
    {
        private readonly IProductRepository _productRepository;
        private readonly IProductPriceProvider _productPriceProvider;
        private readonly IProductDetailRepository _productDetailRepository;
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IProductInventoryProviderResolver _productInventoryProviderResolver;
        private readonly IAttributeOptionIdsSerializer _attributeOptionIdsSerializer;
        private readonly IProductGroupConfigurationProvider _productGroupConfigurationProvider;

        public ProductManager(
            IProductRepository productRepository,
            IProductPriceProvider productPriceProvider,
            IProductDetailRepository productDetailRepository,
            IProductCategoryRepository productCategoryRepository,
            IProductInventoryProviderResolver productInventoryProviderResolver,
            IAttributeOptionIdsSerializer attributeOptionIdsSerializer,
            IProductGroupConfigurationProvider productGroupConfigurationProvider)
        {
            _productRepository = productRepository;
            _productPriceProvider = productPriceProvider;
            _productDetailRepository = productDetailRepository;
            _productCategoryRepository = productCategoryRepository;
            _productInventoryProviderResolver = productInventoryProviderResolver;
            _attributeOptionIdsSerializer = attributeOptionIdsSerializer;
            _productGroupConfigurationProvider = productGroupConfigurationProvider;
        }

        [UnitOfWork(true)]
        public virtual async Task<Product> CreateAsync(Product product, IEnumerable<Guid> categoryIds = null)
        {
            product.TrimUniqueName();

            await CheckProductGroupNameAsync(product);

            await CheckInventoryProviderNameAsync(product);

            await CheckProductUniqueNameAsync(product);

            await _productRepository.InsertAsync(product, autoSave: true);

            await CheckProductDetailAsync(product);

            await UpdateProductCategoriesAsync(product.Id, categoryIds);

            return product;
        }

        protected virtual Task CheckProductGroupNameAsync(Product product)
        {
            if (_productGroupConfigurationProvider.Get(product.ProductGroupName) == null)
            {
                throw new NonexistentProductGroupException(product.ProductGroupName);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task CheckInventoryProviderNameAsync(Product product)
        {
            if (product.InventoryProviderName.IsNullOrEmpty())
            {
                return;
            }

            if (!await _productInventoryProviderResolver.ExistProviderAsync(product.InventoryProviderName!))
            {
                throw new NonexistentInventoryProviderException(product.InventoryProviderName);
            }
        }

        [UnitOfWork(true)]
        public virtual async Task<Product> UpdateAsync(Product product, IEnumerable<Guid> categoryIds = null)
        {
            await CheckProductGroupNameAsync(product);

            await CheckInventoryProviderNameAsync(product);

            await CheckProductUniqueNameAsync(product);

            await _productRepository.UpdateAsync(product, autoSave: true);

            await CheckProductDetailAsync(product);

            await UpdateProductCategoriesAsync(product.Id, categoryIds);

            return product;
        }

        [UnitOfWork(true)]
        public virtual async Task DeleteAsync(Product product)
        {
            await _productCategoryRepository.DeleteAsync(x => x.ProductId.Equals(product.Id));

            await _productRepository.DeleteAsync(product, true);
        }

        [UnitOfWork(true)]
        public virtual async Task DeleteAsync(Guid id)
        {
            await _productCategoryRepository.DeleteAsync(x => x.ProductId.Equals(id));

            await _productRepository.DeleteAsync(id, true);
        }

        [UnitOfWork]
        public virtual async Task<Product> CreateSkuAsync(Product product, ProductSku productSku)
        {
            // productSku.SetSerializedAttributeOptionIds(await _attributeOptionIdsSerializer.FormatAsync(productSku.SerializedAttributeOptionIds));

            await CheckSkuAttributeOptionsAsync(product, productSku);

            await CheckProductSkuNameUniqueAsync(product, productSku);

            productSku.TrimName();

            product.ProductSkus.AddIfNotContains(productSku);

            await CheckProductDetailAsync(product);

            return await _productRepository.UpdateAsync(product, true);
        }

        protected virtual Task CheckProductSkuNameUniqueAsync(Product product, ProductSku productSku)
        {
            if (productSku.Name.IsNullOrEmpty())
            {
                return Task.CompletedTask;
            }

            if (product.ProductSkus.Where(sku => sku.Id != productSku.Id)
                    .FirstOrDefault(sku => sku.Name == productSku.Name) != null)
            {
                throw new ProductSkuCodeDuplicatedException(product.Id, productSku.Name);
            }

            return Task.CompletedTask;
        }

        protected virtual async Task CheckSkuAttributeOptionsAsync(Product product, ProductSku productSku)
        {
            var attributeOptionIds =
                (await _attributeOptionIdsSerializer.DeserializeAsync(productSku.SerializedAttributeOptionIds))
                .ToList();

            if (!product.ProductAttributes.TrueForAll(attribute =>
                    attribute.ProductAttributeOptions.Select(option => option.Id).Intersect(attributeOptionIds)
                        .Count() == 1))
            {
                throw new ProductSkuIncorrectAttributeOptionsException(product.Id,
                    productSku.SerializedAttributeOptionIds);
            }

            if (product.ProductSkus.Where(sku => sku.Id != productSku.Id).FirstOrDefault(sku =>
                    sku.SerializedAttributeOptionIds.Equals(productSku.SerializedAttributeOptionIds)) != null)
            {
                throw new ProductSkuDuplicatedException(product.Id, productSku.SerializedAttributeOptionIds);
            }
        }

        [UnitOfWork]
        public virtual async Task<Product> UpdateSkuAsync(Product product, ProductSku productSku)
        {
            await CheckProductSkuNameUniqueAsync(product, productSku);

            await CheckProductDetailAsync(product);

            return await _productRepository.UpdateAsync(product, true);
        }

        [UnitOfWork]
        public virtual async Task<Product> DeleteSkuAsync(Product product, ProductSku productSku)
        {
            product.ProductSkus.Remove(productSku);

            return await _productRepository.UpdateAsync(product, true);
        }

        [UnitOfWork]
        protected virtual async Task CheckProductUniqueNameAsync(Product product)
        {
            await _productRepository.CheckUniqueNameAsync(product);
        }

        protected virtual async Task CheckProductDetailAsync(Product product)
        {
            if (product.ProductDetailId.HasValue)
            {
                await CheckProductDetailExistAsync(product.ProductDetailId.Value, product.StoreId);
            }

            foreach (var sku in product.ProductSkus.Where(x => x.ProductDetailId.HasValue))
            {
                await CheckProductDetailExistAsync(sku.ProductDetailId!.Value, product.StoreId);
            }
        }

        [UnitOfWork]
        protected virtual async Task CheckProductDetailExistAsync(Guid productDetailId, Guid storeId)
        {
            var productDetail = await _productDetailRepository.GetAsync(productDetailId);

            if (productDetail.StoreId.HasValue && productDetail.StoreId.Value != storeId)
            {
                throw new EntityNotFoundException(typeof(ProductDetail), productDetailId);
            }
        }

        [UnitOfWork(true)]
        protected virtual async Task UpdateProductCategoriesAsync(Guid productId, IEnumerable<Guid> categoryIds)
        {
            await _productCategoryRepository.DeleteAsync(x => x.ProductId.Equals(productId));

            if (categoryIds == null)
            {
                return;
            }

            foreach (var categoryId in categoryIds)
            {
                await _productCategoryRepository.InsertAsync(
                    new ProductCategory(GuidGenerator.Create(), CurrentTenant.Id, categoryId, productId), true);
            }
        }

        public virtual async Task<bool> IsInventorySufficientAsync(Product product, ProductSku productSku, int quantity)
        {
            var model = new InventoryQueryModel(product.TenantId, product.StoreId, product.Id, productSku.Id);

            var inventoryData =
                await (await _productInventoryProviderResolver.GetAsync(product)).GetInventoryDataAsync(model);

            return product.InventoryStrategy == InventoryStrategy.NoNeed || inventoryData.Inventory - quantity >= 0;
        }

        public virtual async Task<InventoryDataModel> GetInventoryDataAsync(Product product, ProductSku productSku)
        {
            var model = new InventoryQueryModel(product.TenantId, product.StoreId, product.Id, productSku.Id);

            return await (await _productInventoryProviderResolver.GetAsync(product)).GetInventoryDataAsync(model);
        }

        public virtual async Task<bool> TryIncreaseInventoryAsync(Product product, ProductSku productSku, int quantity,
            bool reduceSold)
        {
            var model = new InventoryQueryModel(product.TenantId, product.StoreId, product.Id, productSku.Id);

            var isFlashSale = product.InventoryStrategy is InventoryStrategy.FlashSales;

            return await (await _productInventoryProviderResolver.GetAsync(product))
                .TryIncreaseInventoryAsync(model, quantity, reduceSold, isFlashSale);
        }

        public virtual async Task<bool> TryReduceInventoryAsync(Product product, ProductSku productSku, int quantity,
            bool increaseSold)
        {
            var model = new InventoryQueryModel(product.TenantId, product.StoreId, product.Id, productSku.Id);

            var isFlashSale = product.InventoryStrategy is InventoryStrategy.FlashSales;

            return await (await _productInventoryProviderResolver.GetAsync(product))
                .TryReduceInventoryAsync(model, quantity, increaseSold, isFlashSale);
        }

        public virtual async Task<PriceDataModel> GetRealPriceAsync(Product product, ProductSku productSku)
        {
            var price = await _productPriceProvider.GetPriceAsync(product, productSku);

            var discountedPrice = price;

            // Todo: provider execution ordering.
            foreach (var provider in LazyServiceProvider.LazyGetService<IEnumerable<IProductDiscountProvider>>())
            {
                discountedPrice = await provider.GetDiscountedPriceAsync(product, productSku, discountedPrice);
            }

            return new PriceDataModel
            {
                Price = price,
                DiscountedPrice = discountedPrice
            };
        }
    }
}