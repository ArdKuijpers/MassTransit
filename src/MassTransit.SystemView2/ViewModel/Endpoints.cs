// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace MassTransit.SystemView.ViewModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Services.Subscriptions.Messages;
    using Distributor.Messages;

    public class Endpoints :
        KeyedCollectionBase<Endpoint, Uri>
    {
        public bool Remove(Uri endpointUri, string messageName)
        {
            var retValue = Items[endpointUri].Messages.Remove(messageName);
            if (Items[endpointUri].Messages.Count == 0 && Items[endpointUri].Workers.Count == 0)
            {
                var endpoint = Items[endpointUri];
                retValue &= Remove(endpoint);
            }
            return retValue;
        }

        public void Update(IList<SubscriptionInformation> subscriptions)
        {
            subscriptions.ToList().ForEach(Update);
        }

        public void Update(SubscriptionInformation si)
        {
            if (Items.Keys.Contains(si.EndpointUri))
            {
                Items[si.EndpointUri].Messages.Update(new Message
                {
                    MessageName = si.MessageName,
                    ClientId = si.ClientId,
                    CorrelationId = si.CorrelationId,
                    SequenceNumber = si.SequenceNumber,
                    SubscriptionId = si.SubscriptionId
                });
            }
            else
            {
                var endpoint = new Endpoint { EndpointUri = si.EndpointUri };
                Add(endpoint);

                endpoint.Messages.Update(new Message
                {
                    MessageName = si.MessageName,
                    ClientId = si.ClientId,
                    CorrelationId = si.CorrelationId,
                    SequenceNumber = si.SequenceNumber,
                    SubscriptionId = si.SubscriptionId
                });
            }
        }

        public void Update(IWorkerAvailable wa)
        {
            Update(wa.DataUri, wa);
            Update(wa.ControlUri, wa);

            Regex regex = new Regex(@"MassTransit\.Distributor\.Messages\.WorkerAvailable`\d+\[\[(?<type>.+), (?<assembly>.+)\]\], MassTransit");

            Items.Values.Where(x =>
            {
                return x.Messages
                    .ToList()
                    .Where(y =>
                    {
                        return y.MessageName.StartsWith("MassTransit.Distributor.Messages.WorkerAvailable`");
                    })
                    .Where(y =>
                    {
                        return regex.Replace(y.MessageName, "${type}") == wa.WorkerItemType;
                    })
                    .Count() > 0;
            })
            .ToList()
            .ForEach(x => Update(x.EndpointUri, wa));
        }

        private void Update(Uri endpointUri, IWorkerAvailable wa)
        {
            if (!Items.Keys.Contains(endpointUri))
            {
                Add(new Endpoint { EndpointUri = endpointUri });
            }

            Items[endpointUri].Workers.Update(new Worker
            {
                MessageType = wa.WorkerItemType,
                Pending = wa.Pending,
                InProgress = wa.InProgress,
                InProgressLimit = wa.InProgressLimit,
                PendingLimit = wa.PendingLimit,
                Updated = wa.Updated,
                ControlUri = wa.ControlUri,
                DataUri = wa.DataUri
            });
        }
    }
}