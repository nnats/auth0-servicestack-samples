using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Mvc;
using System.Web.Mvc;

[assembly: WebActivator.PreApplicationStartMethod(typeof(ServiceStack_Auth0_Sample.App_Start.AppHost), "Start")]

//IMPORTANT: Add the line below to MvcApplication.RegisterRoutes(RouteCollection) in the Global.asax:
//routes.IgnoreRoute("api/{*pathInfo}"); 

/**
 * Entire ServiceStack Starter Template configured with a 'Hello' Web Service and a 'Todo' Rest Service.
 *
 * Auto-Generated Metadata API page at: /metadata
 * See other complete web service examples at: https://github.com/ServiceStack/ServiceStack.Examples
 */

namespace ServiceStack_Auth0_Sample.App_Start
{
	public class AppHost
		: AppHostBase
	{		
		public AppHost() //Tell ServiceStack the name and where to find your web services
			: base("StarterTemplate ASP.NET Host", typeof(HelloService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
			//Set JSON web services to return idiomatic JSON camelCase properties
			ServiceStack.Text.JsConfig.EmitCamelCaseNames = true;

			//Configure User Defined REST Paths
			Routes
				.Add<Hello>("/hello")
				.Add<Hello>("/hello/{Name*}")
				.Add<Todo>("/todos")
				.Add<Todo>("/todos/{Id}");

			//Change the default ServiceStack configuration
			//SetConfig(new EndpointHostConfig {
			//    DebugMode = true, //Show StackTraces in responses in development
			//});

			//Enable Authentication
			ConfigureAuth(container);

			//Register all your dependencies
			container.Register(new TodoRepository());
			
			//Register In-Memory Cache provider. 
			//For Distributed Cache Providers Use: PooledRedisClientManager, BasicRedisClientManager or see: https://github.com/ServiceStack/ServiceStack/wiki/Caching
			container.Register<ICacheClient>(new MemoryCacheClient());
			container.Register<ISessionFactory>(c => 
				new SessionFactory(c.Resolve<ICacheClient>()));

			//Set MVC to use the same Funq IOC as ServiceStack
			ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
		}

		// Uncomment to enable ServiceStack Authentication and CustomUserSession
		private void ConfigureAuth(Funq.Container container)
		{
			var appSettings = new AppSettings();

			//Default route: /auth/{provider}
			Plugins.Add(new AuthFeature(() => new Auth0UserSession(),
				new IAuthProvider[] {
                    new Auth0Provider(appSettings, appSettings.GetString("oauth.auth0.OAuthServerUrl"))
				})); 

			//Default route: /register
            //Plugins.Add(new RegistrationFeature()); 

            ////Requires ConnectionString configured in Web.Config
            //var connectionString = ConfigurationManager.ConnectionStrings["AppDb"].ConnectionString;
            //container.Register<IDbConnectionFactory>(c =>
            //    new OrmLiteConnectionFactory(connectionString, SqlServerOrmLiteDialectProvider.Instance));

            //container.Register<IUserAuthRepository>(c =>
            //    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

            //var authRepo = (OrmLiteAuthRepository)container.Resolve<IUserAuthRepository>();
            //authRepo.CreateMissingTables();
		}

		public static void Start()
		{
			new AppHost().Init();
		}
	}
}