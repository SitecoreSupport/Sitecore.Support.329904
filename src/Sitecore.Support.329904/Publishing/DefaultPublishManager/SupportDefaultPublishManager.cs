using Sitecore.Abstractions;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Engines;
using Sitecore.Data.Engines.DataCommands;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Publishing;
using Sitecore.Publishing.Diagnostics;
using System;
using static Sitecore.Configuration.Settings;

namespace Sitecore.Support.Publishing.DefaultPublishManager
{
    public class SupportDefaultPublishManager : Sitecore.Publishing.DefaultPublishManager
    {

        public SupportDefaultPublishManager(BaseLanguageManager languageManager, BaseFactory factory, BaseLog log, ProviderHelper<PublishProvider, PublishProviderCollection> providerHelper) : base(languageManager, factory, log, providerHelper)
        {
        }

        public SupportDefaultPublishManager(BaseLanguageManager languageManager, BaseFactory factory, BaseLog log, ProviderHelper<PublishProvider, PublishProviderCollection> providerHelper, BaseEventQueueProvider provider) : base(languageManager, factory, log, providerHelper, provider)
        {
        }


        protected new void DataEngine_SavedItem(object sender, ExecutedEventArgs<SaveItemCommand> e)
        {
            FieldChangeList fieldChanges = e.Command.Changes.FieldChanges;

            //should equal 4 for Lock/Unlock operation
            int fieldChangesCount = fieldChanges.Count;

            //flag  - true if 4 necessary fields (Lock, Revision, Updated, UpdatedBy) were changed
            bool flag = fieldChanges.Contains(Sitecore.FieldIDs.Lock) & fieldChanges.Contains(Sitecore.FieldIDs.Revision) & fieldChanges.Contains(Sitecore.FieldIDs.Updated) & fieldChanges.Contains(Sitecore.FieldIDs.UpdatedBy);

            //if only 4 necessary fields were changed (i.e. Lock/Unlock command were executed), there is no need to executed this handler. Otherwise, base handler should be called

            if (flag & (fieldChangesCount == 4))
            {
                Log.Debug("SupportDefaultPublishManager.DataEngine_SavedItem has not been executed due to Lock/Unlock operation was performed", this);
            }
            else
            {
                base.DataEngine_SavedItem(sender, e);
            }
        }


        public override void Initialize()
        {
            if (!Settings.Publishing.Enabled)
            {
                PublishingLog.Warn("Publishing is disabled due to running under a restricted license.", null);
            }
            else
            {
                base.Initialize();

                //re-subscribe DataEngine_SavedItem event handler from base class to this custom class

                foreach (Database database in Factory.GetDatabases())
                {
                    DataEngine dataEngine = database.Engines.DataEngine;

                    //unsubscribe base event handler
                    dataEngine.SavedItem -= new EventHandler<ExecutedEventArgs<SaveItemCommand>>(base.DataEngine_SavedItem);

                    //subscribe this event handler
                    dataEngine.SavedItem += new EventHandler<ExecutedEventArgs<SaveItemCommand>>(this.DataEngine_SavedItem);
                }
            }
        }
    }
}