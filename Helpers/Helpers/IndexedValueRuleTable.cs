namespace Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// A table allowing fast lookup of value rules by their dimension values.
	/// </summary>
	public sealed class IndexedValueRuleTable : IndexedTable<IValueRule, IProperty, object>
	{
		/// <summary>
		/// The value rule signature.
		/// </summary>
		private readonly IValueRuleSignature valueRuleSignature;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedValueRuleTable" /> class.
		/// </summary>
		/// <param name="valueRuleSignature">The value rule signature.</param>
		/// <exception cref="ArgumentNullException">If any parameter is <c>null</c>.</exception>
		public IndexedValueRuleTable(IValueRuleSignature valueRuleSignature)
			: base(GetDimensions(valueRuleSignature).ToArray())
		{
			if (valueRuleSignature == null)
			{
				throw new ArgumentNullException("valueRuleSignature");
			}

			this.valueRuleSignature = valueRuleSignature;
		}

		/// <inheritdoc />
		protected override IEnumerable<KeyValuePair<IProperty, object>> GetColumnValuesFromRow(IValueRule row)
		{
			foreach (var dimension in this.GetDimensions())
			{
				yield return new KeyValuePair<IProperty, object>(dimension, row.GetValue(dimension));
			}
		}

		/// <summary>
		/// Gets the dimensions o the value rules represented by this set.
		/// </summary>
		/// <returns>The dimensions.</returns>
		private IEnumerable<IProperty> GetDimensions()
		{
			return GetDimensions(this.valueRuleSignature);
		}

		/// <summary>
		/// Gets the dimensions of the specified value rule signature.
		/// </summary>
		/// <param name="signature">The signature.</param>
		/// <returns>The dimensions.</returns>
		private static IEnumerable<IProperty> GetDimensions(IValueRuleSignature signature)
		{
			return signature.DimensionInputProperties;
		}
	}
}
