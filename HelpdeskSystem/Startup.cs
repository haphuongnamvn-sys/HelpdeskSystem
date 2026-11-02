using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(HelpdeskSystem.Startup))]
namespace HelpdeskSystem
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
