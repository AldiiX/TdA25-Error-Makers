using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace TdA25_Error_Makers.Services;





public class ViewRenderService : IViewRenderService {
    private readonly IRazorViewEngine _viewEngine;
    private readonly ITempDataProvider _tempDataProvider;
    private readonly IServiceProvider _serviceProvider;

    public ViewRenderService(
        IRazorViewEngine viewEngine,
        ITempDataProvider tempDataProvider,
        IServiceProvider serviceProvider) {
        _viewEngine = viewEngine;
        _tempDataProvider = tempDataProvider;
        _serviceProvider = serviceProvider;
    }

    public async Task<string> RenderViewToStringAsync(string viewName, object? model, dynamic? viewBag = null) {
        using var scope = _serviceProvider.CreateScope();
        var scopedServices = scope.ServiceProvider;

        var viewEngine = scopedServices.GetRequiredService<IRazorViewEngine>();
        var tempDataProvider = scopedServices.GetRequiredService<ITempDataProvider>();
        var httpContext = new DefaultHttpContext { RequestServices = scopedServices };

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var viewResult = viewEngine.FindView(actionContext, viewName, false);

        if (viewResult.View == null) {
            throw new ArgumentNullException($"View '{viewName}' not found.");
        }

        await using var output = new StringWriter();
        var viewDictionary = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()) {
            Model = model,
        };

        if (viewBag != null) foreach (var prop in viewBag.GetType().GetProperties()) {
            viewDictionary.Add(prop.Name, prop.GetValue(viewBag));
        }

        var tempData = new TempDataDictionary(httpContext, tempDataProvider);

        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewDictionary,
            tempData,
            output,
            new HtmlHelperOptions()
        );

        await viewResult.View.RenderAsync(viewContext);
        return output.ToString();
    }

}