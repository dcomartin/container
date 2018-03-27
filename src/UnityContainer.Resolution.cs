using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Builder;
using Unity.Registration;
using Unity.Resolution;
using Unity.Storage;

namespace Unity
{
    /// <summary>
    /// A simple, extensible dependency injection container.
    /// </summary>
    public partial class UnityContainer
    {
        #region Getting objects

        /// <summary>
        /// GetOrDefault an instance of the requested type with the given name typeFrom the container.
        /// </summary>
        /// <param name="typeToBuild"><see cref="Type"/> of object to get typeFrom the container.</param>
        /// <param name="nameToBuild">Name of the object to retrieve.</param>
        /// <param name="resolverOverrides">Any overrides for the resolve call.</param>
        /// <returns>The retrieved object.</returns>
        public object Resolve(Type typeToBuild, string nameToBuild, params ResolverOverride[] resolverOverrides)
        {
            // Verify arguments
            var name = string.IsNullOrEmpty(nameToBuild) ? null : nameToBuild;
            var type = typeToBuild ?? throw new ArgumentNullException(nameof(typeToBuild));
            var registration = GetRegistration(type, name);
            var context = new BuilderContext(this, (InternalRegistration)registration, null, resolverOverrides);

            return BuilUpPipeline(context);
        }

        #endregion


        #region BuildUp existing object

        /// <summary>
        /// Run an existing object through the container and perform injection on it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method is useful when you don't control the construction of an
        /// instance (ASP.NET pages or objects created via XAML, for instance)
        /// but you still want properties and other injection performed.
        /// </para></remarks>
        /// <param name="typeToBuild"><see cref="Type"/> of object to perform injection on.</param>
        /// <param name="existing">Instance to build up.</param>
        /// <param name="nameToBuild">name to use when looking up the type mappings and other configurations.</param>
        /// <param name="resolverOverrides">Any overrides for the buildup.</param>
        /// <returns>The resulting object. By default, this will be <paramref name="existing"/>, but
        /// container extensions may add things like automatic proxy creation which would
        /// cause this to return a different object (but still type compatible with <paramref name="typeToBuild"/>).</returns>
        public object BuildUp(Type typeToBuild, object existing, string nameToBuild, params ResolverOverride[] resolverOverrides)
        {
            // Verify arguments
            var name = string.IsNullOrEmpty(nameToBuild) ? null : nameToBuild;
            var type = typeToBuild ?? throw new ArgumentNullException(nameof(typeToBuild));
            if (null != existing) InstanceIsAssignable(type, existing, nameof(existing));

            var context = new BuilderContext(this, (InternalRegistration)GetRegistration(type, name), existing, resolverOverrides);

            return BuilUpPipeline(context);
        }


        #endregion


        #region Resolving Collections

        internal static void ResolveArray<T>(IBuilderContext context)
        {
            var container = (UnityContainer)context.Container;
            var list = new List<T>();

            var registrations = (IList<InternalRegistration>)GetNamedRegistrations(container, typeof(T));
            foreach (var registration in registrations)
            {
                if (registration.Type.GetTypeInfo().IsGenericTypeDefinition)
                    list.Add((T)((BuilderContext)context).NewBuildUp(typeof(T), registration.Name));
                else
                    list.Add((T)((BuilderContext)context).NewBuildUp(registration));
            }

            context.Existing = list.ToArray();
            context.BuildComplete = true;
        }

        internal static void ResolveGenericArray<T>(IBuilderContext context, Type type)
        {
            var set = new MiniHashSet<InternalRegistration>();
            var container = (UnityContainer)context.Container;
            GetNamedRegistrations(container, typeof(T), set);
            GetNamedRegistrations(container, type, set);

            context.Existing = set.Select(registration => (T) ((BuilderContext) context).NewBuildUp(typeof(T), registration.Name))
                                  .ToArray();
            context.BuildComplete = true;
        }
        
        internal static void ResolveEnumerable<T>(IBuilderContext context)
        {
            var container = (UnityContainer)context.Container;
            var list = new List<T>();

            var registrations = (IList<InternalRegistration>)GetExplicitRegistrations(container, typeof(T));
            foreach (var registration in registrations)
            {
                if (registration.Type.GetTypeInfo().IsGenericTypeDefinition)
                    list.Add((T)((BuilderContext)context).NewBuildUp(typeof(T), registration.Name));
                else
                    list.Add((T)((BuilderContext)context).NewBuildUp(registration));
            }

            context.Existing = list;
            context.BuildComplete = true;
        }

        internal static void ResolveGenericEnumerable<T>(IBuilderContext context, Type type)
        {
            var set = new MiniHashSet<InternalRegistration>();
            var container = (UnityContainer)context.Container;
            GetExplicitRegistrations(container, typeof(T), set);
            GetExplicitRegistrations(container, type, set);

            context.Existing = set.Select(registration => (T) ((BuilderContext) context).NewBuildUp(typeof(T), registration.Name))
                                  .ToList();
            context.BuildComplete = true;
        }
        #endregion
    }
}
