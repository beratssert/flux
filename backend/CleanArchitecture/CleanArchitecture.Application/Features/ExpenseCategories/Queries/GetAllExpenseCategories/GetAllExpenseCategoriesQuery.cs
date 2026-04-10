using AutoMapper;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.ExpenseCategories.Queries.GetAllExpenseCategories
{
    public class GetAllExpenseCategoriesQuery : IRequest<List<GetAllExpenseCategoriesViewModel>>
    {
    }

    public class GetAllExpenseCategoriesQueryHandler : IRequestHandler<GetAllExpenseCategoriesQuery, List<GetAllExpenseCategoriesViewModel>>
    {
        private readonly IExpenseCategoryRepositoryAsync _expenseCategoryRepository;
        private readonly IMapper _mapper;

        public GetAllExpenseCategoriesQueryHandler(IExpenseCategoryRepositoryAsync expenseCategoryRepository, IMapper mapper)
        {
            _expenseCategoryRepository = expenseCategoryRepository;
            _mapper = mapper;
        }

        public async Task<List<GetAllExpenseCategoriesViewModel>> Handle(GetAllExpenseCategoriesQuery request, CancellationToken cancellationToken)
        {
            var entities = await _expenseCategoryRepository.GetActiveAsync();
            return _mapper.Map<List<GetAllExpenseCategoriesViewModel>>(entities);
        }
    }
}
