namespace Helpers
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Base class for tables that enable fast lookups by indexed column values.
	/// </summary>
	/// <typeparam name="TRow">The type of the rows.</typeparam>
	/// <typeparam name="TColumn">The type of the (indexable) columns.</typeparam>
	/// <typeparam name="TValue">The type of the column values.</typeparam>
	public abstract class IndexedTable<TRow, TColumn, TValue>
	{
		/// <summary>
		/// The internal index of columns and column values.
		/// </summary>
		private readonly Dictionary<TColumn, RowsPerValueDictionary> index = new Dictionary<TColumn, RowsPerValueDictionary>();

		/// <summary>
		/// Initializes a new instance of the <see cref="IndexedTable{TRow,TColumn,TValue}" /> class.
		/// </summary>
		/// <param name="indexedColumns">The indexed columns. Make sure this contains all columns used for lookup.</param>
		/// <exception cref="ArgumentNullException">If any parameter is <c>null</c>.</exception>
		protected IndexedTable(IEnumerable<TColumn> indexedColumns)
		{
			if (indexedColumns == null)
			{
				throw new ArgumentNullException("indexedColumns");
			}

			foreach (var column in indexedColumns)
			{
				this.index[column] = new RowsPerValueDictionary();
			}
		}

		/// <summary>
		/// Adds the specified rows.
		/// </summary>
		/// <param name="rows">The rows.</param>
		public void AddRange(IEnumerable<TRow> rows)
		{
			foreach (var row in rows)
			{
				this.Add(row);
			}
		}

		/// <summary>
		/// Adds the specified row.
		/// </summary>
		/// <param name="row">The row.</param>
		public void Add(TRow row)
		{
			foreach (var columnValue in this.GetColumnValuesFromRow(row))
			{
				var rowsPerValue = this.index[columnValue.Key];
				rowsPerValue.Add(columnValue.Value, row);
			}
		}

		/// <summary>
		/// Removes the specified row.
		/// </summary>
		/// <param name="row">The row.</param>
		public void Remove(TRow row)
		{
			foreach (var columnValue in this.GetColumnValuesFromRow(row))
			{
				var rowsPerValue = this.index[columnValue.Key];
				RowSet rowSet;
				if (rowsPerValue.TryGetValue(columnValue.Value, out rowSet))
				{
					rowSet.Remove(row);
				}
			}
		}

		/// <summary>
		/// Looks up rows matching the specified indexed column values.
		/// </summary>
		/// <param name="columnValues">The column values.</param>
		/// <returns>The found rows.</returns>
		public IEnumerable<TRow> Lookup(IDictionary<TColumn, TValue> columnValues)
		{
			List<RowSet> rowSets;
			if (!this.TryGetMatchingRowSets(columnValues, out rowSets) || rowSets.Count == 0)
			{
				return Enumerable.Empty<TRow>();
			}

			// sort row sets ascending by their number of row.
			rowSets.Sort(RowSetCountComparer);

			return GetRowsContainedInAllSets(rowSets);
		}

		/// <summary>
		/// Gets the indexed column values from the specified row.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>The column values.</returns>
		protected abstract IEnumerable<KeyValuePair<TColumn, TValue>> GetColumnValuesFromRow(TRow row);

		/// <summary>
		/// Tries to get matching rows sets for the specified column values.
		/// </summary>
		/// <param name="columnValues">The indexed column values.</param>
		/// <param name="matchingRowSets">The matching row sets.</param>
		/// <returns><c>true</c> if getting the matching row sets was successful; otherwise, <c>false</c></returns>
		private bool TryGetMatchingRowSets(IDictionary<TColumn, TValue> columnValues, out List<RowSet> matchingRowSets)
		{
			var rowSets = new List<RowSet>();
			foreach (var columnValue in columnValues)
			{
				RowSet rowSet;
				if (!this.index[columnValue.Key].TryGetValue(columnValue.Value, out rowSet))
				{
					// unknown column value, no need to continue as no rows can match.
					matchingRowSets = null;
					return false;
				}

				rowSets.Add(rowSet);
			}

			matchingRowSets = rowSets;
			return true;
		}

		/// <summary>
		/// Gets the rows that are contained in all sets.
		/// </summary>
		/// <param name="rowSets">The row sets.</param>
		/// <returns>The rows contained in all sets.</returns>
		private static IEnumerable<TRow> GetRowsContainedInAllSets(IList<RowSet> rowSets)
		{
			foreach (var row in rowSets[0])
			{
				var rowInAllSets = true;
				for (var a = 1; a < rowSets.Count; a++)
				{
					if (!rowSets[a].Contains(row))
					{
						rowInAllSets = false;
						break;
					}
				}

				if (rowInAllSets)
				{
					// matching row found
					yield return row;
				}
			}
		}

		/// <summary>
		/// Row set count comparer sorting rows sets by their number of rows.
		/// </summary>
		/// <param name="set1">The set1.</param>
		/// <param name="set2">The set2.</param>
		/// <returns>The result.</returns>
		private static int RowSetCountComparer(RowSet set1, RowSet set2)
		{
			if (set1.Count < set2.Count)
			{
				return -1;
			}

			if (set1.Count > set2.Count)
			{
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// A dictionary holding a set of rows per each column value.
		/// </summary>
		private class RowsPerValueDictionary : Dictionary<TValue, RowSet>
		{
			/// <summary>
			/// Adds the specified value.
			/// </summary>
			/// <param name="value">The value.</param>
			/// <param name="row">The row.</param>
			public void Add(TValue value, TRow row)
			{
				RowSet rows;
				if (!this.TryGetValue(value, out rows))
				{
					this[value] = rows = new RowSet();
				}

				rows.Add(row);
			}
		}

		/// <summary>
		/// A set of rows.
		/// </summary>
		private class RowSet : HashSet<TRow>
		{
		}
	}
}