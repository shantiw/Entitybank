using XData.Data.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    // System.ComponentModel.DataAnnotations.dll

    public static class AttributeFactory
    {
        public static KeyAttribute CreateKeyAttribute(this XElement annotation)
        {
            const string NAME = "Key";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            return new KeyAttribute();
        }

        // RowVersion
        public static TimestampAttribute CreateTimestampAttribute(this XElement annotation)
        {
            const string NAME = "Timestamp";
            const string SYNONYM = "RowVersion";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME && name != SYNONYM) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME + "/" + SYNONYM, name));

            return new TimestampAttribute();
        }

        public static ConcurrencyCheckAttribute CreateConcurrencyCheckAttribute(this XElement annotation)
        {
            const string NAME = "ConcurrencyCheck";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            return new ConcurrencyCheckAttribute();
        }

        public static DefaultValueAttribute CreateDefaultValueAttribute(this XElement annotation, XElement propertySchema)
        {
            const string NAME = "DefaultValue";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string dataType = propertySchema.Attribute(SchemaVocab.DataType).Value;
            Type type = GetType(dataType);

            string value = annotation.GetArgumentValue("Value");
            return new DefaultValueAttribute(type, value);
        }

        public static DisplayNameAttribute CreateDisplayNameAttribute(this XElement annotation)
        {
            const string NAME = "DisplayName";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string displayName = annotation.GetArgumentValue("DisplayName");
            return new DisplayNameAttribute(displayName);
        }

        public static DisplayAttribute CreateDisplayAttribute(this XElement annotation)
        {
            const string NAME = "Display";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            DisplayAttribute attribute = new DisplayAttribute();
            string value = annotation.GetArgumentValue("AutoGenerateField");
            if (value != null)
            {
                attribute.AutoGenerateField = bool.Parse(value);
            }
            value = annotation.GetArgumentValue("AutoGenerateFilter");
            if (value != null)
            {
                attribute.AutoGenerateFilter = bool.Parse(value);
            }
            value = annotation.GetArgumentValue("Description");
            if (value != null)
            {
                attribute.Description = value;
            }
            value = annotation.GetArgumentValue("GroupName");
            if (value != null)
            {
                attribute.GroupName = value;
            }
            value = annotation.GetArgumentValue("Name");
            if (annotation.Element("Name") != null)
            {
                attribute.Name = value;
            }
            value = annotation.GetArgumentValue("Order");
            if (value != null)
            {
                attribute.Order = int.Parse(value);
            }
            value = annotation.GetArgumentValue("Prompt");
            if (value != null)
            {
                attribute.Name = value;
            }
            value = annotation.GetArgumentValue("ResourceType");
            if (value != null)
            {
                attribute.ResourceType = GetType(value);
            }
            value = annotation.GetArgumentValue("ShortName");
            if (value != null)
            {
                attribute.Name = value;
            }

            return attribute;
        }

        public static DisplayFormatAttribute CreateDisplayFormatAttribute(this XElement annotation)
        {
            const string NAME = "DisplayFormat";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            DisplayFormatAttribute attribute = new DisplayFormatAttribute();
            string value = annotation.GetArgumentValue("ApplyFormatInEditMode");
            if (value != null)
            {
                attribute.ApplyFormatInEditMode = bool.Parse(value);
            }
            value = annotation.GetArgumentValue("ConvertEmptyStringToNull");
            if (value != null)
            {
                attribute.ConvertEmptyStringToNull = bool.Parse(value);
            }
            value = annotation.GetArgumentValue("DataFormatString");
            if (value != null)
            {
                attribute.DataFormatString = value;
            }
            value = annotation.GetArgumentValue("HtmlEncode");
            if (value != null)
            {
                attribute.HtmlEncode = bool.Parse(value);
            }
            value = annotation.GetArgumentValue("NullDisplayText");
            if (value != null)
            {
                attribute.NullDisplayText = value;
            }

            return attribute;
        }

        //    System.ComponentModel.DataAnnotations.ValidationAttribute
        //      System.ComponentModel.DataAnnotations.CompareAttribute
        //      System.ComponentModel.DataAnnotations.CustomValidationAttribute
        //      System.ComponentModel.DataAnnotations.DataTypeAttribute
        //        System.ComponentModel.DataAnnotations.CreditCardAttribute
        //        System.ComponentModel.DataAnnotations.EmailAddressAttribute
        //        System.ComponentModel.DataAnnotations.EnumDataTypeAttribute
        //        System.ComponentModel.DataAnnotations.FileExtensionsAttribute
        //        System.ComponentModel.DataAnnotations.PhoneAttribute
        //        System.ComponentModel.DataAnnotations.UrlAttribute
        //      System.ComponentModel.DataAnnotations.MaxLengthAttribute
        //      System.ComponentModel.DataAnnotations.MinLengthAttribute
        //      System.ComponentModel.DataAnnotations.RangeAttribute
        //      System.ComponentModel.DataAnnotations.RegularExpressionAttribute
        //      System.ComponentModel.DataAnnotations.RequiredAttribute
        //      System.ComponentModel.DataAnnotations.StringLengthAttribute

        //
        public static CompareAttribute CreateCompareAttribute(this XElement annotation)
        {
            const string NAME = "Compare";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string value = annotation.GetArgumentValue("OtherProperty");
            CompareAttribute attribute = new CompareAttribute(value);

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static CustomValidationAttribute CreateCustomValidationAttribute(this XElement annotation)
        {
            const string NAME = "CustomValidation";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string type = annotation.GetArgumentValue("ValidatorType");
            Type validatorType = GetType(type);
            string method = annotation.GetArgumentValue("Method");
            CustomValidationAttribute attribute = new CustomValidationAttribute(validatorType, method);

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        //
        public static DataTypeAttribute CreateDataTypeAttribute(this XElement annotation)
        {
            const string NAME = "DataType";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException();

            DataTypeAttribute attribute = null;
            string value = annotation.GetArgumentValue("CustomDataType");
            if (value != null)
            {
                attribute = new DataTypeAttribute(value);
            }

            value = annotation.GetArgumentValue("DataType");
            if (value != null)
            {
                attribute = new DataTypeAttribute((DataType)Enum.Parse(typeof(DataType), value));
            }

            if (attribute == null) throw new ArgumentException(SchemaMessages.NotProvideParameter);

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static CreditCardAttribute CreateCreditCardAttribute(this XElement annotation)
        {
            const string NAME = "CreditCard";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            CreditCardAttribute attribute = new CreditCardAttribute();

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static EmailAddressAttribute CreateEmailAddressAttribute(this XElement annotation)
        {
            const string NAME = "EmailAddress";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            EmailAddressAttribute attribute = new EmailAddressAttribute();

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static EnumDataTypeAttribute CreateEnumDataTypeAttribute(this XElement annotation)
        {
            const string NAME = "EnumDataType";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string enumType = annotation.GetArgumentValue("EnumType");
            Type type = GetType(enumType);
            EnumDataTypeAttribute attribute = new EnumDataTypeAttribute(type);

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static FileExtensionsAttribute CreateFileExtensionsAttribute(this XElement annotation)
        {
            const string NAME = "FileExtensions";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            FileExtensionsAttribute attribute = new FileExtensionsAttribute();
            string extensions = annotation.GetArgumentValue("Extensions");
            if (extensions != null)
            {
                attribute.Extensions = extensions;
            }

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static PhoneAttribute CreatePhoneAttribute(this XElement annotation)
        {
            const string NAME = "Phone";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            PhoneAttribute attribute = new PhoneAttribute();

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static UrlAttribute CreateUrlAttribute(this XElement annotation)
        {
            const string NAME = "Url";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            UrlAttribute attribute = new UrlAttribute();

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        //
        public static MaxLengthAttribute CreateMaxLengthAttribute(this XElement annotation)
        {
            const string NAME = "MaxLength";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string value = annotation.GetArgumentValue("Length");
            int length = int.Parse(value);
            MaxLengthAttribute attribute = new MaxLengthAttribute(length);

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static MinLengthAttribute CreateMinLengthAttribute(this XElement annotation)
        {
            const string NAME = "MinLength";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string value = annotation.GetArgumentValue("Length");
            int length = int.Parse(value);
            MinLengthAttribute attribute = new MinLengthAttribute(length);

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static RangeAttribute CreateRangeAttribute(this XElement annotation)
        {
            const string NAME = "Range";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            XElement property = annotation.Parent;
            string dataType = property.Attribute(SchemaVocab.DataType).Value;
            Type type = GetType(dataType);

            string minimum = annotation.GetArgumentValue("Minimum");
            string maximum = annotation.GetArgumentValue("Maximum");

            RangeAttribute attribute;
            if (type == typeof(int))
            {
                attribute = new RangeAttribute(int.Parse(minimum), int.Parse(maximum));
            }
            else if (type == typeof(double))
            {
                attribute = new RangeAttribute(double.Parse(minimum), double.Parse(maximum));
            }
            else
            {
                attribute = new RangeAttribute(type, minimum, maximum);
            }

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static RegularExpressionAttribute CreateRegularExpressionAttribute(this XElement annotation)
        {
            const string NAME = "RegularExpression";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string pattern = annotation.GetArgumentValue("Pattern");
            var attribute = new RegularExpressionAttribute(pattern);

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        //MSDN
        //Remarks
        //When you set AllowEmptyStrings to true for a data field, Dynamic Data does not perform validation and transforms the empty string to a null value.This value is then passed to the database.
        //If the database does not allow null values, it raises an error.To avoid this error, you must also set the DisplayFormatAttribute.ConvertEmptyStringToNull to false.
        //  AllowEmptyStrings   false(default)  true
        //  null                false           false
        //  string.Empty        false           true
        //  "  "                false           true
        //  "ABC"               true            true
        public static RequiredAttribute CreateRequiredAttribute(this XElement annotation)
        {
            const string NAME = "Required";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            RequiredAttribute attribute = new RequiredAttribute();
            string allowEmptyStrings = annotation.GetArgumentValue("AllowEmptyStrings");
            if (allowEmptyStrings != null)
            {
                attribute.AllowEmptyStrings = bool.Parse(allowEmptyStrings);
            }

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        public static StringLengthAttribute CreateStringLengthAttribute(this XElement annotation)
        {
            const string NAME = "StringLength";
            string name = annotation.Attribute(SchemaVocab.Name).Value;
            if (name != NAME) throw new ArgumentException(string.Format(SchemaMessages.ExpectedBut, NAME, name));

            string maximumLength = annotation.GetArgumentValue("MaximumLength");
            StringLengthAttribute attribute = new StringLengthAttribute(int.Parse(maximumLength));
            string minimumLength = annotation.GetArgumentValue("MinimumLength");
            if (minimumLength != null)
            {
                attribute.MinimumLength = int.Parse(minimumLength);
            }

            FillValidationAttribute(attribute, annotation);
            return attribute;
        }

        //
        private static void FillValidationAttribute(ValidationAttribute attribute, XElement annotation)
        {
            if (annotation.Element("ErrorMessage") != null)
            {
                attribute.ErrorMessage = annotation.Element("ErrorMessage").Value;
            }
            if (annotation.Element("ErrorMessageResourceType") != null)
            {
                string type = annotation.Element("ErrorMessageResourceType").Value;
                attribute.ErrorMessageResourceType = GetType(type);
            }
            if (annotation.Element("ErrorMessageResourceName") != null)
            {
                attribute.ErrorMessageResourceName = annotation.Element("ErrorMessageResourceName").Value;
            }
        }

        private static string GetArgumentValue(this XElement annotation, string name)
        {
            XElement xArgument = annotation.Elements(SchemaVocab.Argument).FirstOrDefault(x => x.Attribute(SchemaVocab.Name).Value == name);
            if (xArgument == null) return null;

            return xArgument.Attribute(SchemaVocab.Value).Value;
        }

        private static Type GetType(string type)
        {
            return TypeHelper.GetType(type);
        }

        internal static List<ValidationAttribute> CreateValidationAttributes(this XElement propertySchema)
        {
            List<ValidationAttribute> validationAttributes = new List<ValidationAttribute>();
            foreach (XElement annotation in propertySchema.Elements())
            {
                ValidationAttribute validationAttribute = null;
                switch (annotation.Name.LocalName)
                {
                    case "Compare":
                        validationAttribute = annotation.CreateCompareAttribute();
                        break;
                    case "CustomValidation":
                        validationAttribute = annotation.CreateCustomValidationAttribute();
                        break;
                    case "DataType":
                        validationAttribute = annotation.CreateDataTypeAttribute();
                        break;
                    case "Range":
                        validationAttribute = annotation.CreateRangeAttribute();
                        break;
                    case "RegularExpression":
                        validationAttribute = annotation.CreateRegularExpressionAttribute();
                        break;
                    case "Required":
                        validationAttribute = annotation.CreateRequiredAttribute();
                        break;
                    case "StringLength":
                        validationAttribute = annotation.CreateStringLengthAttribute();
                        break;

                    // .NET Framework 4.5
                    case "MaxLength":
                        validationAttribute = annotation.CreateMaxLengthAttribute();
                        break;
                    case "MinLength":
                        validationAttribute = annotation.CreateMinLengthAttribute();
                        break;
                    case "CreditCard":
                        validationAttribute = annotation.CreateCreditCardAttribute();
                        break;
                    case "EmailAddress":
                        validationAttribute = annotation.CreateEmailAddressAttribute();
                        break;
                    case "Phone":
                        validationAttribute = annotation.CreatePhoneAttribute();
                        break;
                    case "Url":
                        validationAttribute = annotation.CreateUrlAttribute();
                        break;
                    case "EnumDataType":
                        validationAttribute = annotation.CreateEnumDataTypeAttribute();
                        break;
                    case "FileExtensions":
                        validationAttribute = annotation.CreateFileExtensionsAttribute();
                        break;
                }
                if (validationAttribute != null)
                {
                    validationAttributes.Add(validationAttribute);
                }
            }
            return validationAttributes;
        }


    }
}