using System;
using System.Collections.Generic;
using Microsoft.Bot.Connector;

namespace RightpointLabs.BotLib
{
    public class AuthenticationResultActivity : SimpleBaseActivity
    {
        public string Code { get; set; }
        public Uri RequestUri { get; set; }
        public LoginState State { get; set; }
        public Action<string> Done { get; set; }
        public string Error { get; set; }
        public string ErrorDescription { get; set; }

        public AuthenticationResultActivity(IActivity innerActivity) : base(innerActivity)
        {
        }
    }

    public class SimpleBaseActivity : IActivity
    {
        public SimpleBaseActivity(IActivity other)
        {
            this.Type = other.Type;
            this.Id = other.Id;
            this.ServiceUrl = other.ServiceUrl;
            this.Timestamp = other.Timestamp;
            this.LocalTimestamp = other.LocalTimestamp;
            this.ChannelId = other.ChannelId;
            this.From = other.From;
            this.Conversation = other.Conversation;
            this.Recipient = other.Recipient;
            this.ReplyToId = other.ReplyToId;
            this.ChannelData = other.ChannelData;
            this.Entities = other.Entities;
        }

        public IMessageActivity AsMessageActivity()
        {
            return null;
        }

        public IContactRelationUpdateActivity AsContactRelationUpdateActivity()
        {
            return null;
        }

        public IInstallationUpdateActivity AsInstallationUpdateActivity()
        {
            return null;
        }

        public IConversationUpdateActivity AsConversationUpdateActivity()
        {
            return null;
        }

        public ITypingActivity AsTypingActivity()
        {
            return null;
        }

        public IEndOfConversationActivity AsEndOfConversationActivity()
        {
            return null;
        }

        public IEventActivity AsEventActivity()
        {
            return null;
        }

        public IInvokeActivity AsInvokeActivity()
        {
            return null;
        }

        public TypeT GetChannelData<TypeT>()
        {
            return default(TypeT);
        }

        public bool TryGetChannelData<TypeT>(out TypeT instance)
        {
            instance = default(TypeT);
            return false;
        }

        public IMessageUpdateActivity AsMessageUpdateActivity()
        {
            return null;
        }

        public IMessageDeleteActivity AsMessageDeleteActivity()
        {
            return null;
        }

        public IMessageReactionActivity AsMessageReactionActivity()
        {
            return null;
        }

        public ISuggestionActivity AsSuggestionActivity()
        {
            return null;
        }

        public string Type { get; set; }
        public string Id { get; set; }
        public string ServiceUrl { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public DateTimeOffset? LocalTimestamp { get; set; }
        public string ChannelId { get; set; }
        public ChannelAccount From { get; set; }
        public ConversationAccount Conversation { get; set; }
        public ChannelAccount Recipient { get; set; }
        public string ReplyToId { get; set; }
        public dynamic ChannelData { get; set; }
        public IList<Entity> Entities { get; set; }
    }
}