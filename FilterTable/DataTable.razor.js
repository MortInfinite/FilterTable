// Copy table contents to clipboard.
function copyTable() 
{
	return new Promise((resolve, reject) => 
	{
		// Find the table element.
		var element = document.querySelector('.mud-table-root').outerHTML;
		
		// Remove the filters from the table.
		element = removeFiltersFromTable(element);

		try
		{
			// Firefox currently disables the ClipboardItem feature, by default.
			// To enable it in Firefox, go to about:Config and enable the dom.events.asyncClipboard.clipboardItem flag.
			const type = 'text/html';
			const spreadSheetRow = new Blob([element], {type});
			const clipboardItem = new ClipboardItem({[type]: spreadSheetRow})

			navigator.clipboard.write([clipboardItem]).then(() => 
			{
				resolve();
			}, (err) => 
			{
				reject(err);
			});
		}
		catch
		{
			// If called from Firefox, fall back to the old clipboard API.
			navigator.clipboard.writeText(element);
			resolve();
		}
	});
}

// Copy selected rows to clipboard.
function copySelectedRows() 
{
	return new Promise((resolve, reject) => 
	{
		// Find the table element.
		var element = document.querySelector('.mud-table-root').outerHTML;
		
		// Remove the filters from the table.
		element = removeFiltersFromTable(element);

		// Remove unselected rows from the table.
		element = removeUnselectedRowsFromTable(element);

		try
		{
			const type = 'text/html';
			const spreadSheetRow = new Blob([element], {type});
			const clipboardItem = new ClipboardItem({[type]: spreadSheetRow})

			navigator.clipboard.write([clipboardItem]).then(() => 
			{
				resolve();
			}, (err) => 
			{
				reject(err);
			});
		}
		catch
		{
			// If called from Firefox, fall back to the old clipboard API.
			navigator.clipboard.writeText(element);
			resolve();
		}
	});
}

// Remove data-filter elements from the specified HTML.
function removeFiltersFromTable(html)
{
	var element = htmlToElement(html);

	var filterElements = element.querySelectorAll('div.data-filter')
	for (var count=0; count<filterElements.length; count++) 
	{
		var filterElement = filterElements[count];
		var parent = filterElement.parentElement;
		parent.removeChild(filterElement);
	}

	return element.outerHTML;
}

// Convert specified HTML to an element.
function htmlToElement(html) 
{
    var template = document.createElement('template');
    html = html.trim();
    template.innerHTML = html;
    return template.content.firstChild;
}

// Remove rows that aren't selected, from the specified HTML.
function removeUnselectedRowsFromTable(html)
{
	var element = htmlToElement(html);

	var filterElements = element.querySelectorAll('tbody > tr:not(.selected)')
	for (var count=0; count<filterElements.length; count++) 
	{
		var filterElement = filterElements[count];
		var parent = filterElement.parentElement;
		parent.removeChild(filterElement);
	}

	return element.outerHTML;
}

// Clear the selected text, using one of the methods that the used web browser supports.
function clearSelectedText()
{
	try
	{
		// If the window has a getSelection method.
		if(window.getSelection)
		{
			// Get the selection object.
			var selection = window.getSelection();

			// If the window contains a getSelection method, use that method to clear selection.
			if(selection.empty)
				selection.empty();
			// If the window contains a removeAllRanges method, use that method to clear selection.
			else if(selection.removeAllRanges)
				selection.removeAllRanges();
		}
		// If the window has a selection property.
		else if(document.selection)
		{
			document.selection.empty();
		}
	}
	catch
	{
		// Don't cause an error if selection can't be cleared.
	}
}