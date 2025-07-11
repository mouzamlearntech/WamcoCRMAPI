﻿using AutoMapper;
using POS.Common.GenericRepository;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Data.Resources;
using POS.Domain;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace POS.Repository
{
    public class ExpenseRepository : GenericRepository<Expense, POSDbContext>,
            IExpenseRepository
    {
        private readonly IPropertyMappingService _propertyMappingService;
        private readonly IMapper _mapper;
        private readonly UserInfoToken _userInfoToken;
        public ExpenseRepository(
            IUnitOfWork<POSDbContext> uow,
            IPropertyMappingService propertyMappingService,
            IMapper mapper,
            UserInfoToken userInfoToken
            ) : base(uow)
        {
            _propertyMappingService = propertyMappingService;
            _mapper = mapper;
            _userInfoToken = userInfoToken;
        }

        public async Task<ExpenseList> GetExpenses(ExpenseResource expenseResource)
        {
            var locationIds = new List<Guid>();
            if (expenseResource.LocationId.HasValue)
            {
                locationIds = [expenseResource.LocationId.Value];
            }
            else
            {
                locationIds = _userInfoToken.LocationIds;
            }

            var collectionBeforePaging = All
                .Where(c => locationIds.Contains(c.LocationId))
                .Include(c => c.ExpenseBy)
                .Include(c => c.ExpenseCategory)
                .Include(C => C.Location)
                .Include(c => c.ExpenseTaxes)
                    .ThenInclude(c => c.Tax)
            .ApplySort(expenseResource.OrderBy,
            _propertyMappingService.GetPropertyMapping<ExpenseDto, Expense>());

            if (!string.IsNullOrEmpty(expenseResource.Reference))
            {
                // trim & ignore casing
                var referenceWhereClause = expenseResource.Reference
                    .Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => EF.Functions.Like(a.Reference, $"{referenceWhereClause}%"));
            }

            if (expenseResource.FromDate.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseDate >= new DateTime(expenseResource.FromDate.Value.Year, expenseResource.FromDate.Value.Month, expenseResource.FromDate.Value.Day, 0, 0, 1));
            }
            if (expenseResource.ToDate.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseDate <= new DateTime(expenseResource.ToDate.Value.Year, expenseResource.ToDate.Value.Month, expenseResource.ToDate.Value.Day, 23, 59, 59));
            }

            if (!string.IsNullOrEmpty(expenseResource.Description))
            {
                // trim & ignore casing
                var descriptionWhereClause = expenseResource.Description
                    .Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => EF.Functions.Like(a.Description, $"{descriptionWhereClause}%"));
            }

            if (expenseResource.ExpenseCategoryId.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseCategoryId == expenseResource.ExpenseCategoryId);
            }

            if (expenseResource.ExpenseById.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseById == expenseResource.ExpenseById);
            }

            return await new ExpenseList(_mapper).Create(collectionBeforePaging,
                expenseResource.Skip,
                expenseResource.PageSize);
        }


        public async Task<ExpenseList> GetExpensesReport(ExpenseResource expenseResource)
        {
            var collectionBeforePaging = AllIncluding(c => c.ExpenseBy, cs => cs.ExpenseCategory).ApplySort(expenseResource.OrderBy,
                _propertyMappingService.GetPropertyMapping<ExpenseDto, Expense>());

            if (!string.IsNullOrEmpty(expenseResource.Reference))
            {
                // trim & ignore casing
                var referenceWhereClause = expenseResource.Reference
                    .Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => EF.Functions.Like(a.Reference, $"{referenceWhereClause}%"));
            }

            if (expenseResource.FromDate.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseDate >= new DateTime(expenseResource.FromDate.Value.Year, expenseResource.FromDate.Value.Month, expenseResource.FromDate.Value.Day, 0, 0, 1));
            }
            if (expenseResource.ToDate.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseDate <= new DateTime(expenseResource.ToDate.Value.Year, expenseResource.ToDate.Value.Month, expenseResource.ToDate.Value.Day, 23, 59, 59));
            }

            if (!string.IsNullOrEmpty(expenseResource.Description))
            {
                // trim & ignore casing
                var descriptionWhereClause = expenseResource.Description
                    .Trim().ToLowerInvariant();
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => EF.Functions.Like(a.Description, $"{descriptionWhereClause}%"));
            }

            if (expenseResource.ExpenseCategoryId.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseCategoryId == expenseResource.ExpenseCategoryId);
            }

            if (expenseResource.ExpenseById.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.ExpenseById == expenseResource.ExpenseById);
            }

            if (expenseResource.LocationId.HasValue)
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => a.LocationId == expenseResource.LocationId);
            }
            else
            {
                collectionBeforePaging = collectionBeforePaging
                    .Where(a => _userInfoToken.LocationIds.Contains(a.LocationId));
            }

            return await new ExpenseList(_mapper).Create(collectionBeforePaging,
                0,
                0);
        }
    }
}