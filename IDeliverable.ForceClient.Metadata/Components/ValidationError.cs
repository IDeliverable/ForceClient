using System;
using System.Runtime.Serialization;

namespace IDeliverable.ForceClient.Metadata.Components
{
    [DataContract()]
    public class ValidationError : IEquatable<ValidationError>
    {
        public ValidationError(string propertyName, string message)
        {
            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("The parameter 'propertyName' cannot be null or empty.", nameof(propertyName));

            if (String.IsNullOrEmpty(message))
                throw new ArgumentException("The parameter 'message' cannot be null or empty.", nameof(message));

            mPropertyName = propertyName;
            mMessage = message;
        }


        [DataMember()]
        private readonly string mPropertyName;

        [DataMember()]
        private readonly string mMessage;

        public string PropertyName => mPropertyName;
        
        public string Message => mMessage;
        
        public override string ToString()
        {
            return mMessage;
        }
        
        public bool Equals(ValidationError other)
        {
            return Equals(mPropertyName, other.mPropertyName) && Equals(mMessage, other.mMessage);
        }
        
        public override bool Equals(object obj)
        {
            return Equals((ValidationError)obj);
        }
        
        public override int GetHashCode()
        {
            var hash = 13;
            hash = (hash * 7) + mPropertyName.GetHashCode();
            hash = (hash * 7) + mMessage.GetHashCode();
            return hash;
        }
    }
}
