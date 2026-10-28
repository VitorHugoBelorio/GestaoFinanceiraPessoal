using GestaoFinanceiraPessoal.Api.Contracts.Categories;
using GestaoFinanceiraPessoal.Api.Data;
using GestaoFinanceiraPessoal.Api.Domain.Entities;
using GestaoFinanceiraPessoal.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoFinanceiraPessoal.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/categories")]
public class CategoriesController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> List(
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var categories = await dbContext.Categories
            .AsNoTracking()
            .Where(category => category.UserId == userId)
            .OrderBy(category => category.Type)
            .ThenBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Type))
            .ToListAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.GetRequiredUserId();
        var normalizedName = request.Name.Trim();

        if (normalizedName.Length < 2)
        {
            ModelState.AddModelError(nameof(request.Name), "A etiqueta precisa ter pelo menos 2 caracteres.");
            return ValidationProblem(ModelState);
        }

        if (await dbContext.Categories.AnyAsync(
            category => category.UserId == userId
                && category.Type == request.Type
                && category.Name == normalizedName,
            cancellationToken))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Esta etiqueta já existe para esse tipo de lançamento.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var category = new Category
        {
            UserId = userId,
            Name = normalizedName,
            Type = request.Type
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(List),
            new CategoryResponse(category.Id, category.Name, category.Type));
    }
}
