﻿using POS.Helper;
using Microsoft.AspNetCore.Mvc;

namespace POS.API.Controllers
{
    public class BaseController : ControllerBase
    {
        public IActionResult ReturnFormattedResponse<T>(ServiceResponse<T> response)
        {
            if (response.Success)
            {
                return Ok(response.Data);
            }
            return StatusCode(response.StatusCode, response.Errors);
        }

        public IActionResult GenerateResponse<T>(ServiceResponse<T> result)
        {
            if (result.Success)
            {
                return Ok(result.Data);
            }
            return StatusCode((int)result.StatusCode, result.Errors);
        }
    }
}