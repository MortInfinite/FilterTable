using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace FilterTableExample.Data
{
	/// <summary>
	/// Provides access to retrieve <see cref="MyDataType"/> entries from the database.
	/// </summary>
	/// <remarks>
	/// To create the database, call the following command in Package Manager Console:
	/// <![CDATA[
	/// Update-Database
	/// ]]>
	/// </remarks>
	public class MyDataTypeService
	{
		/// <summary>
		/// Create a new MyDataTypeService.
		/// </summary>
		/// <param name="myDataTypeContextFactory">Database context used to access data from the database.</param>
		/// <exception cref="ArgumentNullException">Thrown if the <paramref name="myDataTypeContextFactory"/> is null.</exception>
		public MyDataTypeService(IDbContextFactory<MyDataTypeContext> myDataTypeContextFactory)
		{
			MyDataTypeContextFactory = myDataTypeContextFactory ?? throw new ArgumentNullException(nameof(myDataTypeContextFactory));
		}

		/// <summary>
		/// Query result containing a result set and a count of the total number of results available, that match a filter criteria.
		/// </summary>
		public struct QueryResult
		{ 
			/// <summary>
			/// Results matching the filter criteria.
			/// </summary>
			public IList<MyDataType> Items;

			/// <summary>
			/// Total number of results available.
			/// </summary>
			public int TotalCount;
		}

		/// <summary>
		/// Retrieve a filtered list of <see cref="MyDataType"/> items.
		/// </summary>
		/// <param name="filter">Filter expressions used to filter the list of <see cref="MyDataType"/> items.</param>
		/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
		/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
		/// <param name="skip">Number of items to skip, in the query result. This is used for paging through a large result set.</param>
		/// <param name="maxCount">Maximum number of results to retrieve.</param>
		/// <returns>
		/// Query result containing total number of available results, matching the filter, and a list of results not 
		/// exceeding the number of results specified in <paramref name="maxCount"/>.
		/// </returns>
		public async Task<QueryResult> GetData(IEnumerable<Expression<Func<MyDataType, bool>>> filter, string sortLabel="Id", bool sortAscending=true, int skip = 0, int maxCount=100, CancellationToken cancellationToken=default)
		{
			using(MyDataTypeContext myDataTypeContext = await MyDataTypeContextFactory.CreateDbContextAsync(cancellationToken))
			{
				IQueryable<MyDataType>  myDataTypes = myDataTypeContext.MyDataTypes;
				QueryResult				queryResult = new QueryResult();

				try
				{

					// Apply filter expressions.
					foreach(Expression<Func<MyDataType, bool>>? expression in filter)
						myDataTypes = myDataTypes.Where(expression);

					// Sort data by the sorting label.
					myDataTypes = myDataTypes.SortData(sortLabel, sortAscending);

					// Determine how many MyDataType items match the specified filter.
					int totalCount = await myDataTypes.CountAsync(cancellationToken);

					// Indicate the total number of results available.
					queryResult.TotalCount = totalCount;

					if(skip < 0)
						skip = 0;

					// Determine the maximum number of results to retrieve.
					// This is done to prevent the SQL Server from hanging forever, if requesting more results than exist.
					int maxTake = Math.Min(totalCount-skip, maxCount);

					// If there are MyDataType items and the current page contains MyDataType items.
					if(totalCount > 0 && maxTake > 0)
					{
						// Retrieve a subset of data.
						queryResult.Items = await myDataTypes.Skip(skip).Take(maxTake).ToArrayAsync(cancellationToken);
					}
					else
					{ 
						queryResult.Items = new MyDataType[0];
					}
				}
				catch
				{ 
					queryResult.TotalCount	= 0;
					queryResult.Items	= new MyDataType[0];
				}

				return queryResult;
			}
		}

		/// <summary>
		/// Factory used to create the MyDataTypeContext, which is used to access the database.
		/// </summary>
		protected IDbContextFactory<MyDataTypeContext> MyDataTypeContextFactory
		{
			get;
		}
	}
}
