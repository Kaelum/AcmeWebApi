using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace WebApplication
{
	/// <summary>
	///		Summary description for ValidatorRecursive.
	/// </summary>
	public static class ValidatorRecursive
	{
		/// <summary>
		///		Determines whether the specified object is valid recursively.
		/// </summary>
		/// <param name="instance">The object to validate.</param>
		/// <param name="validationResults">A collection to hold each failed validation.</param>
		/// <returns></returns>
		public static bool TryValidateObject(object instance, ICollection<ValidationResult> validationResults)
			=> TryValidateObject(instance, validationResults, false);

		/// <summary>
		///		Determines whether the specified object is valid recursively.
		/// </summary>
		/// <param name="instance">The object to validate.</param>
		/// <param name="validationResults">A collection to hold each failed validation.</param>
		/// <param name="validateAllProperties"><b>true</b> to validate all properties; if
		///		<b>false</b>, only required attributes are validated.</param>
		/// <returns></returns>
		public static bool TryValidateObject(
			object instance,
			ICollection<ValidationResult> validationResults,
			bool validateAllProperties
		)
		{
			return TryValidateObject(instance, validationResults, validateAllProperties, null, null);
		}

		private static bool TryValidateObject(object instance, ICollection<ValidationResult> validationResults, bool validateAllProperties, string? parentName, HashSet<object>? validatedObjects)
		{
			if (validatedObjects == null)
			{
				validatedObjects = new HashSet<object>();
			}
			//short-circuit to avoid infinite loops on cyclical object graphs
			else if (validatedObjects.Contains(instance))
			{
				return true;
			}

			validatedObjects.Add(instance);
			bool result = Validator.TryValidateObject(instance, new ValidationContext(instance), validationResults, validateAllProperties);

			List<PropertyInfo> properties = instance.GetType().GetProperties().Where(prop =>
				prop.CanRead
				&& !prop.GetCustomAttributes(typeof(SkipRecursiveAttribute), false).Any()
				&& prop.GetIndexParameters().Length == 0
			).ToList();

			foreach (PropertyInfo property in properties)
			{
				if (property.PropertyType == typeof(string) || property.PropertyType.IsPrimitive) { continue; }

				object? propertyValue = instance.GetProperty(property.Name);

				if (propertyValue == null) { continue; }

				string propertyName = (
					parentName == null
					? property.Name
					: $"{parentName}.{property.Name}"
				);

				if (propertyValue is IEnumerable enumerableValue)
				{
					//KeyValuePair<string, int> test;
					foreach (object? enumeration in enumerableValue)
					{
						object? value;

						if (enumeration is DictionaryEntry dictionaryEntry)
						{
							value = dictionaryEntry.Value;
						}
						else if (enumeration != null && enumeration.GetType().IsGenericType && enumeration.GetType().Name.StartsWith("KeyValuePair"))
						{
							value = enumeration.GetProperty("Value");
						}
						else
						{
							value = null;
						}

						if (value == null)
						{
							if (enumeration != null)
							{
								List<ValidationResult> nestedResults = new List<ValidationResult>();

								if (!TryValidateObject(enumeration, nestedResults, validateAllProperties, propertyName, validatedObjects))
								{
									result = false;

									foreach (var validationResult in nestedResults)
									{
										//PropertyInfo property1 = property;
										validationResults.Add(new ValidationResult($"{validationResult.ErrorMessage}", validationResult.MemberNames.Select(x => $"{propertyName}.{x}")));
									}
								};
							}
						}
						else
						{
							List<ValidationResult> nestedResults = new List<ValidationResult>();

							if (!TryValidateObject(value, nestedResults, validateAllProperties, propertyName, validatedObjects))
							{
								result = false;

								foreach (var validationResult in nestedResults)
								{
									//PropertyInfo property1 = property;
									validationResults.Add(new ValidationResult($"{propertyName}-pair: {validationResult.ErrorMessage}", validationResult.MemberNames.Select(x => $"{propertyName}.{x}")));
								}
							};
						}
					}
				}
				else
				{
					List<ValidationResult> nestedResults = new List<ValidationResult>();

					if (!TryValidateObject(propertyValue, nestedResults, validateAllProperties, propertyName, validatedObjects))
					{
						result = false;

						foreach (var validationResult in nestedResults)
						{
							//PropertyInfo property1 = property;
							validationResults.Add(new ValidationResult($"{propertyName}-intrinsic: {validationResult.ErrorMessage}", validationResult.MemberNames.Select(x => $"{propertyName}.{x}")));
						}
					};
				}
			}

			return result;
		}
	}
}
