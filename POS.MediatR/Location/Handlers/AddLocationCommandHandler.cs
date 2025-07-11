using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration.UserSecrets;
using POS.Data.Dto;
using POS.Helper;
using POS.MediatR.Location.Commands;
using POS.Repository;
using Microsoft.EntityFrameworkCore;
using POS.Common.UnitOfWork;
using POS.Domain;
using AutoMapper;
using Microsoft.Extensions.Logging;
using POS.MediatR.Language.Commands;

namespace POS.MediatR.Location.Handlers
{
    public class AddLocationCommandHandler (ILocationRepository _locationRepository, IUnitOfWork<POSDbContext> _uow, ILogger<AddLocationCommand> _logger, IMapper _mapper) : IRequestHandler<AddLocationCommand, ServiceResponse<LocationDto>>
    {
        public async Task<ServiceResponse<LocationDto>> Handle(AddLocationCommand request, CancellationToken cancellationToken)
        {
            var existingEntity = await _locationRepository.FindBy(c => c.Name == request.Name).FirstOrDefaultAsync();
            if (existingEntity != null)
            {
                _logger.LogError("Location Already Exist");
                return ServiceResponse<LocationDto>.Return409("Location Already Exist.");
            }
            var entity = _mapper.Map<POS.Data.Entities.Location>(request);
            _locationRepository.Add(entity);
            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Save Page have Error");
                return ServiceResponse<LocationDto>.Return500();
            }
            var entityToReturn = _mapper.Map<LocationDto>(entity);
            return ServiceResponse<LocationDto>.ReturnResultWith200(entityToReturn);
        }
    }
}
