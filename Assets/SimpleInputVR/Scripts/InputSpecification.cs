namespace SimpleInputVR
{
	/// <summary>
	/// This struct contains everything the class needs to know about an input value.
	/// Just leave the possible input values empty if you have no constraints!
	/// </summary>
	[System.Serializable]
	public struct InputSpecification {
		public InputType type;
		public string inputDescription;
		public string[] possibleInputValues;

		/// <summary>
		/// Checks if a certain value would match the given specifications.
		/// </summary>
		/// <returns><c>true</c>, if the given value is valid, <c>false</c> otherwise.</returns>
		/// <param name="value">The value in question.</param>
		public bool isValidValue(string value)
		{
			if (possibleInputValues.Length > 0)
			{
				// There are predefined values for this input value. Compare with all possible values:
				foreach (string s in possibleInputValues)
				{
					if (type == InputType.Integer)
					{
						int expectedValue = 0;
						int enteredValue = 0;
						// Try to parse both the entered and the expected value.
						if (int.TryParse(s, out expectedValue) && int.TryParse(value, out enteredValue) && expectedValue == enteredValue)
						{
							// Both int values can be parsed and are equal to each other.
							return true;
						}
					}
					else if (type == InputType.UnsignedInteger)
					{
						uint expectedValue = 0;
						uint enteredValue = 0;
						// Try to parse both the entered and the expected value.
						if (uint.TryParse(s, out expectedValue) && uint.TryParse(value, out enteredValue) && expectedValue == enteredValue)
						{
							// Both int values can be parsed and are equal to each other.
							return true;
						}
					}
					else if (type == InputType.FloatingPointNumber)
					{
						// Try to parse both the entered and the expected value.
						float expectedValue = 0;
						float enteredValue = 0;
						if (float.TryParse(s, out expectedValue) && float.TryParse(value, out enteredValue) && expectedValue == enteredValue)
						{
							// Both float values can be parsed and are equal to each other.
							return true;
						}
					}
					else if (type == InputType.String)
					{
						if (string.Equals(s, value))
						{
							// The two strings are equal to each other.
							return true;
						}
					}
				}
				return false;
			}
			else
			{
				// There are no predefined values for this input value. Check the type according to the InputType:
				if (type == InputType.Integer)
				{
					int outValue;
					return int.TryParse(value, out outValue);
				}
				else if (type == InputType.UnsignedInteger)
				{
					uint outValue;
					return uint.TryParse(value, out outValue);
				}
				else if (type == InputType.FloatingPointNumber)
				{
					float outValue;
					return float.TryParse(value, out outValue);
				}
				else if (type == InputType.String)
				{
					// It's always a string at minimum.
					return true;
				}
				else
				{
					// The enum has grown bigger but the new types aren't supported yet...
					return false;
				}
			}
		}
	}
}