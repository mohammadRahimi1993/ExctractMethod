using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractMrthod
{
    class CodeExample
    {
        public int f(ref int l, ref int j)
        {
            int a, b;
            l = l + 1;
            j = j + 1;
            return l;
        }

        public void h()
        {
            int m, n;
            m = 10;
            n = 20;
            m = f(ref m, ref n);
            Console.WriteLine("m:" + m + "n:" + n);
            m = f(ref n, ref n);
            m = f(ref n, ref n);
            Console.ReadKey();
        }

        public void InOut()
        {
            int d;
            int i = int.Parse(Console.ReadLine());
            int j = int.Parse(Console.ReadLine());
            i = i + 1;
            if (i < 5)
            {
                d = i * j;
                d = d + 1;
                i = d * 2;
            }
            else
            {
                d = i - j;
                j = d + 1;
            }
            j = d * i;
            Console.WriteLine(j);
            if (j < 100) { };
        }

        public void CheckInOut()
        {
            int c = 1;
            int x = 4;
            int y = 7;
            while (x < c)
            {
                if (y > 3)
                {
                    y = x + 1;
                    x = x + 1;
                }

                else
                {
                    int a = x + 1;
                    y = x + 2;
                }

                x = x + 1;
            }
            Console.WriteLine();

        }
    }
}
