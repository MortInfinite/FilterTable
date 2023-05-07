# FilterTable
A filtered table component, using Blazor Server. 

The filtered table component will generate a LINQ statement, based on the filter criteria selected by the user, and will show the filtered result in a pageable and sortable table. 

The contents of the table (or selected rows), can be copied to the clipboard, in an HTML format that is suited to be pasted into Microsoft Excel.

The component depends on the MudBlazor library to render the table.

This project uses .NET 6.

# FilterTableExample
The FilterTableExample project shows how to use the FilterTable to filter a set of values, which are retrieved from a database using Entity Framework.

This example project contains Entity Framework migration code that which can generate an SQL Server LocalDB database and populate it with a few sample entries, used by the example project.
