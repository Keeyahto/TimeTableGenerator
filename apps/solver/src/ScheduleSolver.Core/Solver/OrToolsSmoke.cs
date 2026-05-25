using Google.OrTools.Sat;

namespace ScheduleSolver.Core.Solver;

public static class OrToolsSmoke
{
    public static string Run()
    {
        var model = new CpModel();
        var x = model.NewBoolVar("x");
        var y = model.NewBoolVar("y");
        model.AddBoolOr(new[] { x, y });
        model.Maximize(x + y);

        var solver = new CpSolver();
        var status = solver.Solve(model);
        return status.ToString();
    }
}
