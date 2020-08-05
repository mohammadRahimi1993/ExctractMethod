using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using System.Text.RegularExpressions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractMrthod
{
    class Tools
    {
        public static ControlFlowGraph MControlFlowGraphCompilation(string Code)
        {
            CSharpParseOptions options = CSharpParseOptions.Default.WithFeatures(new[] { new KeyValuePair<string, string>("flow-analysis", "") });
            var tree = CSharpSyntaxTree.ParseText(Code, options);
            var compilation = CSharpCompilation.Create("c", new[] { tree });
            var model = compilation.GetSemanticModel(tree, ignoreAccessibility: true);

            var methodBodySyntax = tree.GetCompilationUnitRoot().DescendantNodes().OfType<BaseMethodDeclarationSyntax>().Last();

            var cfgFromSyntax = ControlFlowGraph.Create(methodBodySyntax, model);

            return cfgFromSyntax;

            //foreach (var Block in cfgFromSyntax.Blocks)
            //{

            //    foreach (var operation in Block.Operations)
            //    {
            //        var syntax = operation.Syntax;
            //        Console.WriteLine(syntax.ToString());
            //    }
            //    Console.WriteLine("");
            //}
        }

        public static Dictionary<int, Node> MControlFlowGraph(string Code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(Code);
            ControlFlowGraph cfg = MControlFlowGraphCompilation(Code);
            Dictionary<int, Node> nodes = new Dictionary<int, Node>();

            //initial Node
            foreach (var item in cfg.Blocks)
            {
                Node node = new Node();
                node.id = item.Ordinal;
                node.parentList = MFindPredecessore(item);  // Add Parent
                node.basicBlock = item;
                nodes.Add(node.id, node);
            }

            //Add Child
            foreach (var block in cfg.Blocks)
            {
                nodes[block.Ordinal].childList = new List<int>();
                foreach (var element in nodes)
                    if (element.Value.parentList.Contains(block.Ordinal))
                        nodes[block.Ordinal].childList.Add(element.Value.id);
            }

            //Add Line Number and statement
            var statementSyntaxes = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>().ToList();
            foreach (var block in cfg.Blocks)
            {
                nodes[block.Ordinal].statements = new List<Statement>();
                foreach (var item in block.Operations)
                {

                    Statement operation = new Statement();
                    StatementSyntax statement = statementSyntaxes.Where(x => x.FullSpan.Start <= item.Syntax.FullSpan.Start && x.FullSpan.End >= item.Syntax.FullSpan.End).Last();
                    operation.NumberLine = statement.SyntaxTree.GetLineSpan(statement.Span).StartLinePosition.Line;   //1:get line id 
                    TextSpan lineSpan = tree.GetText().Lines[operation.NumberLine].Span;
                    operation.statement = tree.GetText().ToString(lineSpan);                                          //2:get line text
                    nodes[block.Ordinal].statements.Add(operation);
                }

                if (block.BranchValue != null)        // add line example while,if , ...
                {

                    Statement operation = new Statement();
                    StatementSyntax statement = statementSyntaxes.Where(x => x.FullSpan.Start <= block.BranchValue.Syntax.FullSpan.Start && x.FullSpan.End >= block.BranchValue.Syntax.FullSpan.End).First();
                    int lineId = statement.SyntaxTree.GetLineSpan(block.BranchValue.Syntax.Span).StartLinePosition.Line;
                    operation.NumberLine = lineId;                                                                     //1:get line id 
                    TextSpan lineSpan = tree.GetText().Lines[lineId].Span;
                    operation.statement = tree.GetText().ToString(lineSpan);
                    nodes[block.Ordinal].statements.Add(operation);
                }
            }
            return nodes;
        }

        public static Dictionary<int, Node> MControlFlowGraphWithouthEdgeBack(string code)
        {
            Dictionary<int, Node> cfg = MControlFlowGraph(code);
            // List Of travel Node
            List<int> travelNode = new List<int>();
            foreach (var basicBlock in cfg)
            {
                foreach (var item in travelNode)        // check back edge in childList by travelNode
                    if (basicBlock.Value.childList.Exists(x => x == item)) //edge back is (A,B)
                    {
                        basicBlock.Value.childList.RemoveAll(x => x == item);           //1:node id:A.....child list(B)
                        cfg[item].parentList.RemoveAll(x => x == basicBlock.Value.id);   //2:node id:B.....parent list(A)
                    }
                travelNode.Add(basicBlock.Value.id);
            }
            return cfg;
        }

        public static void writeCFG(string Code)
        {
            Dictionary<int, Node> basicBlocks = new Dictionary<int, Node>();
            basicBlocks = MControlFlowGraph(Code);
            foreach (var block in basicBlocks)
            {
                Console.WriteLine("BB" + block.Value.basicBlock.Ordinal + ":");
                Console.Write("Parent is:");
                block.Value.parentList.ForEach(x => Console.Write(x + " , "));
                Console.WriteLine();
                Console.Write("Child is:");
                block.Value.childList.ForEach(x => Console.Write(x + " , "));
                Console.WriteLine(); Console.WriteLine();
                foreach (var stat in block.Value.statements)
                    Console.WriteLine(stat.NumberLine + "." + stat.statement.Replace("           ", " "));
                Console.WriteLine("...........................................");
            }
        }

        public static Dictionary<int, List<int>> MDominant(string Code)
        {
            var cfgFromSyntax = MControlFlowGraphCompilation(Code);
            List<int> NewDom = new List<int>();
            List<int> InitialDom = new List<int>();
            int CountBlock = cfgFromSyntax.Blocks.Count();
            //all node put in NodeDom Collection 
            foreach (var Block in cfgFromSyntax.Blocks)
            {
                InitialDom.Add(Block.Ordinal);
            }
            Dictionary<int, List<int>> DictionaryDom = new Dictionary<int, List<int>>();
            //For all node create Dominant
            foreach (var Block in cfgFromSyntax.Blocks)
            {
                List<int> ExtendDom = new List<int>();
                foreach (var item in InitialDom)
                    ExtendDom.Add(item);
                if (Block.Ordinal != 0)
                    DictionaryDom.Add(Block.Ordinal, ExtendDom);
                else
                    DictionaryDom.Add(Block.Ordinal, null);
            }

            bool change = true;

            List<int> ParentList = new List<int>();

            while (change == true)
            {
                foreach (var node in cfgFromSyntax.Blocks)
                {
                    NewDom.Add(node.Ordinal);                         //1
                    ParentList = MFindPredecessore(node);
                    if (ParentList.Count == 1)
                    {
                        InitialDom = DictionaryDom[ParentList.ElementAt(0)];  //2
                        if (InitialDom != null)
                            foreach (var item in InitialDom)
                                NewDom.Add(item);
                        else
                            NewDom.Add(0);                                     //2
                    }
                    else if (ParentList.Count > 1)
                    {
                        foreach (var item in ParentList)
                        {
                            List<int> FirstDom = new List<int>();
                            FirstDom = DictionaryDom[item];
                            if (FirstDom != null)
                                InitialDom = FirstDom.Intersect(InitialDom).ToList();
                        }
                        foreach (var item in InitialDom)
                            NewDom.Add(item);                                  //2
                    }
                    InitialDom = DictionaryDom[node.Ordinal];
                    if (NewDom != InitialDom)
                        change = true;
                    if (DictionaryDom.ContainsKey(node.Ordinal))
                    {
                        List<int> DOList = DictionaryDom[node.Ordinal];
                        if (DOList != null)
                        {
                            DOList.Clear();
                            foreach (var item in NewDom)
                                DOList.Add(item);
                        }
                    }
                    NewDom.Clear();
                }
                change = false;
            }

            return DictionaryDom;


        }

        public static void MWriteDominant(string Code)
        {
            Dictionary<int, List<int>> Dominant = MDominant(Code);
            foreach (var item in Dominant)
            {
                Console.Write(" Dominant collection from BB" + item.Key + "  is: ");
                if (item.Value != null)
                    item.Value.ForEach(x => Console.Write(x + " and "));
                Console.WriteLine("");
            }
        }

        public static Dictionary<int, Node> MReverseControlFlowGraph(string Code)
        {
            var cfg = MControlFlowGraphCompilation(Code);
            Dictionary<int, Node> reverseControlFlowGraph = new Dictionary<int, Node>();
            foreach (var Basicblock in cfg.Blocks.Reverse())
            {
                //inital node and child
                Node node = new Node();
                node.id = Basicblock.Ordinal;
                node.childList = MFindPredecessore(Basicblock);
                node.basicBlock = Basicblock;
                node.parentList = new List<int>();
                reverseControlFlowGraph.Add(Basicblock.Ordinal, node);
            }

            //add  parent
            foreach (var item in reverseControlFlowGraph)
                foreach (var element in reverseControlFlowGraph)
                    if (element.Value.childList.Exists(x => x == item.Value.id))
                        item.Value.parentList.Add(element.Value.id);

            //sort post dominance by queue
            Dictionary<int, Node> reverseControlFlowGraphIsSort = new Dictionary<int, Node>();
            Queue sortBlockQueue = new Queue();
            sortBlockQueue.Enqueue(cfg.Blocks.Last().Ordinal);
            while (sortBlockQueue.Count > 0)
            {
                var Id = sortBlockQueue.Dequeue().ToString();
                foreach (var item in reverseControlFlowGraph)
                    if (item.Value.basicBlock.Ordinal == Int32.Parse(Id))
                    {
                        if (reverseControlFlowGraphIsSort.ContainsValue(item.Value))
                            break;
                        else
                        {
                            reverseControlFlowGraphIsSort.Add(item.Value.basicBlock.Ordinal, item.Value);
                            item.Value.childList.ForEach(x => sortBlockQueue.Enqueue(x));
                        }
                    }

            }
            return reverseControlFlowGraphIsSort;
        }

        public static Dictionary<int, postDominant> MPostDominant(string Code)
        {
            ControlFlowGraph CFG = MControlFlowGraphCompilation(Code);
            Dictionary<int, Node> reversControlFlowGraph = MReverseControlFlowGraph(Code);
            Dictionary<int, postDominant> postDominants = new Dictionary<int, postDominant>();

            // initial postDominant
            foreach (var basicBlock in CFG.Blocks.Reverse())
            {
                postDominant postDom = new postDominant();
                BasicBlockKind basicBlockKind = basicBlock.Kind;
                if (basicBlockKind.ToString() == "Exit")
                    postDominants.Add(basicBlock.Ordinal, postDom);
                else
                {
                    postDom.PostDominantBasicBlock = new List<BasicBlock>();
                    postDom.PostDominantId = new List<int>();
                    postDom.PostDominantBasicBlock.AddRange(CFG.Blocks);
                    //postDom.PostDominantBasicBlock.RemoveAt((CFG.Blocks.Count()) - 1);      // delete exit block
                    postDom.PostDominantBasicBlock.ForEach(x => postDom.PostDominantId.Add(x.Ordinal));
                    postDominants.Add(basicBlock.Ordinal, postDom);
                }
            }

            // obtaine postDominat for every node
            foreach (var item in reversControlFlowGraph)
            {
                if (item.Value.basicBlock.Kind.ToString() == "Exit")    // Set Node Exit
                {
                    postDominants[item.Key].PostDominantBasicBlock = new List<BasicBlock>();
                    postDominants[item.Key].PostDominantId = new List<int>();
                    postDominants[item.Key].PostDominantBasicBlock.Add(item.Value.basicBlock);
                    postDominants[item.Key].PostDominantId.Add(item.Value.id);
                    postDominants[item.Key].check = true;
                    continue;
                }                         //basic block is exit

                List<int> basicBlockResult = new List<int>();
                postDominants[item.Key].PostDominantId.ForEach(x => basicBlockResult.Add(x));           //basic block result

                postDominants[item.Key].PostDominantBasicBlock.RemoveRange(0, postDominants[item.Key].PostDominantBasicBlock.Count);
                postDominants[item.Key].PostDominantId.RemoveRange(0, postDominants[item.Key].PostDominantId.Count);
                postDominants[item.Key].PostDominantBasicBlock.Add(item.Value.basicBlock);            //1
                postDominants[item.Key].PostDominantId.Add(item.Value.basicBlock.Ordinal);            //1
                var parentList = item.Value.parentList;

                if (parentList.Count == 1)
                {
                    int parent = new int();
                    parent = parentList.ElementAt(0);
                    if (postDominants[parent].PostDominantBasicBlock == null)
                        postDominants[parent].PostDominantBasicBlock = new List<BasicBlock>();    // create new bject
                    if (postDominants[parent].PostDominantId == null)
                        postDominants[parent].PostDominantId = new List<int>();                   // create new object
                    if (postDominants[parent].PostDominantBasicBlock.Count() != 0 && postDominants[parent].check == true)                // if node is not Exit
                    {
                        postDominants[parent].PostDominantBasicBlock.ForEach(x => postDominants[item.Key].PostDominantBasicBlock.Add(x));        //2
                        postDominants[parent].PostDominantId.ForEach(x => postDominants[item.Key].PostDominantId.Add(x));
                    }
                    else if (postDominants[parent].PostDominantBasicBlock.Count() == 0)
                    {
                        postDominants[item.Key].PostDominantBasicBlock.Add(CFG.Blocks.Last());    // Add Exit Block 
                        postDominants[item.Key].PostDominantId.Add(CFG.Blocks.Last().Ordinal);
                    }                                                                                                                          // 2
                    postDominants[item.Key].check = true;
                }

                else if (parentList.Count > 1)
                {
                    // Intersect predecessore
                    foreach (var member in parentList)
                    {
                        List<int> basicBlockIntersect = postDominants[member].PostDominantId;
                        basicBlockResult = basicBlockResult.Intersect(basicBlockIntersect).ToList();
                    }

                    // Add list postDominantId
                    basicBlockResult.ForEach(x => postDominants[item.Key].PostDominantId.Add(x));

                    // Find basicBlock
                    foreach (var id in basicBlockResult)
                        foreach (var BB in CFG.Blocks)
                            if (id == BB.Ordinal)
                                postDominants[item.Key].PostDominantBasicBlock.Add(BB);
                    basicBlockResult.Clear();
                }

                postDominants[item.Key].check = true;
            }

            //sort postDominant
            Queue queue = new Queue();
            Dictionary<int, Node> cfg = MControlFlowGraph(Code);
            foreach (var postDominant in postDominants)
            {

                postDominant postDominantIsSort = new postDominant();
                postDominantIsSort.PostDominantId = new List<int>();
                postDominantIsSort.PostDominantBasicBlock = new List<BasicBlock>();
                queue.Enqueue(cfg.Last().Value.id);      //initial Quequ

                //initial postDominant
                postDominantIsSort.check = true;
                postDominantIsSort.id = postDominant.Key;
                while (queue.Count > 0 && postDominantIsSort.PostDominantId.Count != postDominant.Value.PostDominantId.Count)
                {
                    int Id = int.Parse(queue.Dequeue().ToString());                //1:Id pop from Queqe
                    Node node = cfg.ElementAtOrDefault(Id).Value;
                    if (postDominant.Value.PostDominantId.Exists(x => x == Id))
                    {
                        postDominantIsSort.PostDominantId.Add(node.id);
                        postDominantIsSort.PostDominantBasicBlock.Add(node.basicBlock);
                    }
                    foreach (var parent in node.parentList)   //2:parent Id add to queue
                        if (queue.Contains(parent) == false)  //parent hasn't in queue
                            queue.Enqueue(parent);
                }
                queue.Clear();
                postDominant.Value.PostDominantId.Clear();
                postDominantIsSort.PostDominantId.ForEach(x => postDominant.Value.PostDominantId.Add(x));  //Add sorting Ids
                postDominant.Value.PostDominantBasicBlock.Clear();
                postDominantIsSort.PostDominantBasicBlock.ForEach(x => postDominant.Value.PostDominantBasicBlock.Add(x)); //Add sorting basic Blocks
            }

            return postDominants;
        }

        public static void MWritePostDominant(string Code)
        {
            Dictionary<int, postDominant> postDominant = Tools.MPostDominant(Code);
            foreach (var item in postDominant)
            {
                Console.Write(" postDominant collection from BB" + item.Key + "  is:  ");
                item.Value.PostDominantId.ForEach(x => Console.Write(x + "  and  "));
                Console.WriteLine("");
            }
        }

        public static List<int> MFindPredecessore(BasicBlock block)
        {
            List<int> ParentList = new List<int>();
            //Console.WriteLine("Predecessore Block   " + block.Ordinal + "    is");
            var ListPre = block.Predecessors.ToList();
            if (ListPre != null)
                foreach (var item in ListPre)
                {
                    ParentList.Add(item.Source.Ordinal);
                    //Console.WriteLine(item.Source.Ordinal);
                }
            //Console.WriteLine("");
            return ParentList;
        }

        public static Dictionary<int, Reach> MReach(string code)
        {
            Dictionary<int, Node> nodes = MControlFlowGraphWithouthEdgeBack(code);
            Queue queue = new Queue();               //Create queue
            Dictionary<int, Reach> setReach = new Dictionary<int, Reach>();     //Create set of reach
            foreach (var item in nodes)
            {
                Reach reach = new Reach();
                reach.ReachsId = new List<int>();
                reach.Reachs = new List<BasicBlock>();
                reach.id = item.Value.id;
                queue.Enqueue(item.Value.id);
                while (queue.Count > 0)                  //Queue is empty?
                {
                    string basicBlockId = queue.Dequeue().ToString();
                    //Add Id
                    reach.ReachsId.Add(Int32.Parse(basicBlockId));
                    Node node = new Node();
                    node = nodes.First(x => x.Key == int.Parse(basicBlockId)).Value;
                    //Add Child to queue
                    foreach (var child in node.childList)
                    {
                        if (reach.ReachsId.Contains(child) || queue.Contains(child))   // has child in queue or reachsId?
                            continue;
                        else
                            queue.Enqueue(child);
                    }
                    //Add basicBlock
                    reach.Reachs.Add(node.basicBlock);
                }
                setReach.Add(item.Value.id, reach);
            }
            return setReach;
        }

        public static void MWriteReach(string Code)
        {
            Dictionary<int, Reach> setOfReach = Tools.MReach(Code);              //4:Compute Reach Collection
            foreach (var item in setOfReach)
            {
                Console.Write(" Reach collection from BB" + item.Key + "  is:  ");
                item.Value.ReachsId.ForEach(x => Console.Write(x + "  and  "));
                Console.WriteLine("");
            }
        }

        public static List<Node> MControlDependencyGraph(string Code)
        {
            Dictionary<int, Node> cfg = MControlFlowGraph(Code);
            Dictionary<int, postDominant> postDominants = MPostDominant(Code);
            List<Edge> edges = new List<Edge>();
            List<Node> nodes = new List<Node>();  //childes entry

            //create edge that must check
            foreach (var basicBlock in cfg)
            {
                Node node = new Node();           //1-1:obtaining entry child
                node.basicBlock = basicBlock.Value.basicBlock;
                node.id = basicBlock.Value.id;
                node.parentList = new List<int>();
                if (node.basicBlock.Kind != BasicBlockKind.Entry)
                    node.parentList.Add(cfg.First().Value.id);
                nodes.Add(node);

                //create edge
                List<int> postDominant = postDominants[basicBlock.Value.id].PostDominantId;
                foreach (var child in basicBlock.Value.childList)
                    if ((postDominant.Exists(x => x == child)) == false)
                    {
                        Edge edge = new Edge();
                        edge.A = basicBlock.Value.id;
                        edge.B = child;
                        edges.Add(edge);
                    }
            }

            Stack stack = new Stack();
            //Obtaining predecessore edge
            foreach (var edge in edges)
            {
                postDominants[edge.A].PostDominantId.ForEach(x => stack.Push(x));   //can not travel revers postDominant Id so we use from stack
                while (stack.Count > 0)
                {
                    int IdA = int.Parse(stack.Pop().ToString());
                    if (postDominants[edge.B].PostDominantId.Exists(x => x == IdA))
                    {
                        edge.parentEdgeAB = IdA;
                        break;
                    }
                }
                stack.Clear();
            }

            //Obtaining nodes related to A  from edge (A,B)
            foreach (var edge in edges)
            {
                postDominants[edge.B].PostDominantId.ForEach(x => stack.Push(x)); // B travel reverse in post Dominant tree
                while (stack.Count > 0)
                {
                    int IdB = int.Parse(stack.Pop().ToString());
                    if (IdB == edge.parentEdgeAB)
                        break;
                    else
                    {
                        edge.dependencyNodeA.Add(IdB);
                    }
                }
                stack.Clear();
            }

            //1-3:obtaining entry child
            edges.ForEach(edge => edge.dependencyNodeA.ForEach(x => nodes.Find(node => node.id == x).parentList.Clear()));

            //create dependency graph 
            Dictionary<int, Node> dependencyGraph = new Dictionary<int, Node>();
            foreach (var edge in edges)
            {
                int index = nodes.FindIndex(x => x.id == edge.A);
                if (nodes[index].childList == null)
                    nodes[index].childList = new List<int>();
                edge.dependencyNodeA.ForEach(x => nodes[index].childList.Add(x));
                edge.dependencyNodeA.ForEach(x => nodes.Find(y => y.id == x).parentList.Add(nodes[index].id));
            }

            nodes[cfg.First().Value.id].childList = new List<int>();  //Add child entry
            foreach (var node in nodes)
                if (node.parentList.Exists(x => x == cfg.First().Value.id))
                    nodes.First().childList.Add(node.id);

            //Add Line Number and statement
            SyntaxTree tree = CSharpSyntaxTree.ParseText(Code);
            ControlFlowGraph cfgCompilation = MControlFlowGraphCompilation(Code);
            var statementSyntaxes = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>().ToList();
            foreach (var block in cfgCompilation.Blocks)
            {
                nodes[block.Ordinal].statements = new List<Statement>();
                foreach (var item in block.Operations)
                {

                    Statement operation = new Statement();
                    StatementSyntax statement = statementSyntaxes.Where(x => x.FullSpan.Start <= item.Syntax.FullSpan.Start && x.FullSpan.End >= item.Syntax.FullSpan.End).Last();
                    operation.NumberLine = statement.SyntaxTree.GetLineSpan(statement.Span).StartLinePosition.Line;   //1:get line id 
                    TextSpan lineSpan = tree.GetText().Lines[operation.NumberLine].Span;
                    operation.statement = tree.GetText().ToString(lineSpan);                                          //2:get line text
                    nodes[block.Ordinal].statements.Add(operation);
                }

                if (block.BranchValue != null)        // add line example while,if , ...
                {

                    Statement operation = new Statement();
                    StatementSyntax statement = statementSyntaxes.Where(x => x.FullSpan.Start <= block.BranchValue.Syntax.FullSpan.Start && x.FullSpan.End >= block.BranchValue.Syntax.FullSpan.End).First();
                    int lineId = statement.SyntaxTree.GetLineSpan(block.BranchValue.Syntax.Span).StartLinePosition.Line;
                    operation.NumberLine = lineId;                                                                     //1:get line id 
                    TextSpan lineSpan = tree.GetText().Lines[lineId].Span;
                    operation.statement = tree.GetText().ToString(lineSpan);
                    nodes[block.Ordinal].statements.Add(operation);
                }
            }
            return nodes;

            return nodes;
        }

        public static void MWriteDependencyGroph(string Code)
        {
            List<Node> basicBlocks = new List<Node>();
            basicBlocks = MControlDependencyGraph(Code);
            foreach (var block in basicBlocks)
            {
                Console.WriteLine("BB" + block.basicBlock.Ordinal + ":");
                Console.Write("Parent is:");
                if (block.parentList != null)
                    block.parentList.ForEach(x => Console.Write(x + " , "));
                Console.WriteLine();
                Console.Write("Child is:");
                if (block.childList != null)
                    block.childList.ForEach(x => Console.Write(x + " , "));
                Console.WriteLine(); Console.WriteLine();
                foreach (var stat in block.statements)
                    Console.WriteLine(stat.NumberLine + "." + stat.statement.Replace("           ", " "));
                Console.WriteLine("...........................................");

            }
        }

        public static List<Dom> MDomBasicBlock(string Code)
        {
            List<Node> nodesCDG = new List<Node>();
            Node block = new Node();
            nodesCDG = MControlDependencyGraph(Code);    //Create Dependency Graph


            //Create List Dom For basic block
            List<Dom> doms = new List<Dom>();
            Queue queue = new Queue();
            foreach (var basicblock in nodesCDG)
            {
                if (basicblock.basicBlock.Kind == BasicBlockKind.Entry || basicblock.basicBlock.Kind == BasicBlockKind.Exit)
                    continue;
                basicblock.parentList.ForEach(x => queue.Enqueue(x));     // 1:Add parents node to queqe
                List<Node> nodes = new List<Node>();
                while (queue.Count > 0)
                {
                    int id = int.Parse(queue.Dequeue().ToString());
                    Node node = nodesCDG.Where(x => x.id == id).First();
                    nodes.Add(node);                                      // 2:Add dominant block
                    if (node.childList != null)
                        node.childList.ForEach(x => queue.Enqueue(x));    // 3:Add childs node to queue 
                }
                Dom dom = new Dom(basicblock.id, basicblock, nodes);
                doms.Add(dom);
            }

            return doms;
        }

        public static List<Node> Blocks(string Code, int LineNumber)
        {
            List<Node> nodesCDG = new List<Node>();
            Node block = new Node();
            nodesCDG = MControlDependencyGraph(Code);      //Create Dependency Graph
            List<Dom> doms = new List<Dom>();
            doms = MDomBasicBlock(Code);                   //Create Doms Collection
            Dictionary<int, Reach> reachs = MReach(Code);  //Create reachs

            //Find Basic block
            foreach (var node in nodesCDG)
            {
                if (node.statements != null && node.statements.Exists(x => x.NumberLine == LineNumber))
                {
                    block = node;
                    break;
                }
            }

            //Create Blocks Collection
            List<Node> Blocks = new List<Node>();
            foreach (var node in nodesCDG)
            {
                if (node.basicBlock.Kind == BasicBlockKind.Entry || node.basicBlock.Kind == BasicBlockKind.Exit)
                    continue;
                Dom dom = doms.Where(x => x.Id == node.id).Single();
                Reach reach = reachs.Values.Where(x => x.id == node.id).Single();
                if (dom.Doms.Exists(x => x.id == block.id) && reach.ReachsId.Exists(x => x == block.id))
                    Blocks.Add(node);
            }

            return Blocks;
        }

        public static Dictionary<int, Node> MDataFlowAnalysisCompilation(string Code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(Code);
            var Mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

            var compilation = CSharpCompilation.Create("MyCompilation", syntaxTrees: new[] { tree }, references: new[] { Mscorlib });
            var model = compilation.GetSemanticModel(tree);
            var Statements = tree.GetRoot().DescendantNodes().OfType<StatementSyntax>().ToList();
            var Expressions = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>();

            //1:Create DFA for every basicBlock  
            var basicBlocks = MControlFlowGraph(Code);
            foreach (var block in basicBlocks)
            {
                if (block.Value.basicBlock.Kind == BasicBlockKind.Entry || block.Value.basicBlock.Kind == BasicBlockKind.Exit)
                    continue;
                List<StatementSyntax> syntaxes = new List<StatementSyntax>();

                if (block.Value.basicBlock.Operations.Length >= 2)   // if Operations is two or more 
                {
                    foreach (var statement in Statements) //Find First Statement
                        if ((statement.FullSpan.Start <= block.Value.basicBlock.Operations.First().Syntax.FullSpan.Start) && (statement.FullSpan.End >= block.Value.basicBlock.Operations.First().Syntax.FullSpan.End))
                            syntaxes.Add(statement);
                    var firstSyntax = syntaxes.Last();
                    syntaxes.Clear();

                    foreach (var statement in Statements) //Find Last Statement
                        if ((statement.FullSpan.Start <= block.Value.basicBlock.Operations.Last().Syntax.FullSpan.Start) && (statement.FullSpan.End >= block.Value.basicBlock.Operations.Last().Syntax.FullSpan.End))
                            syntaxes.Add(statement);
                    var lastSyntax = syntaxes.Last();
                    syntaxes.Clear();
                    block.Value.dataFlowAnalysisBlock = model.AnalyzeDataFlow(firstSyntax, lastSyntax);
                }

                else if (block.Value.basicBlock.Operations.Length == 1)     //Operations of basicBlock is one
                {
                    foreach (var statement in Statements)
                        if ((statement.FullSpan.Start <= block.Value.basicBlock.Operations.First().Syntax.FullSpan.Start) && (statement.FullSpan.End >= block.Value.basicBlock.Operations.First().Syntax.FullSpan.End))
                            syntaxes.Add(statement);
                    var firstSyntax = syntaxes.Last();
                    block.Value.dataFlowAnalysisBlock = model.AnalyzeDataFlow(firstSyntax);
                    syntaxes.Clear();
                }
            }

            return basicBlocks;
        }

        public static Dictionary<int, Node> Gen(string Code)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(Code);
            List<ISymbol> variabales = new List<ISymbol>();
            Dictionary<int, Node> basicBlocks = MDataFlowAnalysisCompilation(Code);

            //create gen collection
            foreach (var item in basicBlocks)
            {
                if (item.Value.dataFlowAnalysisBlock != null)
                    foreach (var element in item.Value.dataFlowAnalysisBlock.AlwaysAssigned)
                        variabales.Add(element);
                item.Value.gens = new List<Gen>();
                foreach (var variable in variabales)
                {
                    Gen gen = new Gen();
                    foreach (var operation in item.Value.basicBlock.Operations.Reverse())
                    {
                        var tokens = operation.Syntax.DescendantTokens().ToList();
                        //check sentence For example x = x + 1;
                        if (tokens.Exists(x => x.ValueText.ToString() == "=" | x.ValueText.ToString() == "+=" | x.ValueText.ToString() == "-=") & tokens.Exists(x => x.ValueText.ToString() == variable.ToString()))
                        {
                            TextSpan spanVariable = tokens.Find(x => x.ValueText.ToString() == variable.ToString()).Span;
                            TextSpan spanEqual = tokens.Find(x => x.ValueText.ToString() == "=" | x.ValueText.ToString() == "+=" | x.ValueText.ToString() == "-=").Span;
                            if (spanVariable.End < spanEqual.Start)       //this sentence is checking variable must come before eqaul : x = totalAmount + 2;   
                            {
                                gen.lineId = tree.GetLineSpan(operation.Syntax.Span).EndLinePosition.Line;
                                gen.variable = variable.ToString();
                                break;
                            }
                        }

                        //check sentence For example x++;
                        if (tokens.Exists(x => x.ValueText.ToString() == "++" | x.ValueText.ToString() == "--") & !(tokens.Exists(x => x.ValueText.ToString() == "=")) & tokens.Exists(x => x.ValueText.ToString() == variable.ToString()))
                        {
                            gen.lineId = tree.GetLineSpan(operation.Syntax.Span).EndLinePosition.Line;
                            gen.variable = variable.ToString();
                            break;
                        }
                    }

                    item.Value.gens.Add(gen);
                }
                variabales.Clear();
            }

            return basicBlocks;
        }

        public static Dictionary<int, Node> kill(Dictionary<int, Node> basicBlocks, string code)
        {
            List<string> variables = new List<string>();
            List<Node> checkNodes = new List<Node>();
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            foreach (var block in basicBlocks)
                checkNodes.Add(block.Value);                                                  //1:create node collection
            foreach (var block in basicBlocks)
            {
                block.Value.gens.ForEach(x => variables.Add(x.variable));                    //2:what variables are check for block
                block.Value.kills = new List<Kill>();
                checkNodes.Remove(block.Value);                                              //3:block must remove from Checknode(CheckBlock)

                foreach (var node in checkNodes)
                    foreach (var operation in node.basicBlock.Operations)
                    {
                        var tokens = operation.Syntax.DescendantTokens().ToList();
                        foreach (var variable in variables)
                        {
                            Kill killVariable = new Kill();

                            //check sentence For example x = x + 1;
                            if (tokens.Exists(x => x.ValueText.ToString() == "=" | x.ValueText.ToString() == "+=" | x.ValueText.ToString() == "-=") & tokens.Exists(x => x.ValueText.ToString() == variable.ToString()))
                            {
                                TextSpan spanVariable = tokens.Find(x => x.ValueText.ToString() == variable.ToString()).Span;
                                TextSpan spanEqual = tokens.Find(x => x.ValueText.ToString() == "=" | x.ValueText.ToString() == "+=" | x.ValueText.ToString() == "-=").Span;
                                if (spanVariable.End < spanEqual.Start)       //this sentence is checking variable must come before eqaul : x = totalAmount + 2;   
                                {
                                    killVariable.lineId = tree.GetLineSpan(operation.Syntax.Span).EndLinePosition.Line;
                                    killVariable.variable = variable;
                                    block.Value.kills.Add(killVariable);
                                }
                            }

                            //check sentence For example x++;
                            if (tokens.Exists(x => x.ValueText.ToString() == "++" | x.ValueText.ToString() == "--") & !(tokens.Exists(x => x.ValueText.ToString() == "=")) & tokens.Exists(x => x.ValueText.ToString() == variable.ToString()))
                            {
                                killVariable.lineId = tree.GetLineSpan(operation.Syntax.Span).EndLinePosition.Line;
                                killVariable.variable = variable;
                                block.Value.kills.Add(killVariable);
                            }


                        }
                    }

                checkNodes.Add(block.Value);
                variables.Clear();
            }

            return basicBlocks;
        }

        public static void InOut(Dictionary<int, Node> basicBlocks, string code)
        {
            foreach (var block in basicBlocks)           //1: Out[n] = gen[n]
            {
                block.Value.outs = new List<Out>();
                block.Value.ins = new List<In>();
                block.Value.gens.ForEach(x => block.Value.outs.Add(new Out(x.lineId, x.variable)));
            }
            bool change = true;
            while (change == true)
            {
                change = false;
                foreach (var block in basicBlocks)
                {
                    foreach (var ParentId in block.Value.parentList)
                    {
                        var parentBlack = basicBlocks.First(x => x.Value.id == ParentId);
                        //parentBlack.Value.outs.ForEach(x => block.Value.ins.Add(new In(x.id, x.variable)));
                        foreach (var outt in parentBlack.Value.outs)
                        {
                            if (!(block.Value.ins.Exists(x => x.id == outt.id & x.variable == outt.variable)))
                                block.Value.ins.Add(new In(outt.id, outt.variable));
                        }
                    }                                         //create In collection

                    List<Out> oldeOut = new List<Out>(block.Value.outs);                                        //oldout = out[n]
                    block.Value.outs.Clear();
                    block.Value.gens.ForEach(x => block.Value.outs.Add(new Out(x.lineId, x.variable)));         //Add gen    

                    foreach (var inn in block.Value.ins)
                    {
                        if (!block.Value.kills.Exists(x => x.variable == inn.variable & x.lineId == inn.id))
                            block.Value.outs.Add(new Out(inn.id, inn.variable));
                    }                                                    //Add (In - kill)         

                    foreach (var outt in block.Value.outs)
                    {
                        if (!(oldeOut.Exists(x => x.id == outt.id & x.variable == x.variable)))
                        {
                            change = true;
                            break;
                        }
                    }                                                  //4:compare outold with old
                }
            }
        }
    }

}

