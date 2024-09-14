# FilterTable
Contains a DataTable component, which provides filtering capabilities for a set of data.

The filter parameters are passed to delegate, which is responsible for loading the filtered set of data. 

The component depends on the MudBlazor library to render the table.

# FilterDataAccess
Containts a DataAccessService class, which can be used as a base class for a data access service, which will receive a set of filter parameters from the FilterTable and perform the filtering operations on an Entity Framework DataContext.

# FilterTypes
Defines the filtering primitives used by the FilterTable and FilterDataAccess projects.

# BlazorLogViewer
Example project, which demonstrates how to use the DataTable to retrieve filtered log data from a web service. The LogEntriesTable Page demonstrates how to use the FilterTable component and the LogEntryService demonstrates how to retrieve a set of filtered data from a web service.

# BlazorLogViewer.Server
Blazor Server implementation of the BlazorLogViewer project.

# BlazorLogViewer.WASM
Blazor WASM implementation of the BlazorLogViewer project.

# LogDataAccess
Example project, which demonstrates how to use the DataAccessService class, to filter data from an Entity Framework DataContext and wraps this functionality in a LogEntryController web service.

# LogData
Defines the LogEntry example data type, that is returned by the LogDataAccess project and shown by the BlazorLogViewer projects.

This project is implemented in .NET 8.
