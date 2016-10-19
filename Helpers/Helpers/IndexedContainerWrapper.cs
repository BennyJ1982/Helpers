namespace Helpers
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// A value rule container wrapper that uses indexes in order to speed up lookups.
	/// </summary>
	public class IndexedContainerWrapper : ValueRuleContainerWrapperDecorator
	{
		/// <summary>
		/// The indexed value rule table.
		/// </summary>
		private readonly IndexedValueRuleTable indexedValueRuleTable;

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedContainerWrapper" /> class.
		/// </summary>
		/// <param name="previousWrapper">The previous wrapper.</param>
		/// <exception cref="System.ArgumentNullException">If any parameter is <c>null</c>.</exception>
		public IndexedContainerWrapper(IValueRuleContainerWrapper previousWrapper)
			: base(previousWrapper)
		{
			if (previousWrapper == null)
			{
				throw new ArgumentNullException("previousWrapper");
			}

			this.indexedValueRuleTable = new IndexedValueRuleTable(previousWrapper.ValueRuleContainer.ValueRuleSignature);
			this.indexedValueRuleTable.AddRange(previousWrapper.ValueRuleContainer.ValueRules);
		}

		/// <inheritdoc />
		public override IEnumerable<IValueRule> GetAll(IDictionary<IProperty, object> dimensionPropertyValues)
		{
			return this.indexedValueRuleTable.Lookup(dimensionPropertyValues);
		}

		/// <inheritdoc />
		public override bool TryUpdateDimensionProperties(IValueRule valueRule, Func<bool> updateCallback)
		{
			// first remove exising value rule fom the set
			this.indexedValueRuleTable.Remove(valueRule);

			try
			{
				// change dimension values
				return updateCallback();
			}
			finally
			{
				// re-add changed value rule to the set
				this.indexedValueRuleTable.Add(valueRule);
			}
		}
	}
}
