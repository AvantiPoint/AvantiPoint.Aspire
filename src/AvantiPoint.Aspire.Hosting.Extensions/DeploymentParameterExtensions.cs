using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace AvantiPoint.Aspire.Hosting.Extensions;

/// <summary>
/// Helpers for parameters that only matter at deployment time.
/// </summary>
public static class DeploymentParameterExtensions
{
    extension(IDistributedApplicationBuilder builder)
    {
        /// <summary>
        /// Adds a parameter that is required when publishing/deploying, but optional during local
        /// development. In publish mode this behaves like <see cref="ParameterResourceBuilderExtensions.AddParameter(IDistributedApplicationBuilder, string, bool)"/>
        /// (the value must be supplied); during <c>aspire run</c> it resolves to an empty value so the
        /// inner loop isn't blocked by configuration that only matters for a deploy.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="secret">Whether the parameter should be treated as a secret.</param>
        [AspireExport("addDeploymentParameter")]
        public IResourceBuilder<ParameterResource> AddDeploymentParameter(string name, bool secret = false)
        {
            if (builder.ExecutionContext.IsPublishMode)
            {
                return builder.AddParameter(name, secret);
            }

            return builder.AddResource(new ParameterResource(name, _ => string.Empty, secret));
        }

        /// <summary>
        /// Adds a parameter that is required when publishing/deploying, but optional during local
        /// development, falling back to <paramref name="value"/> when one isn't supplied. In publish mode
        /// this behaves like <see cref="ParameterResourceBuilderExtensions.AddParameter(IDistributedApplicationBuilder, string, ParameterDefault, bool, bool)"/>;
        /// during <c>aspire run</c> it resolves to the default value.
        /// </summary>
        /// <param name="name">The parameter name.</param>
        /// <param name="value">The default value used when one isn't supplied.</param>
        /// <param name="secret">Whether the parameter should be treated as a secret.</param>
        public IResourceBuilder<ParameterResource> AddDeploymentParameter(string name, ParameterDefault value, bool secret = false)
        {
            if (builder.ExecutionContext.IsPublishMode)
            {
                return builder.AddParameter(name, value, secret);
            }

            return builder.AddResource(new ParameterResource(name, _ => value.GetDefaultValue(), secret));
        }
    }
}
