namespace VNZ.Service.DashboardService;

public interface IService
{
    Task<Response.DashboardResponse> GetDashboardAsync();
}
