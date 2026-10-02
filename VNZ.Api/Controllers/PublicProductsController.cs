using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using VNZ.Service.Localization;
using ProductService = VNZ.Service.ProductService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/public/products")]
[AllowAnonymous]
public sealed class PublicProductsController : ControllerBase
{
    private readonly ProductService.IService _productService;

    public PublicProductsController(ProductService.IService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPublicProductList()
    {
        var lang = LocaleResolver.Resolve(Request.Query["lang"].ToArray());
        var data = await _productService.GetPublicProductListAsync(lang);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách Product công khai thành công.",
            HttpContext.TraceIdentifier));
    }
}
