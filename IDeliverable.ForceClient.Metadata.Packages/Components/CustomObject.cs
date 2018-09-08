using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using IDeliverable.Utils.Core;

namespace IDeliverable.ForceClient.Metadata.Components
{
    public class CustomObject : IGrouping<CustomObject, CustomField>
    {
        private static readonly XName sElementName = XName.Get("CustomObject", Constants.MetadataXmlNamespace);

        public static CustomObject FromXml(string name, XElement xml)
        {
            if (xml.Name != sElementName)
                throw new ArgumentException($"The element '{xml.Name}' is not supported; expected '{sElementName}'.");

            var nsm = xml.CreateNamespaceManager("m", Constants.MetadataXmlNamespace);

            var fieldQuery =
                from fieldElement in xml.XPathSelectElements("./m:fields", nsm)
                select CustomField.FromXml(name, fieldElement);

            return new CustomObject(name, fieldQuery.ToArray());
        }

        public CustomObject(string name, IEnumerable<CustomField> customFields)
        {
            Name = name;
            CustomFields = customFields;
        }

        public string Name { get; }
        public bool IsStandardObject => !Name.EndsWith("__c");
        public IEnumerable<CustomField> CustomFields { get; }

        public XElement ToXml()
        {
            return
                new XElement
                (
                    sElementName,
                    from c in CustomFields select c.ToXml()
                );
        }

        public CustomObject SelectChanges()
        {
            var changedCustomFieldsQuery =
                from customField in CustomFields
                where customField.IsChanged
                select customField;

            if (changedCustomFieldsQuery.Any())
                return new CustomObject(Name, changedCustomFieldsQuery.ToArray());

            return null;
        }

        #region Grouping

        public CustomObject Key => this;

        public IEnumerator<CustomField> GetEnumerator()
        {
            return CustomFields.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return CustomFields.GetEnumerator();
        }

        #endregion
    }
}
