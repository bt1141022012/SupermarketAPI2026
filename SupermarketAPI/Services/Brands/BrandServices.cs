using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SupermarketAPI.DTOs;
using SupermarketAPI.Models;

namespace SupermarketAPI.Services.Brands
{
    public class BrandServices : IBrandServices
    {
        private readonly SupermarketDbContext _db;
        private readonly IMapper _mapper;

        public BrandServices(SupermarketDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        public async Task<int> DeleteBrand(int brandId)
        {
            var brand = await _db.Brands.FindAsync(brandId);
            if (brand == null)
                return -1;

            _db.Brands.Remove(brand);
            return await _db.SaveChangesAsync();
        }

        public async Task<BrandResponse?> GetBrand(int brandId)
        {
            var brand = await _db.Brands.FindAsync(brandId);
            return brand == null ? null : _mapper.Map<Brand, BrandResponse>(brand);
        }

        public async Task<List<BrandResponse>> GetBrands()
        {
            var brands = await _db.Brands.ToListAsync();
            return _mapper.Map<List<Brand>, List<BrandResponse>>(brands);
        }

        public async Task<PaginationDTO<BrandResponse>> GetBrandsPaginated(int pageNumber, int pageSize)
        {
            if (pageNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "La página debe ser mayor que cero.");

            if (pageSize < 1)
                throw new ArgumentOutOfRangeException(nameof(pageSize), "El tamaño de página debe ser mayor que cero.");

            var totalCount = await _db.Brands.CountAsync();
            var brands = await _db.Brands
                .OrderBy(brand => brand.BrandId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginationDTO<BrandResponse>
            {
                Items = _mapper.Map<List<Brand>, List<BrandResponse>>(brands),
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<int> PostBrand(BrandRequest brand)
        {
            var entity = _mapper.Map<BrandRequest, Brand>(brand);
            await _db.Brands.AddAsync(entity);
            await _db.SaveChangesAsync();
           // return 0;
            return entity.BrandId;
        }

         public async Task<int> CrearMarca(BrandRequest brand)
        {
            var entity = _mapper.Map<BrandRequest, Brand>(brand);
            await _db.Brands.AddAsync(entity);
            await _db.SaveChangesAsync();
           // return 0;
            return entity.BrandId;
        }

        public async Task<int> PutBrand(int brandId, BrandRequest brand)
        {
            var entity = await _db.Brands.FindAsync(brandId);
            if (entity == null)
                return -1;

            entity.BrandName = brand.BrandName;
            entity.BrandStatus = brand.BrandStatus;
            _db.Brands.Update(entity);

            return await _db.SaveChangesAsync();
        }
    }
}