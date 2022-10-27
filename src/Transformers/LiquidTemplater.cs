﻿using DeaneBarker.Optimizely.Endpoints.Controllers;
using Fluid;
using Optimizely.CMS.Labs.LiquidTemplating.Filters;
using Optimizely.CMS.Labs.LiquidTemplating.Values;
using Optimizely.CMS.Labs.LiquidTemplating.ViewEngine;

namespace DeaneBarker.Optimizely.Endpoints.Transformers
{
    public class LiquidTemplater : ITransformer
    {
        public string Transform(string source, object model)
        {
            TemplateOptions.Default.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            var parser = new CmsFluidViewParser(new FluidParserOptions() { AllowFunctions = true });

            // It should be guaranteed of parsing because it passed validation to get to this point.
            var template = parser.Parse(source);

            // See notes below. Context models are weird ojn MVC installs
            var context = new TemplateContext(new SebIsWrong() { Model = model });
            context.Options.Filters.WithUrlFilters();
            context.SetValue("ContentLoader", new ContentLoaderValue());

            return template.Render(context);
        }
    }
}
