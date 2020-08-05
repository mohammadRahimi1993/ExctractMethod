using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace ExtractMrthod
{
    class Program
    {

        static void Main(string[] args)
        {

            //var Code = File.ReadAllText("C:\\Users\\Mohammad\\Dropbox\\My PC (DESKTOP-95EI2F5)\\Desktop\\Software Engineer\\Project\\CodeExample\\CodeExampleMe.txt");
            var Code = File.ReadAllText("C:\\Users\\Mohammad\\Dropbox\\My PC (DESKTOP-95EI2F5)\\Desktop\\Software Engineer\\Project\\CodeExample\\CodeExample\\CodeExample.txt");
            //var Code = File.ReadAllText("C:\\Users\\Mohammad\\Dropbox\\My PC (DESKTOP-95EI2F5)\\Desktop\\Software Engineer\\Project\\CodeExample\\Example-DrParsa.txt");
            //var Code = File.ReadAllText("C:\\Users\\Mohammad\\Dropbox\\My PC (DESKTOP-95EI2F5)\\Desktop\\Software Engineer\\Project\\CodeExample\\Example-In-Out.txt");
            //var Code = File.ReadAllText("C:\\Users\\Mohammad\\Dropbox\\My PC (DESKTOP-95EI2F5)\\Desktop\\CodeExampe.txt");

            Tools.writeCFG(Code);                                                   //1:basicBlock information 

            Console.WriteLine();
            Tools.MWriteDominant(Code);                                             //2:Compute Dominant Collection                                         
              
            Console.WriteLine();                                                    //3:Compute postDominant Collection
            Tools.MWritePostDominant(Code);
            Console.WriteLine();

            Console.WriteLine();
            Tools.MWriteReach(Code);                                                 //4:Compute Reach Collection
            Console.WriteLine();

            Tools.MWriteDependencyGroph(Code);                                       //5:Compute Dependency Graph
            Console.WriteLine("");

            List<Dom> doms = Tools.MDomBasicBlock(Code);                             //6:compute dom collection
            foreach (var dom in doms)
                dom.show();

            List<Node> nodes = Tools.Blocks(Code, 15);                                //7:Compute Blocks

            Dictionary<int, Node> basicBlocks = Tools.Gen(Code);                      //8:compute Gen

            basicBlocks = Tools.kill(basicBlocks, Code);                              //9:compute Kill

            Tools.InOut(basicBlocks, Code);                                           //10:compute InOut




            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();      //Compute time
            //stopwatch.Start();
            //Tools.MDominant(Code);
            //Tools.MPostDominant(Code);
            //Tools.MReach(Code);
            //Tools.MControlDependencyGraph(Code);
            //Tools.MDomBasicBlock(Code);
            //Tools.Blocks(Code, 15);
            //Dictionary<int, Node> basicBlockss = Tools.Gen(Code);
            //basicBlockss = Tools.kill(basicBlockss, Code);
            //Tools.InOut(basicBlockss, Code);
            //stopwatch.Stop();
            //Console.WriteLine("Time is:" + stopwatch.ElapsedMilliseconds.ToString());
        }

    }
}
