using Grand.Infrastructure;
using Grand.Infrastructure.Plugins;

namespace Widgets.ExtendedWebApi;

public class ExtendedWebApiPlugin : BasePlugin, IPlugin
{
    public override string ConfigurationUrl() => "../ExtendedWebApi/Configure";
}
