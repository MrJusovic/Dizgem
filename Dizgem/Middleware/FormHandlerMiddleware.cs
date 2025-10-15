using Dizgem.Data;
using Dizgem.Services;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

namespace Dizgem.Middleware
{
    public class FormHandlerMiddleware
    {
        private readonly RequestDelegate _next;

        public FormHandlerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Sadece POST ve form içeriği olan istekleri kontrol et
            if (context.Request.Method == "POST" && context.Request.HasFormContentType)
            {
                var formId = context.Request.Form["data-dizgem-handler-id"].ToString();
                if (!string.IsNullOrWhiteSpace(formId))
                {
                    // Form işlenmesi gerektiği için, yeni bir scope oluşturarak servisleri al
                    using (var scope = context.RequestServices.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var formProcessingService = scope.ServiceProvider.GetRequiredService<IFormProcessingService>();
                        var tempDataFactory = scope.ServiceProvider.GetRequiredService<ITempDataDictionaryFactory>();

                        var handler = await dbContext.FormHandlers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(h => h.UniqueIdentifier == formId);

                        if (handler != null)
                        {
                            var success = await formProcessingService.ProcessFormAsync(handler, context.Request.Form);
                            var tempData = tempDataFactory.GetTempData(context);

                            if (success)
                            {
                                tempData["FormSuccessMessage"] = handler.SuccessMessage;
                            }
                            else
                            {
                                tempData["FormErrorMessage"] = "Formunuz işlenirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.";
                            }

                            // Kullanıcıyı formu gönderdiği sayfaya geri yönlendir.
                            context.Response.Redirect(context.Request.Headers["Referer"].ToString());
                            return; // Pipeline'ı burada durdur, controller'a gitme.
                        }
                    }
                }
            }

            // Eğer özel bir form değilse, pipeline'da devam et.
            await _next(context);
        }
    }
}
