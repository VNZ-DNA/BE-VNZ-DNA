using Microsoft.EntityFrameworkCore;
using VNZ.Repository;

namespace VNZ.Service.DepartmentService;

public class Service : IService
{
    private readonly AppDbContext _dbContext;

    public Service(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Response.DepartmentResponse>> GetDepartmentsAsync()
    {
        return await _dbContext.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ThenBy(department => department.Id)
            .Select(department => new Response.DepartmentResponse
            {
                Id = department.Id,
                Name = department.Name
            })
            .ToListAsync();
    }
}
