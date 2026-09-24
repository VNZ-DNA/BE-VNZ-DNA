namespace VNZ.Service.DepartmentService;

public interface IService
{
    Task<List<Response.DepartmentResponse>> GetDepartmentsAsync();
}
