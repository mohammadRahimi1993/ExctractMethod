using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractMrthod
{
    public class Tree
    {
        public int id;
        public List<int> ParentList;
        public List<int> ChildList;

        public List<int> AddParent(BasicBlock Basicblock)
        {
            ParentList.Add(Basicblock.Ordinal);
            return ParentList;
        }
        public List<int> AddChild(BasicBlock Basicblock)
        {
            ChildList.Add(Basicblock.Ordinal);
            return ChildList;
        }
    }

    public class Node
    {
        public int id;
        public List<int> parentList;
        public List<int> childList;
        public BasicBlock basicBlock;
        public DataFlowAnalysis dataFlowAnalysisBlock;
        public List<Statement> statements;
        public List<Gen> gens;
        public List<Kill> kills;
        public List<In> ins;
        public List<Out> outs;
    }

    public class Statement
    {
        public string statement;
        public int NumberLine;
    }

    public class Reach
    {
        public int id;
        public List<BasicBlock> Reachs;
        public List<int> ReachsId;
    }

    public class Edge
    {
        public int A;
        public int B;
        public int parentEdgeAB;   //  parent nearest of A , B in postDominanttree
        public List<int> dependencyNodeA = new List<int>();  // set of node dependency to Node A 
    }

    public class postDominant
    {
        //public postDominant(int id, List<int> PostDominantId, List<BasicBlock> PostDominantBasicBlock , Boolean check)
        //{
        //    this.check = check;
        //    this.PostDominantId = PostDominantId;
        //    this.PostDominantBasicBlock = PostDominantBasicBlock;
        //    this.id = id;
        //}

        public int id;
        public List<int> PostDominantId;
        public List<BasicBlock> PostDominantBasicBlock;
        public Boolean check = false;

    }

    public class Dom
    {
        //attribute
        private int _id;
        private Node _basicBlock;
        private List<Node> _Doms;

        //Property
        public int Id
        {
            get { return _id; }
            set { _id = value; }
        }
        public Node BasicBlock
        {
            get { return _basicBlock; }
            set { BasicBlock = _basicBlock; }
        }
        public List<Node> Doms
        {
            get { return _Doms; }
            set { _Doms = value; }
        }

        public Dom()
        {

        }

        public Dom(int id, Node basicBlock, List<Node> Doms)
        {
            _id = id;
            _basicBlock = basicBlock;
            _Doms = Doms;
        }

        public void show()
        {
            Console.Write(" Doms Collection from BB" + _id + "  is:  ");
            _Doms.ForEach(x => Console.Write(x.id + "  and  "));
            Console.WriteLine("");
        }

        ~Dom()
        {

        }
    }

    public class Gen
    {
        private int _lineId;                //attribute
        private string _variable;           //attribute

        public int lineId
        {
            get { return _lineId; }
            set { _lineId = value; }
        }              //Property

        public string variable
        {
            get { return _variable; }
            set { _variable = value; }
        }          //Property

        public Gen()
        {

        }

        public Gen(int lineId, string variable)
        {
            _lineId = lineId;
            _variable = variable;
        }

        ~Gen()
        {

        }
    }

    public class Kill
    {
        private int _lineId;
        private string _variable;

        public int lineId
        {
            get { return _lineId; }
            set { _lineId = value; }
        }     //property

        public string variable
        {
            get { return _variable; }
            set { _variable = value; }
        }    // property

        public Kill()
        {

        }            //Constructore

        public Kill(int lineId, string variable)
        {
            _lineId = lineId;
            _variable = variable;
        }       //Constructore
    }

    public class In
    {
        string _variable;
        int _id;

        public string variable
        {
            get { return _variable; }
            set { _variable = value; }
        }

        public int id
        {
            get { return _id; }
            set { _id = value; }
        }

        public In()
        {

        }

        public In(int id, string variable)
        {
            _id = id;
            _variable = variable;
        }

        ~In()
        {

        }
    }

    public class Out
    {
        string _variable;
        int _id;

        public string variable
        {
            get { return _variable; }
            set { _variable = variable; }
        }

        public int id
        {
            get { return _id; }
            set { _id = id; }
        }

        public Out()
        {

        }

        public Out(int Id, string Variable)
        {
            _id = Id;
            _variable = Variable;
        }

        ~Out()
        {

        }
    }







    //    public class PostDominants : IEnumerable
    //    {
    //        private postDominant[] _postDominants;
    //        public PostDominants(postDominant[] pArray)
    //        {
    //            _postDominants = new postDominant[pArray.Length];

    //            for (int i = 0; i < pArray.Length; i++)
    //            {
    //                _postDominants[i] = pArray[i];
    //            }
    //        }

    //        // Implementation for the GetEnumerator method.
    //        IEnumerator IEnumerable.GetEnumerator()
    //        {
    //            return (IEnumerator)GetEnumerator();
    //        }

    //        public postDominantsEnum GetEnumerator()
    //        {
    //            return new postDominantsEnum(_postDominants);
    //        }
    //    }

    //    public class postDominantsEnum : IEnumerator
    //    {
    //        public postDominant[] _postDominants;

    //        // Enumerators are positioned before the first element
    //        // until the first MoveNext() call.
    //        int position = -1;

    //        public PeopleEnum(postDominant[] list)
    //        {
    //            _postDominants = list;
    //        }

    //        public bool MoveNext()
    //        {
    //            position++;
    //            return (position < _postDominants.Length);
    //        }

    //        public void Reset()
    //        {
    //            position = -1;
    //        }

    //        object IEnumerator.Current
    //        {
    //            get
    //            {
    //                return Current;
    //            }
    //        }

    //        public postDominant Current
    //        {
    //            get
    //            {
    //                try
    //                {
    //                    return _postDominants[position];
    //                }
    //                catch (IndexOutOfRangeException)
    //                {
    //                    throw new InvalidOperationException();
    //                }
    //            }
    //        }
    //    }
}
