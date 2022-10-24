# Content Cloud Endpoints

This library allows editors to create URLs to a Content Cloud instance that return arbitrary content data, either in JSON or HTML.

Using the default implementation, editors can specify a query in a SQL-like syntax. This will execute against the repository and return JSON. Optionally, they can provide a Liquid template. This will be applied to the results of the query, and the endpoint will return an HTML fragment.

## Query Syntax

The query processing is abstracted to a service called `IQueryProcessor`. It skmply takes in a string representing a query and returns an object of data.

Key principle: _a string turns into data_. How this actually happens is immaterial. Writing a new query processor and plugging it in shouldn't be hard.

(TODO: Set it up as a DI'd service. I was lazy and just wrote it as a dynamic function.)

In the default implementation, a query syntax called "TreeQL" is used. This is a SQL-like syntax for querying trees of data. (I originally wrote this for something other than Opti. The basic structure of a tree-based query is pretty universal.) It's built on a parsing library called Parlot. Parsing a string of TreeQL returns a TreeQuery object. The `TreeQLProcessor` then does some (really, really inefficient) LINQ to retrieve content.

Basic format:

```
SELECT [scope] of [target]
  WHERE [field] [=|<>|contains] [value] [and|or] [field] [=|<>|contains] [value]
  ORDER BY [value] [direction]
  SKIP [#]
  LIMIT [#]
```

At the moment, the implementation is limited. The only fields that can be ordered on are `date` (`IVersionable.StartPublish`) and `name`. Over time, I'll expand support for all the features of the query object. 

Example:

```
SELECT children OF /blog/
  WHERE name CONTAINS "bears"
  ORDER BY date DESC
  LIMIT 3
```

That will return the latest three blog posts with the string "bears" in the title, order reverse chronologically.

(TODO: I want to provide other `IQueryProcessors`, like GraphQL. All a query processor needs to do is return data from a string.)

(MEANING: Don't @ me with stuff like, "Yeah, but aren't we using GraphQL..." _I get it._ But, as I noted above, a query is just a service that returns data from a string. How you do that is up to you. If you want GraphQL, then write an `IQueryProcessor` for it.)

(Maybe someday, I will write `ISamuelLJacksonQueryProcessor`, which with you can write queries like: `gimme my muthaf*cking content from that muthaf*cking repository!!!` I might be joking, but again with the core point: _a string returns data_. Don't over-think this.)

## Content Labels

To avoid the vagaries involved with paths, a `ContentLabel` string property can be added to your model. This can be used in queries to identify a target regardless of location:

```
SELECT children OF label:BLOGHOME
```

That will return the children of whatever content contains `BLOGHOME` as the value of its `ContentLabel` property, wherever it's located.

(The code that returns the content label is pluggable. There's a static `Func<IContent,string>` on `TreeQLProcessor`. Return `NULL` if content doesn't have a label.)

(TODO: In a perfect world, you would just search for the label, but, again, the default implementation assumes you don't have Find, so it's wildly inefficient.)

## Output Formatting

If no Liquid template is provided, the results of the query will be serialized into JSON and returned with a content type of `application/json`.

(TODO: This serialization architecture changed in CMS12, and I can't figure it out. There's a stub method in there to manually serialize. I will replace this when I get some clarification on how to do it properly in CMS12.)

If a Liquid template is provided, it will be executed against the data returned by the query. That data will be available in the `Model` identifier.

For example, to format a list of our blog posts from above:

```
<ul>
{% for post in Model %}
  <li><a href="{{ post | url }}">{{ post.Name }}</a></li>
{% endfor %}
</ul>
```

This HTML fragment will be returned with a content type of `text/html`.

## To Create an Endpoint

Endpoints are currently pages. This made sense because they need a URL.

(TODO: I can conceive of a way to make them blocks you can store as headless content, perhaps with some neat routing goodness allowing for variables and such, but that will come later.)

1. Create a a `Content Endpoint` page somewhere in the tree. By default, this will suppress from showing in navigation.
2. Enter a query
3. Optionally enter a template
4. Publish

Your data -- either JSON or HTML fragment -- will be available at the page's URL.

## Using HTML Fragments

These are designed to be implemented by [HDA](https://htmx.org/essays/hypermedia-driven-applications/) libraries.

For example, if wanted to embed our list of blog posts on another site, loading them dynamically when the page loads, we could use [HTMX](https://htmx.org/) like this:

```html
<div hx-get="http://domain.com/path/to/endpoint" hx-trigger="load"></div>
```

When the page loads, our endpoint will be called, returning our HTML fragment (an unordered list, in the example above), which will load as the `innerHTML` of the `DIV`
