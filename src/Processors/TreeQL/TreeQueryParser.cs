using EPiServer.Cms.Shell.UI.Reports.Internal.NotPublishedPages;
using Org.BouncyCastle.Asn1.X509;
using Parlot;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using static Parlot.Fluent.Parsers;

namespace DeaneBarker.Optimizely.Endpoints.TreeQL
{
    public static class TreeQueryParser
    {

        public static Func<TextSpan, bool> TargetValidator = (t) => { return true; }; // Validate anything by default
        public static string TargetValidatorError = "Target must (1) begin and end with a forward slash, (2) be an integer, or (3) start with \"@\"";

        public static string[] AllowedOperators = new[] { "=", "!=", ">", ">=", "<", "<=" };

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
            var and = Terms.Text("and");
            var or = Terms.Text("or");
            var select = Terms.Text("select");


            // Target
            var children = Terms.Text("children");
            var results = Terms.Text("results");
            var parent = Terms.Text("parent");
            var descendants = Terms.Text("descendants");
            var ancestors = Terms.Text("ancestors");
            var self = Terms.Text("self");
            var siblings = Terms.Text("siblings");
            var inclusive = Terms.Text("inclusive");
            var exclusive = Terms.Text("exclusive");

            var scope = ZeroOrOne(OneOf(results, parent, children, descendants, self, ancestors, siblings).ElseError("Expected scope")).Then(v =>
            {
                return v ?? "self";
            });
            var path = Terms.NonWhiteSpace().When(t => TargetValidator(t));
            var target = 
                scope // Item1
                .AndSkip(of)
                .And(path) // Item2
                .And(ZeroOrOne(OneOf(inclusive, exclusive))) // Item3
                .Then(v =>
            {
                return new Target()
                {
                    Scope = v.Item1,
                    Path = v.Item2.ToString(),
                    Inclusive = v.Item3 == "inclusive"
                };
            });



            // Where clause
            var conjunction = OneOf(and, or);
            var where = Terms.Text("where");
            var fieldName = Terms.NonWhiteSpace();
            var contains = Terms.Text("contains");
            var lessThan = Terms.Text("<");
            var equals = Terms.Text("=");
            var greaterThan = Terms.Text(">");
            var notEqualTo = Terms.Text("!=");
            var value = Terms.String(StringLiteralQuotes.SingleOrDouble);
            var whereClause = ZeroOrOne(conjunction) // Item1
                .And(fieldName) // Item2
                //.And(Terms.NonWhiteSpace().When(t => AllowedOperators.Contains(t.ToString().Trim())).ElseError($"Allowed operators: {string.Join(", ", AllowedOperators)}")) // Item3
                .And(OneOf(contains, lessThan, equals, greaterThan, notEqualTo))
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



            // Full Command
            parser =
                select.ElseError("Expected \"select\"")
                .And(Separated(and, target)).ElseError(TargetValidatorError) // Item 1
                .AndSkip(ZeroOrOne(where))
                .And(ZeroOrMany(whereClause)) // Item 2
                .AndSkip(ZeroOrOne(sortSeparator))
                .And(ZeroOrOne(Separated(Literals.Char(','), sortValue))) // Item 3
                .And(ZeroOrOne(limit.SkipAnd(number))) // Item 4
                .Then(v =>
                {
                    var query = new TreeQuery()
                    {
                        Targets = v.Item2,
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
