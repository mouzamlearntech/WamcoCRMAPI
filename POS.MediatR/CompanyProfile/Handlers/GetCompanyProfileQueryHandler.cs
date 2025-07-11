using AutoMapper;
using POS.Data.Dto;
using POS.Helper;
using POS.MediatR.CommandAndQuery;
using POS.Repository;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using POS.Data;
using POS.MediatR.Language.Commands;

namespace POS.MediatR.Handlers
{
    public class GetCompanyProfileQueryHandler(
        ICompanyProfileRepository companyProfileRepository,
        IMapper mapper,
        PathHelper pathHelper,
        ILanguageRepository languageRepository,
        ILocationRepository locationRepository,
        IMediator mediator)
        : IRequestHandler<GetCompanyProfileQuery, CompanyProfileDto>
    {

        public async Task<CompanyProfileDto> Handle(GetCompanyProfileQuery request, CancellationToken cancellationToken)
        {
            var locations = await locationRepository.All.ToListAsync();
            var languages = await languageRepository.All.OrderBy(c => c.Order).ToListAsync();
            var companyProfile = await companyProfileRepository.All.FirstOrDefaultAsync();
            if (companyProfile == null)
            {
                companyProfile = new CompanyProfile
                {
                    Address = "3822 Crim Lane Dayton, OH 45407",
                    LogoUrl = "",
                    Title = "Point of Sale",
                    CurrencyCode = "USD",

                };
            }

            var response = mapper.Map<CompanyProfileDto>(companyProfile);
            response.Languages = await mediator.Send(new GetAllLanguageCommand());
            response.Locations = mapper.Map<List<LocationDto>>(locations);
            if (!string.IsNullOrWhiteSpace(response.LogoUrl))
            {
                response.LogoUrl = Path.Combine(pathHelper.CompanyLogo, response.LogoUrl);
            }
            return mapper.Map<CompanyProfileDto>(response);
        }
    }
}
