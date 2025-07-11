using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POS.Common.UnitOfWork;
using POS.Data;
using POS.Domain;
using POS.Helper;
using POS.MediatR.CommandAndQuery;
using POS.Repository;

namespace POS.MediatR
{
    public class RecoverPasswordCommandHandler(
        IUserRepository userRepository,
        IMediator mediator,
        IUnitOfWork<POSDbContext> unitOfWork)
        : IRequestHandler<RecoverPasswordCommand, ServiceResponse<bool>>
    {
        public async Task<ServiceResponse<bool>> Handle(RecoverPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await userRepository.All.FirstOrDefaultAsync(c => c.Email == request.UserName);
            if (user == null && user.ResetPasswordCode != request.Token)
            {
                return ServiceResponse<bool>.ReturnFailed(404, "User not found");
            }

            var command = new ResetPasswordCommand
            {
                UserName = user.UserName,
                Password = request.Password,
            };

            var result = await mediator.Send(command);

            if (!result.Success)
            {
                return ServiceResponse<bool>.ReturnFailed(500, "Internal Server Error");
            }

            user.ResetPasswordCode = null;
            userRepository.Update(user);

            if (await unitOfWork.SaveAsync() < 0)
            {
                return ServiceResponse<bool>.ReturnFailed(500, "Internal Server Error");
            }

            return ServiceResponse<bool>.ReturnSuccess();
        }
    }
}
