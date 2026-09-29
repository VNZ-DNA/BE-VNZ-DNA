using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
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
        var data = await _productService.GetPublicProductListAsync();

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách Product công khai thành công.",
            HttpContext.TraceIdentifier));
    }
}
