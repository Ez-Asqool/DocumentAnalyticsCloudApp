namespace DocumentAnalyticsCloudApp.Services
{
    /*
     * DocumentClassifierService is a rule-based classifier that analyzes the content of a document and assigns it to a predefined category path within a multi-level classification tree.
     * 
     * -- Developed By: Ez-Aldeen Asqool (E.A Developer) --
     */
    public class DocumentClassifierService
    {
        private readonly Dictionary<string, List<string>> _rules;

        public DocumentClassifierService()
        {
            _rules = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // * For Artificial Intelligence *
                { "Computer Science > AI > NLP", new() { "neural", "language model", "tokenizer", "transformer", "bert", "gpt" } },
                { "Computer Science > AI > Machine Learning", new() { "regression", "classifier", "training", "dataset", "scikit-learn", "random forest", "gradient boosting" } },
                { "Computer Science > AI > Computer Vision", new() { "image", "opencv", "cnn", "detection", "segmentation", "YOLO", "resnet" } },

                // * For Web Development *
                { "Computer Science > Web > Frontend", new() { "html", "css", "javascript", "react", "vue", "tailwind", "bootstrap" } },
                { "Computer Science > Web > Backend", new() { "asp.net", "node.js", "django", "laravel", "api", "mvc", "controller" } },
                { "Computer Science > Web > Full Stack", new() { "full stack", "frontend", "backend", "rest", "json", "authentication" } },

                // * For Cloud and Distributed Systems *
                { "Computer Science > Cloud > Azure", new() { "azure", "blob storage", "function app", "resource group", "app service" } },
                { "Computer Science > Cloud > AWS", new() { "aws", "s3", "ec2", "lambda", "dynamodb" } },
                { "Computer Science > Cloud > Distributed Systems", new() { "distributed", "latency", "replication", "scalability", "consistency" } },

                // * For Data Science *
                { "Computer Science > Data Science > Analytics", new() { "data analysis", "pandas", "matplotlib", "notebook", "jupyter" } },
                { "Computer Science > Data Science > Big Data", new() { "hadoop", "spark", "big data", "data lake", "hive", "mapreduce" } },

                // * For Cybersecurity *
                { "Computer Science > Security > Network Security", new() { "firewall", "ddos", "intrusion detection", "vpn", "packet" } },
                { "Computer Science > Security > Cryptography", new() { "encryption", "rsa", "aes", "public key", "digital signature" } },

                // * For Databases *
                { "Computer Science > Databases > SQL", new() { "sql", "select", "join", "stored procedure", "query", "index" } },
                { "Computer Science > Databases > NoSQL", new() { "mongodb", "nosql", "document store", "collection", "shard" } },

                // * For Operating Systems *
                { "Computer Science > Systems > Operating Systems", new() { "kernel", "process", "thread", "scheduling", "memory management", "semaphore" } },

                // * For Networking *
                { "Computer Science > Networking > Protocols", new() { "http", "tcp", "ip", "udp", "dns", "routing", "packet" } },

                // * For Software Engineering *
                { "Computer Science > Software Engineering > Methodologies", new() { "agile", "scrum", "kanban", "sprint", "requirements", "user stories" } },
                { "Computer Science > Software Engineering > Testing", new() { "unit test", "integration test", "tdd", "mock", "xunit", "jest" } }
            };
        }
        public string Classify(string content)
        {
            var scores = new Dictionary<string, int>();

            foreach (var rule in _rules)
            {
                int count = rule.Value.Count(keyword =>
                    content.Contains(keyword, StringComparison.OrdinalIgnoreCase));

                if (count > 0)
                    scores[rule.Key] = count;
            }

            if (scores.Count == 0)
                return "Unclassified";

            var bestMatch = scores.OrderByDescending(kvp => kvp.Value).First();
            return bestMatch.Key;
        }
        /*
        public string Classify(string content)
        {
            foreach (var rule in _rules)
            {
                foreach (var keyword in rule.Value)
                {
                    if (content.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                        return rule.Key;
                }
            }

            return "Unclassified";
        }
        */
    }
}
