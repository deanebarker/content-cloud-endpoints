using EPiServer.Cms.Shell.UI.Reports.Internal.NotPublishedPages;
using Org.BouncyCastle.Asn1.X509;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using static Parlot.Fluent.Parsers;

namespace DeaneBarker.Optimizely.Endpoints.TreeQL
{
    public static class TreeQueryParser
    {

        private static string commentPrefix = "#";

        private static Parser<TreeQuery> parser;

        public static TreeQuery Parse(string q, object data)
        {
            foreach (var property in data.GetType().GetProperties())
            {
                var value = property.GetValue(data, null);
                q = q.Replace(string.Concat("@", property.Name), value.ToString());
            }

            return Parse(q);
        }

        public static TreeQuery Parse(string q)
        {
            q = Clean(q);
            var query = parser.Parse(q);
            query.Source = q;
            return query;
        }

        private static string Clean(string input)
        {
            var lines = input.Split(new string[] { "\n", "\r\n", Environment.NewLine }, StringSplitOptions.None).AsQueryable();

            lines = lines
                .Where(l => !l.Trim().StartsWith(commentPrefix))
                .Select(s => s.Trim());

            return string.Join(" ", lines).ToLower().Trim(); 
        }

        static TreeQueryParser()
        {
            var of = Terms.Text("of");
            var select = Terms.Text("select");

            // Scope
            var children = Terms.Text("children");
            var parent = Terms.Text("parent");
            var descendants = Terms.Text("descendants");
            var ancestors = Terms.Text("ancestors");
            var self = Terms.Text("self");
            var siblings = Terms.Text("siblings");
            var scope = ZeroOrOne(OneOf(parent, children, descendants, self, ancestors, siblings).ElseError("Expected scope")).Then(v =>
            {
                return v ?? "self";
            });

            // Target	
            var path = Terms.NonWhiteSpace().When(t => t.ToString().StartsWith("/") && t.ToString().EndsWith("/") || t.ToString().StartsWith("label"));
            var inclusive = Terms.Text("inclusive");
            var exclusive = Terms.Text("exclusive");
            var target = path.And(ZeroOrOne(OneOf(inclusive, exclusive))).Then(v =>
            {
                return new Target()
                {
                    Path = v.Item1.ToString(),
                    Inclusive = v.Item2 == "inclusive"
                };
            });

            // Sort Separator
            var order = Terms.Text("order");
            var by = Terms.Text("by");
            var sortSeparator = order.And(by);

            // Sort Value
            var ascending = Terms.Text("asc");
            var descending = Terms.Text("desc");
            var sortDirection = OneOf(ascending, descending);
            var sortValue = SkipWhiteSpace(Literals.Pattern(c => char.IsLetterOrDigit(c) || c == ':')).And(ZeroOrOne(OneOf(Terms.Text("asc"), Terms.Text("desc")))).Then(v =>
            {
                return new Sort()
                {
                    Value = v.Item1.ToString(),
                    Direction = v.Item2?.ToString() == "desc" ? SortDirection.Descending : SortDirection.Ascending
                };
            });

            // Limit value
            var limit = Terms.Text("limit");
            var number = Terms.Integer();

            // Where clause
            var where = Terms.Text("where");
            var and = Terms.Text("and");
            var or = Terms.Text("or");
            var conjunction = OneOf(and, or);
            var fieldName = Terms.NonWhiteSpace();
            var contains = Terms.Text("contains");
            var lessThan = Terms.Text("<");
            var equals = Terms.Text("=");
            var greaterThan = Terms.Text(">");
            var notEqualTo = Terms.Text("!=");
            var value = Terms.String(StringLiteralQuotes.SingleOrDouble);
            var whereClause = ZeroOrOne(conjunction) // Item1
                .And(fieldName) // Item2
                .And(OneOf(contains, lessThan, equals, greaterThan, notEqualTo)) // Item3
                .And(value) // Item4
                .Then(v =>
                {
                    var fieldName = v.Item2.ToString().Split(':').First();
                    var type = v.Item2.ToString().Contains(":") ? v.Item2.ToString().Split(':').Last() : "string";

                    return new Filter()
                    {
                        Conjunction = v.Item1,
                        FieldName = fieldName,
                        Type = type,
                        Operator = v.Item3.ToString(),
                        Value = v.Item4.ToString()
                    };
                });

            // Full Command
            parser =
                select.ElseError("Expected \"select\"")
                .SkipAnd(scope) // Item 1
                .AndSkip(of).ElseError("Expected \"of\"")
                .And(target.ElseError("Expected a target. Target must begin and end with a forward slash: \"/\"")) // Item 2
                .AndSkip(ZeroOrOne(where))
                .And(ZeroOrMany(whereClause)) // Item 3
                .AndSkip(ZeroOrOne(sortSeparator))
                .And(ZeroOrOne(Separated(Literals.Char(','), sortValue))) // Item 4
                .And(ZeroOrOne(limit.SkipAnd(number))) // Item 5
                .Then(v =>
                {
                    var query = new TreeQuery()
                    {
                        Scope = v.Item1,
                        Target = v.Item2,
                        Sort = v.Item4 ?? new List<Sort>(),
                        Limit = Convert.ToInt32(v.Item5.ToString())
                    };

                    foreach (var item in v.Item3)
                    {
                        query.Filters.Add(item);
                    }

                    return query;

                });


        }
    }
}
