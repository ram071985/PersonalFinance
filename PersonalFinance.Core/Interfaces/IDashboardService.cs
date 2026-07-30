using PersonalFinance.Core.Dtos;
using PersonalFinance.Core.Dtos.Dashboard;

namespace PersonalFinance.Core.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetSummaryAsync();
}