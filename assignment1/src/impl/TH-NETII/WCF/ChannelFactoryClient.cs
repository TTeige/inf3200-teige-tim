using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace THNETII.WCF
{
	public class ChannelFactoryClient<IService> : ICommunicationObject, IDisposable
	{
		private readonly ChannelFactory<IService> factory;

		protected ChannelFactory<IService> ChannelFactory
		{ get { return factory; } }

		public event EventHandler Closed
		{
			add { factory.Closed += value; }
			remove { factory.Closed -= value; }
		}

		public event EventHandler Closing
		{
			add { factory.Closing += value; }
			remove { factory.Closing -= value; }
		}

		public event EventHandler Faulted
		{
			add { factory.Faulted += value; }
			remove { factory.Faulted -= value; }
		}

		public event EventHandler Opened
		{
			add { factory.Opened += value; }
			remove { factory.Opened -= value; }
		}

		public event EventHandler Opening
		{
			add { factory.Opening += value; }
			remove { factory.Opening -= value; }
		}

		public ClientCredentials Credentials => factory.Credentials;

		public ServiceEndpoint Endpoint => factory.Endpoint;

		public CommunicationState State => factory.State;

		protected ChannelFactoryClient(ChannelFactory<IService> factory)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			this.factory = factory;
		}

		public ChannelFactoryClient() : this(new ChannelFactory<IService>("*")) { }
		public ChannelFactoryClient(string endpointConfigurationName) : this(new ChannelFactory<IService>(endpointConfigurationName)) { }
		public ChannelFactoryClient(string endpointConfigurationName, EndpointAddress remoteAddress) : this(new ChannelFactory<IService>(endpointConfigurationName, remoteAddress)) { }
		public ChannelFactoryClient(Binding binding, EndpointAddress remoteAddress) : this(new ChannelFactory<IService>(binding, remoteAddress)) { }

		public void Abort() => factory.Abort();

		public void Close() => factory.Close();

		public void Close(TimeSpan timeout) => factory.Close(timeout);

		public IAsyncResult BeginClose(AsyncCallback callback, object state) => factory.BeginClose(callback, state);

		public IAsyncResult BeginClose(TimeSpan timeout, AsyncCallback callback, object state) => factory.BeginClose(timeout, callback, state);

		public void EndClose(IAsyncResult result) => factory.EndClose(result);

		public void Open() => factory.Open();

		public void Open(TimeSpan timeout) => factory.Open(timeout);

		public IAsyncResult BeginOpen(AsyncCallback callback, object state) => factory.BeginOpen(callback, state);

		public IAsyncResult BeginOpen(TimeSpan timeout, AsyncCallback callback, object state) => factory.BeginOpen(timeout, callback, state);

		public void EndOpen(IAsyncResult result) => factory.EndOpen(result);

		public void Dispose()
		{ using (factory) { } }
	}
}