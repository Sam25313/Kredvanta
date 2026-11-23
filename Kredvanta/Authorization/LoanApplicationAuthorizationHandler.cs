using Microsoft.AspNetCore.Authorization;
using Kredvanta.Data;
using Kredvanta.Models.Applications;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;


namespace Kredvanta.Authorization;

public static class  Operations
{
    public static readonly OperationAuthorizationRequirement Read = new() { Name = nameof(Read) };
    public static readonly OperationAuthorizationRequirement Modify = new() { Name = nameof(Modify) };
}


public class LoanApplicationAuthorizationHandler 
    : AuthorizationHandler<OperationAuthorizationRequirement, LoanApplication>
    {
     private readonly ApplicationDbContext _db;
    public LoanApplicationAuthorizationHandler(ApplicationDbContext db) => _db = db;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        LoanApplication application)
    {
        if (context.User.IsInRole(AppRoles.Administrator))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is null) return;

        // Staff who may read a case
        if (requirement == Operations.Read && AppRoles.CaseReaders.Any(r => context.User.IsInRole(r)))
        {
            context.Succeed(requirement);
            return;
        }

        // Borrowers can only read their own applications
        if (context.User.IsInRole(AppRoles.Borrower))
          { 
            var owns = await _db.Borrowers.AnyAsync(b => b.Id == application.BorrowerId && b.ApplicationUserId == userId);

        if (owns) context.Succeed(requirement);
         }
       }
    }



  
