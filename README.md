# Content Cloud Endpoints

This library allows editors to create URLs to a Content Cloud instance that return content data, either in JSON or HTML.

At the default implementation, editors can specify a query in a SQL-like syntax. This will execute against the repository and return JSON. Optionally, they can provide a Liquid template. This will be applied to the results of the query, and return an HTML fragment.
