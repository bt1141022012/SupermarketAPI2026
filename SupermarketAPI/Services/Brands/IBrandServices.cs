using SupermarketAPI.DTOs;

namespace SupermarketAPI.Services.Brands
{
    public interface IBrandServices
    {
        Task<int> PostBrand(BrandRequest brand);
        Task<List<BrandResponse>> GetBrands();
        Task<PaginationDTO<BrandResponse>> GetBrandsPaginated(int pageNumber, int pageSize);
        Task<BrandResponse?> GetBrand(int brandId);
        Task<int> PutBrand(int brandId, BrandRequest brand);
        Task<int> DeleteBrand(int brandId);
    }
}