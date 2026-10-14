using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SupermarketAPI.DTOs;
using SupermarketAPI.Models;

namespace SupermarketAPI.Services.Products
{
    public class ProductServices : IProductServices
    {
        private readonly SupermarketDbContext _db;
        private readonly IMapper _mapper;

        public ProductServices(SupermarketDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<int> DeleteProduct(int productId)
        {
            var product = await _db.Products.FindAsync(productId);
            if (product == null)
                return -1;

            _db.Products.Remove(product);

            return await _db.SaveChangesAsync();
        }

        public async Task<ProductResponse> GetProduct(int productId)
        {
            var product = await _db.Products.FindAsync(productId);
            var productResponse = _mapper.Map<Product, ProductResponse>(product);

            return productResponse;
        }

        public async Task<List<ProductResponse>> GetProducts()
        {
            var products = await _db.Products
                .Include(o=>o.User) 
                .ToListAsync();
            var productsList = _mapper.Map<List<Product>, List<ProductResponse>>(products);

            return productsList;
        }

        public async Task<int> PostProduct(ProductRequest product)
        {
            var productRequest = _mapper.Map<ProductRequest, Product>(product);
            await _db.Products.AddAsync(productRequest);
            await _db.SaveChangesAsync();
            return productRequest.ProductId;
        }

        public async Task<int> PutProduct(int productId, ProductRequest product)
        {
            var entity = await _db.Products.FindAsync(productId);
            if (entity == null)
                return -1;

            entity.ProductName = product.ProductName;
            entity.ProductPrice = product.ProductPrice;
            entity.ProductStatus = product.ProductStatus;
            entity.UserId = product.UserId;

            _db.Products.Update(entity);

            return await _db.SaveChangesAsync();
        }
    }
}
