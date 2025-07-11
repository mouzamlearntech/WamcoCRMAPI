﻿using AutoMapper;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Data.Dto;
using POS.Domain;
using POS.Helper;
using POS.MediatR.CommandAndQuery;
using POS.Repository;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Azure;
using Microsoft.AspNetCore.Components.Forms;
using POS.MediatR.Language.Commands;

namespace POS.MediatR.Handlers
{
    public class UpdateCompanyProfileCommandHandler
        : IRequestHandler<UpdateCompanyProfileCommand, ServiceResponse<CompanyProfileDto>>
    {
        private readonly ICompanyProfileRepository _companyProfileRepository;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork<POSDbContext> _uow;
        private readonly ILogger<UpdateCompanyProfileCommandHandler> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly PathHelper _pathHelper;
        private readonly ILanguageRepository _languageRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IMediator _mediator; 

        public UpdateCompanyProfileCommandHandler(
            ICompanyProfileRepository companyProfileRepository,
            IMapper mapper,
            IUnitOfWork<POSDbContext> uow,
            ILogger<UpdateCompanyProfileCommandHandler> logger,
            IWebHostEnvironment webHostEnvironment,
            PathHelper pathHelper,
            ILanguageRepository languageRepository,
            ILocationRepository locationRepository,
            IMediator mediator)
        {
            _companyProfileRepository = companyProfileRepository;
            _mapper = mapper;
            _uow = uow;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _pathHelper = pathHelper;
            _languageRepository = languageRepository;
            _locationRepository = locationRepository;
            _mediator = mediator;
        }
        public async Task<ServiceResponse<CompanyProfileDto>> Handle(UpdateCompanyProfileCommand request, CancellationToken cancellationToken)
        {
            var logoUrl = string.Empty;

            if (!string.IsNullOrWhiteSpace(request.ImageData))
            {
                logoUrl = $"{Guid.NewGuid()}.{Path.GetExtension(request.LogoUrl)}";
            }

            CompanyProfile companyProfile;
            if (request.Id.HasValue)
            {
                companyProfile = await _companyProfileRepository.FindAsync(request.Id.Value);
                if (companyProfile != null)
                {
                    companyProfile.Title = request.Title;
                    companyProfile.Address = request.Address;
                    companyProfile.Phone = request.Phone;
                    companyProfile.Email = request.Email;
                    companyProfile.CurrencyCode = request.CurrencyCode;
                    companyProfile.TaxName = request.TaxName;
                    companyProfile.TaxNumber = request.TaxNumber;
                    if (!string.IsNullOrWhiteSpace(request.ImageData))
                    {
                        companyProfile.LogoUrl = logoUrl;
                    }
                    _companyProfileRepository.Update(companyProfile);
                }
                else
                {
                    companyProfile = new CompanyProfile
                    {
                        Address = request.Address,
                        Title = request.Title
                    };
                    if (!string.IsNullOrWhiteSpace(request.ImageData))
                    {
                        companyProfile.LogoUrl = logoUrl;
                    }
                    _companyProfileRepository.Add(companyProfile);
                }
            }
            else
            {
                companyProfile = _mapper.Map<CompanyProfile>(request);
                if (!string.IsNullOrWhiteSpace(request.ImageData))
                {
                    companyProfile.LogoUrl = logoUrl;
                }
                _companyProfileRepository.Add(companyProfile);
            }

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Error while Updating Company Profile.");
                return ServiceResponse<CompanyProfileDto>.Return500();
            }

            if (!string.IsNullOrWhiteSpace(request.ImageData))
            {
                string pathToSave = _webHostEnvironment.WebRootPath;
                pathToSave = Path.Combine(pathToSave, _pathHelper.CompanyLogo);
                if (!Directory.Exists(pathToSave))
                {
                    Directory.CreateDirectory(pathToSave);
                }
                var documentPath = Path.Combine(pathToSave, companyProfile.LogoUrl);
                string base64 = request.ImageData.Split(',').LastOrDefault();
                if (!string.IsNullOrWhiteSpace(base64))
                {
                    byte[] bytes = Convert.FromBase64String(base64);
                    try
                    {
                        await File.WriteAllBytesAsync($"{documentPath}", bytes);
                    }
                    catch
                    {
                        _logger.LogError("Error while saving files");
                    }
                }
            }
            var result = _mapper.Map<CompanyProfileDto>(companyProfile);
            if (!string.IsNullOrWhiteSpace(result.LogoUrl))
            {
                result.LogoUrl = Path.Combine(_pathHelper.CompanyLogo, result.LogoUrl);
            }

            result.Languages = await _mediator.Send(new GetAllLanguageCommand());
            var locations = await _locationRepository.All.ToListAsync();
            result.Locations = _mapper.Map<List<LocationDto>>(locations);
            return ServiceResponse<CompanyProfileDto>.ReturnResultWith200(result);
        }
    }
}
