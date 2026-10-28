using GestaoFinanceiraPessoal.Api.Contracts.Onboarding;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain.Enums;
using GestaoFinanceiraPessoal.Api.Extensions;
using GestaoFinanceiraPessoal.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/onboarding")]
public class OnboardingController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OnboardingStatusResponse>> GetStatus(
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var profile = await dbContext.FinancialProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        return profile is null
            ? Ok(new OnboardingStatusResponse(false, null, null, null, null))
            : Ok(new OnboardingStatusResponse(
                true,
                profile.InitialCapital,
                profile.EmploymentType,
                profile.IncomeSetupMode,
                profile.MonthlyIncomeReminderDay));
    }

    [HttpPost("complete")]
    public async Task<ActionResult<OnboardingStatusResponse>> Complete(
        CompleteOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();

        if (await dbContext.FinancialProfiles.AnyAsync(
            profile => profile.UserId == userId,
            cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "O onboarding desta conta já foi concluído.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var user = await dbContext.Users.FirstAsync(item => item.Id == userId, cancellationToken);
        OnboardingSetupService.AddInitialSetup(dbContext, user, request);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new OnboardingStatusResponse(
            true,
            request.InitialCapital,
            request.EmploymentType,
            request.IncomeSetupMode,
            request.IncomeSetupMode == IncomeSetupMode.MonthlyReminder ? request.IncomeReminderDay : null));
    }

    [HttpPut("income-reminder")]
    public async Task<ActionResult<OnboardingStatusResponse>> UpdateIncomeReminder(
        UpdateIncomeReminderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var profile = await dbContext.FinancialProfiles
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (profile is null || profile.EmploymentType != EmploymentType.Pj)
        {
            return BadRequest(new ProblemDetails { Title = "O lembrete mensal está disponível para contas PJ após a configuração inicial.", Status = 400 });
        }

        profile.IncomeSetupMode = IncomeSetupMode.MonthlyReminder;
        profile.MonthlyIncomeReminderDay = request.DayOfMonth;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new OnboardingStatusResponse(true, profile.InitialCapital, profile.EmploymentType,
            profile.IncomeSetupMode, profile.MonthlyIncomeReminderDay));
    }
}
