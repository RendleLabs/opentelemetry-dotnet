﻿// <copyright file="TracerNew.cs" company="OpenTelemetry Authors">
// Copyright The OpenTelemetry Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.Trace
{
    /// <summary>
    /// Tracer to record distributed tracing information.
    /// </summary>
    public class TracerNew
    {
        private readonly ActivitySource activitySource;

        internal TracerNew(ActivitySource activitySource)
        {
            this.activitySource = activitySource;
        }

        /// <summary>
        /// Gets the current span from the context.
        /// </summary>
        public TelemetrySpanNew CurrentSpan
        {
            get
            {
                return new TelemetrySpanNew(Activity.Current);
            }
        }

        /// <summary>
        /// Starts span.
        /// </summary>
        /// <param name="name">Span name.</param>
        /// <param name="kind">Kind.</param>
        /// <returns>Span instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpanNew StartSpan(string name, SpanKind kind = SpanKind.Internal)
        {
            // TODO: Open Question - should we have both StartSpan and StartActiveSpan?
            // Or should we call this method StartActiveSpan
            // This method StartSpan is starting a Span and making it Active.
            // OTel spec calls for StartSpan, and StartActiveSpan being separate.
            // Need to see if it makes sense for .NET to strictly adhere to it.
            // Some discussions in Spec: https://github.com/open-telemetry/opentelemetry-specification/pull/485
            if (!this.activitySource.HasListeners())
            {
                return TelemetrySpanNew.NoopInstance;
            }

            var activityKind = this.ConvertToActivityKind(kind);
            var activity = this.activitySource.StartActivity(name, activityKind);
            if (activity == null)
            {
                return TelemetrySpanNew.NoopInstance;
            }

            return new TelemetrySpanNew(activity);
        }

        /// <summary>
        /// Starts span.
        /// </summary>
        /// <param name="name">Span name.</param>
        /// <param name="kind">Kind.</param>
        /// <param name="parent">Parent for new span.</param>
        /// <returns>Span instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpanNew StartSpan(string name, SpanKind kind, in SpanContextNew parent)
        {
            if (!this.activitySource.HasListeners())
            {
                return TelemetrySpanNew.NoopInstance;
            }

            var activityKind = this.ConvertToActivityKind(kind);
            var activity = this.activitySource.StartActivity(name, activityKind, parent.ActivityContext);
            if (activity == null)
            {
                return TelemetrySpanNew.NoopInstance;
            }

            return new TelemetrySpanNew(activity);
        }

        /// <summary>
        /// Starts span.
        /// </summary>
        /// <param name="name">Span name.</param>
        /// <param name="kind">Kind.</param>
        /// <param name="parentSpan">Parent for new span.</param>
        /// <param name="attributes">Initial attributes for the span.</param>
        /// <param name="links"> <see cref="Link"/> for the span.</param>
        /// <param name="startTime"> Start time for the span.</param>
        /// <returns>Span instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpanNew StartSpan(string name, SpanKind kind, in TelemetrySpanNew parentSpan, IEnumerable<KeyValuePair<string, string>> attributes = null, IEnumerable<LinkNew> links = null, DateTimeOffset startTime = default)
        {
            return this.StartSpan(name, kind, parentSpan.Context, attributes, links, startTime);
        }

        /// <summary>
        /// Starts span.
        /// </summary>
        /// <param name="name">Span name.</param>
        /// <param name="kind">Kind.</param>
        /// <param name="parentContext">Parent Context for new span.</param>
        /// <param name="attributes">Initial attributes for the span.</param>
        /// <param name="links"> <see cref="Link"/> for the span.</param>
        /// <param name="startTime"> Start time for the span.</param>
        /// <returns>Span instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpanNew StartSpan(string name, SpanKind kind, in SpanContextNew parentContext, IEnumerable<KeyValuePair<string, string>> attributes = null, IEnumerable<LinkNew> links = null, DateTimeOffset startTime = default)
        {
            if (!this.activitySource.HasListeners())
            {
                return TelemetrySpanNew.NoopInstance;
            }

            var activityKind = this.ConvertToActivityKind(kind);

            IList<ActivityLink> activityLinks = null;
            if (links != null && links.Count() > 0)
            {
                activityLinks = new List<ActivityLink>();
                foreach (var link in links)
                {
                    activityLinks.Add(link.ActivityLink);
                }
            }

            var activity = this.activitySource.StartActivity(name, activityKind, parentContext.ActivityContext, attributes, activityLinks, startTime);
            if (activity == null)
            {
                return TelemetrySpanNew.NoopInstance;
            }

            return new TelemetrySpanNew(activity);
        }

        /// <summary>
        /// Makes the given span as the current one.
        /// </summary>
        /// <param name="span">The span to be made current.</param>
        /// <returns>The current span.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TelemetrySpanNew WithSpan(TelemetrySpanNew span)
        {
            span.Activate();
            return span;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ActivityKind ConvertToActivityKind(SpanKind kind)
        {
            switch (kind)
            {
                case SpanKind.Client:
                    return ActivityKind.Client;
                case SpanKind.Consumer:
                    return ActivityKind.Consumer;
                case SpanKind.Internal:
                    return ActivityKind.Internal;
                case SpanKind.Producer:
                    return ActivityKind.Producer;
                case SpanKind.Server:
                    return ActivityKind.Server;
                default:
                    return ActivityKind.Internal;
            }
        }
    }
}