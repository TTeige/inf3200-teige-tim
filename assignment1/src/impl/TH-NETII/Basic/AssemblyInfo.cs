using System;
using System.Reflection;

namespace THNETII
{
	public partial class AssemblyInfo
	{
		private System.Reflection.Assembly assem_obj;

		private System.Reflection.AssemblyCopyrightAttribute assem_copyright;
		private bool load_copyright = false;

		public virtual System.Reflection.AssemblyCopyrightAttribute CopyrightAttribute
		{ get { return this.GetPropertyValue(ref this.load_copyright, ref this.assem_copyright, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyCopyrightAttribute>()); } }

		public virtual System.String CopyrightString
		{ 
			get
			{
				var assem_attr = this.CopyrightAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Copyright))
					return string.Empty;
				else
					return assem_attr.Copyright;
			}
		}

		private System.Reflection.AssemblyTrademarkAttribute assem_trademark;
		private bool load_trademark = false;

		public virtual System.Reflection.AssemblyTrademarkAttribute TrademarkAttribute
		{ get { return this.GetPropertyValue(ref this.load_trademark, ref this.assem_trademark, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyTrademarkAttribute>()); } }

		public virtual System.String TrademarkString
		{ 
			get
			{
				var assem_attr = this.TrademarkAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Trademark))
					return string.Empty;
				else
					return assem_attr.Trademark;
			}
		}

		private System.Reflection.AssemblyProductAttribute assem_product;
		private bool load_product = false;

		public virtual System.Reflection.AssemblyProductAttribute ProductAttribute
		{ get { return this.GetPropertyValue(ref this.load_product, ref this.assem_product, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyProductAttribute>()); } }

		public virtual System.String ProductString
		{ 
			get
			{
				var assem_attr = this.ProductAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Product))
					return string.Empty;
				else
					return assem_attr.Product;
			}
		}

		private System.Reflection.AssemblyCompanyAttribute assem_company;
		private bool load_company = false;

		public virtual System.Reflection.AssemblyCompanyAttribute CompanyAttribute
		{ get { return this.GetPropertyValue(ref this.load_company, ref this.assem_company, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyCompanyAttribute>()); } }

		public virtual System.String CompanyString
		{ 
			get
			{
				var assem_attr = this.CompanyAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Company))
					return string.Empty;
				else
					return assem_attr.Company;
			}
		}

		private System.Reflection.AssemblyDescriptionAttribute assem_description;
		private bool load_description = false;

		public virtual System.Reflection.AssemblyDescriptionAttribute DescriptionAttribute
		{ get { return this.GetPropertyValue(ref this.load_description, ref this.assem_description, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyDescriptionAttribute>()); } }

		public virtual System.String DescriptionString
		{ 
			get
			{
				var assem_attr = this.DescriptionAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Description))
					return string.Empty;
				else
					return assem_attr.Description;
			}
		}

		private System.Reflection.AssemblyTitleAttribute assem_title;
		private bool load_title = false;

		public virtual System.Reflection.AssemblyTitleAttribute TitleAttribute
		{ get { return this.GetPropertyValue(ref this.load_title, ref this.assem_title, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyTitleAttribute>()); } }

		public virtual System.String TitleString
		{ 
			get
			{
				var assem_attr = this.TitleAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Title))
					return string.Empty;
				else
					return assem_attr.Title;
			}
		}

		private System.Reflection.AssemblyConfigurationAttribute assem_configuration;
		private bool load_configuration = false;

		public virtual System.Reflection.AssemblyConfigurationAttribute ConfigurationAttribute
		{ get { return this.GetPropertyValue(ref this.load_configuration, ref this.assem_configuration, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyConfigurationAttribute>()); } }

		public virtual System.String ConfigurationString
		{ 
			get
			{
				var assem_attr = this.ConfigurationAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Configuration))
					return string.Empty;
				else
					return assem_attr.Configuration;
			}
		}

		private System.Reflection.AssemblyDefaultAliasAttribute assem_defaultalias;
		private bool load_defaultalias = false;

		public virtual System.Reflection.AssemblyDefaultAliasAttribute DefaultAliasAttribute
		{ get { return this.GetPropertyValue(ref this.load_defaultalias, ref this.assem_defaultalias, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyDefaultAliasAttribute>()); } }

		public virtual System.String DefaultAliasString
		{ 
			get
			{
				var assem_attr = this.DefaultAliasAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.DefaultAlias))
					return string.Empty;
				else
					return assem_attr.DefaultAlias;
			}
		}

		private System.Reflection.AssemblyInformationalVersionAttribute assem_informationalversion;
		private bool load_informationalversion = false;

		public virtual System.Reflection.AssemblyInformationalVersionAttribute InformationalVersionAttribute
		{ get { return this.GetPropertyValue(ref this.load_informationalversion, ref this.assem_informationalversion, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()); } }

		public virtual System.String InformationalVersionString
		{ 
			get
			{
				var assem_attr = this.InformationalVersionAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.InformationalVersion))
					return string.Empty;
				else
					return assem_attr.InformationalVersion;
			}
		}

		private System.Reflection.AssemblyFileVersionAttribute assem_fileversion;
		private bool load_fileversion = false;

		public virtual System.Reflection.AssemblyFileVersionAttribute FileVersionAttribute
		{ get { return this.GetPropertyValue(ref this.load_fileversion, ref this.assem_fileversion, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyFileVersionAttribute>()); } }

		public virtual System.String FileVersionVersion
		{ 
			get
			{
				var assem_attr = this.FileVersionAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Version))
					return string.Empty;
				else
					return assem_attr.Version;
			}
		}

		private System.Reflection.AssemblyCultureAttribute assem_culture;
		private bool load_culture = false;

		public virtual System.Reflection.AssemblyCultureAttribute CultureAttribute
		{ get { return this.GetPropertyValue(ref this.load_culture, ref this.assem_culture, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyCultureAttribute>()); } }

		public virtual System.String CultureString
		{ 
			get
			{
				var assem_attr = this.CultureAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Culture))
					return string.Empty;
				else
					return assem_attr.Culture;
			}
		}

		private System.Reflection.AssemblyVersionAttribute assem_version;
		private bool load_version = false;

		public virtual System.Reflection.AssemblyVersionAttribute VersionAttribute
		{ get { return this.GetPropertyValue(ref this.load_version, ref this.assem_version, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyVersionAttribute>()); } }

		public virtual System.String VersionString
		{ 
			get
			{
				var assem_attr = this.VersionAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Version))
					return string.Empty;
				else
					return assem_attr.Version;
			}
		}

		private System.Reflection.AssemblyKeyFileAttribute assem_keyfile;
		private bool load_keyfile = false;

		public virtual System.Reflection.AssemblyKeyFileAttribute KeyFileAttribute
		{ get { return this.GetPropertyValue(ref this.load_keyfile, ref this.assem_keyfile, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyKeyFileAttribute>()); } }

		public virtual System.String KeyFileString
		{ 
			get
			{
				var assem_attr = this.KeyFileAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.KeyFile))
					return string.Empty;
				else
					return assem_attr.KeyFile;
			}
		}

		private System.Reflection.AssemblyDelaySignAttribute assem_delaysign;
		private bool load_delaysign = false;

		public virtual System.Reflection.AssemblyDelaySignAttribute DelaySignAttribute
		{ get { return this.GetPropertyValue(ref this.load_delaysign, ref this.assem_delaysign, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyDelaySignAttribute>()); } }

		public virtual System.Boolean DelaySignValue
		{ 
			get
			{
				var assem_attr = this.DelaySignAttribute;
				if (assem_attr == null)
					return default(System.Boolean);
				else
					return assem_attr.DelaySign;
			}
		}

		private System.Reflection.AssemblyAlgorithmIdAttribute assem_algorithmid;
		private bool load_algorithmid = false;

		public virtual System.Reflection.AssemblyAlgorithmIdAttribute AlgorithmIdAttribute
		{ get { return this.GetPropertyValue(ref this.load_algorithmid, ref this.assem_algorithmid, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyAlgorithmIdAttribute>()); } }

		public virtual System.UInt32 AlgorithmIdValue
		{ 
			get
			{
				var assem_attr = this.AlgorithmIdAttribute;
				if (assem_attr == null)
					return default(System.UInt32);
				else
					return assem_attr.AlgorithmId;
			}
		}

		private System.Reflection.AssemblyFlagsAttribute assem_flags;
		private bool load_flags = false;

		public virtual System.Reflection.AssemblyFlagsAttribute FlagsAttribute
		{ get { return this.GetPropertyValue(ref this.load_flags, ref this.assem_flags, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyFlagsAttribute>()); } }

		public virtual System.Int32 FlagsAssemblyFlags
		{ 
			get
			{
				var assem_attr = this.FlagsAttribute;
				if (assem_attr == null)
					return default(System.Int32);
				else
					return assem_attr.AssemblyFlags;
			}
		}

		private System.Reflection.AssemblySignatureKeyAttribute assem_signaturekey;
		private bool load_signaturekey = false;

		public virtual System.Reflection.AssemblySignatureKeyAttribute SignatureKeyAttribute
		{ get { return this.GetPropertyValue(ref this.load_signaturekey, ref this.assem_signaturekey, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblySignatureKeyAttribute>()); } }

		public virtual System.String SignatureKeyPublicKey
		{ 
			get
			{
				var assem_attr = this.SignatureKeyAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.PublicKey))
					return string.Empty;
				else
					return assem_attr.PublicKey;
			}
		}

		public virtual System.String SignatureKeyCountersignature
		{ 
			get
			{
				var assem_attr = this.SignatureKeyAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.Countersignature))
					return string.Empty;
				else
					return assem_attr.Countersignature;
			}
		}

		private System.Reflection.AssemblyKeyNameAttribute assem_keyname;
		private bool load_keyname = false;

		public virtual System.Reflection.AssemblyKeyNameAttribute KeyNameAttribute
		{ get { return this.GetPropertyValue(ref this.load_keyname, ref this.assem_keyname, () => this.GetCustomAssemblyAttribute<System.Reflection.AssemblyKeyNameAttribute>()); } }

		public virtual System.String KeyNameString
		{ 
			get
			{
				var assem_attr = this.KeyNameAttribute;
				if (assem_attr == null || string.IsNullOrWhiteSpace(assem_attr.KeyName))
					return string.Empty;
				else
					return assem_attr.KeyName;
			}
		}

		private System.Reflection.AssemblyMetadataAttribute[] assem_metadata;
		private bool load_metadata = false;

		public virtual System.Reflection.AssemblyMetadataAttribute[] MetadataAttributes
		{ get { return this.GetPropertyValue(ref this.load_metadata, ref this.assem_metadata, () => this.GetAllCustomAssemblyAttributes<System.Reflection.AssemblyMetadataAttribute>()); } }

		protected virtual T GetCustomAssemblyAttribute<T>(bool inherit = true) where T : Attribute
		{
			var assem_attr_match = this.assem_obj.GetCustomAttributes(typeof(T), inherit);
			if (assem_attr_match != null && assem_attr_match.Length > 0)
				return (T)(assem_attr_match[0]);
			else
				return default(T);
		}

		protected virtual T[] GetAllCustomAssemblyAttributes<T>(bool inherit = true) where T : Attribute
		{
			var assem_attr_match = this.assem_obj.GetCustomAttributes(typeof(T), inherit);
			if (assem_attr_match != null && assem_attr_match.Length > 0)
				return (T[])(assem_attr_match);
			else
				return new T[0];
		}

		protected virtual T GetPropertyValue<T>(ref bool load_field, ref T assem_field, Func<T> assem_delegate)
		{
			if (!load_field)
			{
				assem_field = assem_delegate != null ? assem_delegate() : default(T);
				load_field = true;
			}
			return assem_field;
		}

		public AssemblyInfo() : this(System.Reflection.Assembly.GetEntryAssembly()) { }

		public AssemblyInfo(Type t) : this(System.Reflection.Assembly.GetAssembly(t)) { }

		public AssemblyInfo(System.Reflection.Assembly assembly)
		{
			if (assembly == null)
				throw new ArgumentNullException("assembly");

			this.assem_obj = assembly;
		}
	}
}
