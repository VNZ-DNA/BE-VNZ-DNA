using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VNZ.Service.Models;
using ProductService = VNZ.Service.ProductService;

namespace VNZ.Api.Controllers;

[ApiController]
[Route("api/v1/admin/products")]
[Authorize(Roles = "Admin")]
public sealed class ProductsController : ControllerBase
{
    private readonly ProductService.IService _productService;

    public ProductsController(ProductService.IService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetProductList(
        [FromQuery] ProductService.Request.GetProductListRequest request)
    {
        var data = await _productService.GetProductListAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy danh sách Product thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetProductDetail(Guid id)
    {
        var data = await _productService.GetProductDetailAsync(id);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Lấy chi tiết sản phẩm thành công.",
            HttpContext.TraceIdentifier));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateProduct(
        Guid id,
        [FromBody] ProductService.Request.UpdateProductRequest request)
    {
        var data = await _productService.UpdateProductAsync(id, request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Cập nhật sản phẩm thành công.",
            HttpContext.TraceIdentifier));
    }



    [HttpPut("display-order")]
    public async Task<IActionResult> ReorderProducts(
        [FromBody] ProductService.Request.ReorderProductsRequest request)
    {
        var data = await _productService.ReorderProductsAsync(request);

        return Ok(ResponseBuilder.SuccessResponse(
            data,
            "Cập nhật thứ tự Product thành công.",
            HttpContext.TraceIdentifier));
    }
}
