using System.Linq.Expressions;
using FilterTypes;
using Microsoft.EntityFrameworkCore;

namespace FilterDataAccess
{
	/// <summary>
	/// Retrieves values from the database, based on a set of filter operations.
	/// </summary>
	/// <remarks>
	/// To create scaffolding, call the following command in Package Manager Console:
	/// <![CDATA[
	/// Scaffold-DbContext -Provider Microsoft.EntityFrameworkCore.SqlServer -Connection name=LoggingConnection
	/// ]]>
	/// </remarks>
	public class DataAccessService<TValue, TDatabaseContext> where TDatabaseContext:DbContext
	{
		/// <summary>
		/// Creates a new data access service.
		/// </summary>
		/// <param name="databaseContextFactory">Database context used to read <see cref="TValue"/> entries from the database.</param>
		/// <param name="getQueryable">Delegate returning a queryable interface from the database context.</param>
		/// <param name="sortQueryable">Delegate used to sort the queryable values, using the label and sort direction.</param>
		/// <exception cref="ArgumentNullException">Thrown if any of the required arguments are null.</exception>
		public DataAccessService(IDbContextFactory<TDatabaseContext> databaseContextFactory, GetQueryableDelegate getQueryable, SortQueryableDelegate sortQueryable)
		{
			DatabaseContextFactory	= databaseContextFactory ?? throw new ArgumentNullException(nameof(databaseContextFactory));
			GetQueryable			= getQueryable ?? throw new ArgumentNullException(nameof(getQueryable));
			SortQueryable			= sortQueryable ?? throw new ArgumentNullException(nameof(sortQueryable));
		}

		#region Types
		/// <summary>
		/// Delegate used to retrieve the queryable interface from the database context.
		/// </summary>
		/// <param name="databaseContext">Database context containing the queryable values.</param>
		/// <returns>Queryable interface, returning values.</returns>
		public delegate IQueryable<TValue> GetQueryableDelegate(TDatabaseContext databaseContext);

		/// <summary>
		/// Delegate used to sort the queryable values, using the label and sort direction.
		/// </summary>
		/// <param name="values">Values to sort.</param>
		/// <param name="sortLabel">Name of the field to sort by.</param>
		/// <param name="sortAscending">Indicates if results should be sorted in ascending order (true) or descending order (false).</param>
		public delegate IOrderedQueryable<TValue> SortQueryableDelegate(IQueryable<TValue> values, string sortLabel, bool sortAscending=true);
		#endregion

		#region Methods
		/// <summary>
		/// Retrieve a filtered list of values.
		/// </summary>
		/// <param name="filter">Filter operations used to filter the list of values.</param>
		/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
		/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
		/// <param name="skip">Number of items to skip, in the query result. This is used for paging through a large result set.</param>
		/// <param name="maxCount">Maximum number of results to retrieve.</param>
		/// <returns>
		/// Query result containing total number of available results, matching the filter, and a list of results not 
		/// exceeding the number of results specified in <paramref name="maxCount"/>.
		/// </returns>
		/// <exception cref="ArgumentException">Thrown if the <paramref name="filter"/> contains invalid arguments.</exception>
		public Task<QueryResult<TValue>> GetFilteredValues(FilterOperation[] filter, string sortLabel="Id", bool sortAscending=true, int skip = 0, int maxCount=100, CancellationToken cancellationToken=default)
		{
			ExpressionGenerator<TValue> expressionGenerator = new ExpressionGenerator<TValue>();

			var filterExpressions = filter.Select(filterPart => expressionGenerator.GenerateFilterExpression(filterPart));

			return GetFilteredValues(filterExpressions, sortLabel, sortAscending, skip, maxCount);
		}

		/// <summary>
		/// Retrieve a filtered list of values.
		/// </summary>
		/// <param name="filter">Filter expressions used to filter the list of values.</param>
		/// <param name="sortLabel">Name of the property that the query should be ordered in.</param>
		/// <param name="sortAscending">Indicates if sorting is ascending (Otherwise it will be descending).</param>
		/// <param name="skip">Number of items to skip, in the query result. This is used for paging through a large result set.</param>
		/// <param name="maxCount">Maximum number of results to retrieve.</param>
		/// <returns>
		/// Query result containing total number of available results, matching the filter, and a list of results not 
		/// exceeding the number of results specified in <paramref name="maxCount"/>.
		/// </returns>
		public async Task<QueryResult<TValue>> GetFilteredValues(IEnumerable<Expression<Func<TValue, bool>>> filter, string sortLabel="Id", bool sortAscending=true, int skip = 0, int maxCount=100, CancellationToken cancellationToken=default)
		{
			using(TDatabaseContext databaseContext = await DatabaseContextFactory.CreateDbContextAsync(cancellationToken))
			{
				System.Diagnostics.Trace.WriteLine("Started GetFilteredValues.");

				IQueryable<TValue>	queryableValues = GetQueryable(databaseContext);
				QueryResult<TValue>	queryResult		= new QueryResult<TValue>();

				try
				{

					// Apply filter expressions.
					foreach(Expression<Func<TValue, bool>>? expression in filter)
						queryableValues = queryableValues.Where(expression);

					// Sort data by the sorting label.
					queryableValues = SortQueryable(queryableValues, sortLabel, sortAscending);

					// Determine how many results match the specified filter.
					int totalCount = await queryableValues.CountAsync(cancellationToken);

					// Indicate the total number of results available.
					queryResult.TotalCount = totalCount;

					if(skip < 0)
						skip = 0;

					// Determine the maximum number of results to retrieve.
					// This is done to prevent the SQL Server from hanging forever, if requesting more results than exist.
					int maxTake = Math.Min(totalCount-skip, maxCount);

					// If there are any results and the current page contains results.
					if(totalCount > 0 && maxTake > 0)
					{
						// Retrieve a subset of data.
						queryResult.Results = await queryableValues.Skip(skip).Take(maxTake).ToArrayAsync(cancellationToken);
					}
					else
					{ 
						queryResult.Results = new TValue[0];
					}
				}
				catch(OperationCanceledException)
				{ 
					System.Diagnostics.Trace.WriteLine($"Canceled retrieving filtered values.");

					queryResult.TotalCount	= 0;
					queryResult.Results	= new TValue[0];
				}
				catch(Exception exception)
				{ 
					System.Diagnostics.Trace.WriteLine($"Failed to retrieve filtered values: {exception.ToString()}.");

					queryResult.TotalCount	= 0;
					queryResult.Results	= new TValue[0];
				}

				System.Diagnostics.Trace.WriteLine("Finished GetFilteredValues.");

				return queryResult;
			}
		}
		#endregion

		#region Properties
		/// Factory used to create the DatabaseContext, which is used to access the database.
		/// </summary>
		protected IDbContextFactory<TDatabaseContext> DatabaseContextFactory
		{
			get;
		}

		/// <summary>
		/// Delegate returning a queryable interface from the database context.
		/// </summary>
		protected GetQueryableDelegate GetQueryable
		{
			get;
		}

		/// <summary>
		/// Delegate used to sort the queryable values, using the label and sort direction.
		/// </summary>
		protected SortQueryableDelegate SortQueryable
		{
			get;
		}
		#endregion
	}
}
