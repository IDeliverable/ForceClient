using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using System.Xml.XPath;
using IDeliverable.Utils.Core;
using IDeliverable.Utils.Core.EventExtensions;

namespace IDeliverable.ForceClient.Metadata.Components
{
    public class CustomField : INotifyPropertyChanged, INotifyDataErrorInfo, IRevertibleChangeTracking
    {
        public CustomField(string objectName, string name, string label, string type, IEnumerable<XElement> additionalElements = null)
        {
            ObjectName = objectName;

            mName = name;
            mLabel = label;
            mType = type;

            mAdditionalElements = additionalElements ?? new XElement[] { };

            AcceptChanges();
            Validate();
        }

        #region XML conversion

        private static readonly XName sElementName = XName.Get("fields", MetadataPackage.MetadataXmlNamespace);

        public static CustomField FromXml(string objectName, XElement xml)
        {
            if (xml.Name != sElementName)
                throw new ArgumentException($"The element '{xml.Name}' is not supported; expected '{sElementName}'.");

            var nsm = xml.CreateNamespaceManager("m", MetadataPackage.MetadataXmlNamespace);

            var name = xml.XPathSelectElement("./m:fullName", nsm).Value;
            var label = xml.XPathSelectElement("./m:label", nsm).Value;
            var type = xml.XPathSelectElement("./m:type", nsm).Value;

            var additionalElements = xml.XPathSelectElements("./*[not(self::m:fullName or self::m:label or self::m:type)]", nsm);

            return new CustomField(objectName, name, label, type, additionalElements);
        }

        private readonly IEnumerable<XElement> mAdditionalElements;

        public XElement ToXml()
        {
            return
                new XElement
                (
                    sElementName,
                    new XElement(XName.Get("fullName", MetadataPackage.MetadataXmlNamespace), mName),
                    new XElement(XName.Get("label", MetadataPackage.MetadataXmlNamespace), mLabel),
                    new XElement(XName.Get("type", MetadataPackage.MetadataXmlNamespace), mType),
                    mAdditionalElements
                );
        }

        #endregion

        #region Navigation

        public string ObjectName { get; }

        #endregion

        #region Properties

        private string mName;
        private string mLabel;
        private string mType;

        public string Name
        {
            get => mName;
            set
            {
                if (!Equals(mName, value))
                {
                    mName = value;
                    ProcessPropertyChanges(nameof(Name));
                }
                mName = value;
            }
        }

        public string Label
        {
            get => mLabel;
            set
            {
                if (!Equals(mLabel, value))
                {
                    mLabel = value;
                    ProcessPropertyChanges(nameof(Label));
                }
                mLabel = value;
            }
        }

        public string Type
        {
            get => mType;
            set
            {
                if (!Equals(mType, value))
                {
                    mType = value;
                    ProcessPropertyChanges(nameof(Type));
                }
                mType = value;
            }
        }

        public override string ToString()
        {
            return mName;
        }

        #endregion

        #region Change tracking

        private string mNameOriginal;
        private string mLabelOriginal;
        private string mTypeOriginal;

        private bool mIsChanged;

        public bool IsChanged => mIsChanged;

        public string LabelOriginal => mLabelOriginal;

        protected void SetIsChanged(bool value)
        {
            if (value != mIsChanged)
            {
                mIsChanged = value;
                OnPropertyChanged(nameof(IsChanged));
            }
        }

        public void AcceptChanges()
        {
            mNameOriginal = mName;
            mLabelOriginal = mLabel;
            mTypeOriginal = mType;

            SetIsChanged(false);
        }

        public void RejectChanges()
        {
            Name = mNameOriginal;
            Label = mLabelOriginal;
            Type = mTypeOriginal;

            SetIsChanged(false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.SafeRaise(ExceptionHandlingMode.Swallow, null, this, new PropertyChangedEventArgs(propertyName));
        }

        private void ProcessPropertyChanges([CallerMemberName] string firstPropertyName = null, params string[] additionalPropertyNames)
        {
            var allPropertyNames = additionalPropertyNames.Union(new[] { firstPropertyName });

            foreach (var propertyNames in allPropertyNames)
                OnPropertyChanged(propertyNames);

            SetIsChanged(CalculateIsChanged());

            foreach (var propertyName in allPropertyNames)
                Validate(propertyName);
        }

        private bool CalculateIsChanged()
        {
            if (!Equals(mNameOriginal, mName))
                return true;
            if (!Equals(mLabelOriginal, mLabel))
                return true;
            if (!Equals(mTypeOriginal, mType))
                return true;

            return false;
        }

        #endregion

        #region Validation

        private List<ValidationError> mErrorList = new List<ValidationError>();

        public IEnumerable GetErrors(string propertyName)
        {
            return propertyName == null
                ? mErrorList.ToArray()
                : mErrorList.Where(x => x.PropertyName == propertyName).ToArray();
        }

        public bool HasErrors => mErrorList.Any();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.SafeRaise(ExceptionHandlingMode.Swallow, null, this, new DataErrorsChangedEventArgs(propertyName));
        }

        private void Validate(string propertyName = null)
        {
            var newErrorList = new List<ValidationError>();

            if (propertyName == null || propertyName == nameof(Label))
            {
                if (String.IsNullOrEmpty(mName))
                    newErrorList.Add(new ValidationError(nameof(Label), "Label cannot be empty.")); // TODO: Localize.
            }

            bool hasChanged;

            // NOTE: The following assumes that validation errors are always 
            // added to the list in the same order!
            hasChanged = !newErrorList.SequenceEqual(mErrorList);
            mErrorList = newErrorList;

            if (hasChanged)
            {
                OnErrorsChanged(propertyName);
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        #endregion
    }
}
