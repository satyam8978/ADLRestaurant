using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

public class SessionAuthFilter : IPageFilter
{
    // This method is invoked when the page handler is selected (this can be left empty if not needed)
    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
        // You can add logic here if needed, but it's not required for your session check
    }

    // This method is invoked before the handler execution
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var routeData = context.RouteData.Values["page"]?.ToString();

        // If the current page is login or register, skip session check
        if (routeData == "/Login" || routeData == "/Register")
        {
            return;
        }

        // Check if the user is logged in by checking the session
        var userId = context.HttpContext.Session.GetString("UserId");

        if (string.IsNullOrEmpty(userId))
        {
            // Redirect to login page if session is not found
            context.Result = new RedirectToPageResult("/Login");
        }
    }

    // This method is invoked after the handler has executed
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
        // Optional: Code after page handler execution
    }
}
