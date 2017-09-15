﻿using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VirtoCommerce.Storefront.Data.Stores;
using VirtoCommerce.Storefront.Extensions;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Stores;
using VirtoCommerce.Storefront.Model.Services;
using VirtoCommerce.Storefront.Model.LinkList.Services;
using VirtoCommerce.Storefront.Model.StaticContent;
using VirtoCommerce.Storefront.Model.Pricing.Services;

namespace VirtoCommerce.Storefront.Middleware
{
    public class WorkContextPopulateMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IConfiguration _configuration;
        private readonly IWorkContextAccessor _workContextAccessor;
        public WorkContextPopulateMiddleware(RequestDelegate next, IHostingEnvironment hostingEnvironment,
                                             IConfiguration configuration, IWorkContextAccessor workContextAccessor)
        {
            _next = next;
            _hostingEnvironment = hostingEnvironment;
            _workContextAccessor = workContextAccessor;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var serviceProvider = context.RequestServices;
            //Need to get store and language segment from old path in ExceptionHandlerPathFeature for correct exception page routing
            var exceptionFeature = context.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionFeature != null)
            {
                context.Request.Path = new PathString(exceptionFeature.Path).GetStoreAndLangSegment() + context.Request.Path;
            }
            else
            {
                var builder = WorkContextBuilder.FromHttpContext(context);
                builder.WithCountries(serviceProvider.GetRequiredService<ICountriesService>());
                await builder.WithStoresAsync(serviceProvider.GetRequiredService<IStoreService>(), _configuration.GetValue<string>("VirtoCommerce:DefaultStore"));
                await builder.WithCurrenciesAsync(serviceProvider.GetRequiredService<ICurrencyService>());
                await builder.WithCatalogsAsync(serviceProvider.GetRequiredService<ICatalogService>());
                //await builder.WithStaticContentAsync(serviceProvider.GetRequiredService<IMenuLinkListService>(), serviceProvider.GetRequiredService<IStaticContentService>());
                //await builder.WithPrices(serviceProvider.GetRequiredService<IPricingService>());
                _workContextAccessor.WorkContext = builder.Build();
            }

            await _next(context);
        }
    }

}
