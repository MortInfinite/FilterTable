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

function removeFiltersFromTable(html)
{
	var element = htmlToElement(html);

	var filterElements = element.querySelectorAll('div.DataFilter')
	for (var count=0; count<filterElements.length; count++) 
	{
		var filterElement = filterElements[count];
		var parent = filterElement.parentElement;
		parent.removeChild(filterElement);
	}

	return element.outerHTML;
}

function htmlToElement(html) 
{
    var template = document.createElement('template');
    html = html.trim();
    template.innerHTML = html;
    return template.content.firstChild;
}

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
