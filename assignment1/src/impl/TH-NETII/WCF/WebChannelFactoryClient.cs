using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace THNETII.WCF
{
	public class WebChannelFactoryClient<IService> : ChannelFactoryClient<IService>
		where IService : class
	{
		protected WebChannelFactory<IService> WebChannelFactory
		{ get { return ChannelFactory as WebChannelFactory<IService>; } }

		protected WebChannelFactoryClient(WebChannelFactory<IService> factory) : base(factory) { }

		public WebChannelFactoryClient() : base(new WebChannelFactory<IService>("*")) { }
		public WebChannelFactoryClient(Uri remoteAddress) : base(new WebChannelFactory<IService>(remoteAddress)) { }
		public WebChannelFactoryClient(string endpointConfigurationName) : base(new WebChannelFactory<IService>(endpointConfigurationName)) { }
		public WebChannelFactoryClient(string endpointConfigurationName, Uri remoteAddress) : base(new WebChannelFactory<IService>(endpointConfigurationName, remoteAddress)) { }
		public WebChannelFactoryClient(Binding binding, Uri remoteAddress) : base(new WebChannelFactory<IService>(binding, remoteAddress)) { }
	}
}
