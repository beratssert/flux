using AutoMapper;
using CleanArchitecture.Core.DTOs.TimeEntries;
using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetAllTimeEntries;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamPeriodSummary;
using CleanArchitecture.Core.Features.TimeEntries.Queries.GetTeamProjectSummary;
using CleanArchitecture.Core.Features.Expenses.Queries.GetAllExpenses;
using CleanArchitecture.Core.Features.ExpenseCategories.Queries.GetAllExpenseCategories;
using CleanArchitecture.Core.Features.Categories.Queries.GetAllCategories;
using CleanArchitecture.Core.Features.Products.Commands.CreateProduct;
using CleanArchitecture.Core.Features.Products.Queries.GetAllProducts;
using CleanArchitecture.Core.Features.Projects;
namespace CleanArchitecture.Core.Mappings
{
    public class GeneralProfile : Profile
    {
        public GeneralProfile()
        {
            CreateMap<Product, GetAllProductsViewModel>().ReverseMap();
            CreateMap<CreateProductCommand, Product>();
            CreateMap<GetAllProductsQuery, GetAllProductsParameter>();
            CreateMap<GetAllCategoriesQuery, GetAllCategoriesParameter>();
            CreateMap<Category, GetAllCategoriesViewModel>().ReverseMap();
            CreateMap<GetAllTimeEntriesQuery, GetAllTimeEntriesParameter>();
            CreateMap<TimeEntry, GetAllTimeEntriesViewModel>();
            CreateMap<GetAllExpensesQuery, GetAllExpensesParameter>();
            CreateMap<Expense, GetAllExpensesViewModel>();
            CreateMap<ExpenseCategory, GetAllExpenseCategoriesViewModel>();
            CreateMap<TeamProjectSummaryDto, GetTeamProjectSummaryViewModel>();
            CreateMap<TeamPeriodSummaryDto, GetTeamPeriodSummaryViewModel>();
            CreateMap<Project, ProjectViewModel>();
        }
    }
}
