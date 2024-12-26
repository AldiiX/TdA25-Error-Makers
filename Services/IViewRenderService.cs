namespace TdA25_Error_Makers.Services;

public interface IViewRenderService {
    Task<string> RenderViewToStringAsync(string viewName, object? model, dynamic? viewBag = null);
}