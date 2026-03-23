using System;
using System.Collections.Generic;
using System.Text;

namespace Solver
{
	class Program
	{
		static void Main(string[] args)
		{
			KellyOptimizer.Solve(new double[] { -103.0, 107.0, -18.0 }, new double[] { 0.344588, 0.57788, 0.077532 }, new double[] { 3.2, 1.6, 14.0 });
		}
	}
}

