using System.Web;
using System.Web.Optimization;

namespace Pets
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/base/jquery-{version}.js"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/base/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/base/bootstrap.js",
                      "~/Scripts/base/respond.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css"));


            bundles.Add(new ScriptBundle("~/bundles/angular").Include(
                      "~/Scripts/angular-1.2.21/angular.js",
                      "~/Scripts/angular-1.2.21/angular-route.js",
                      "~/Scripts/angular-1.2.21/angular-local-storage.js",
                      "~/Scripts/angular-translate.js",
                      "~/Scripts/angular-directives/ng-upload.js",
                      "~/Scripts/angular-directives/messageService.js",
                      "~/Scripts/ui-bootstrap-tpls-0.11.2.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/app").Include(
                     "~/Scripts/app/app.js",
                     "~/Content/site.css"));
        }
    }
}
