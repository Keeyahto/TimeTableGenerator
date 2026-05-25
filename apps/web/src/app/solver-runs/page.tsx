import { SolverRunsView } from "@/components/pages/SolverRunsView";
import { prisma } from "@/lib/prisma";

export const dynamic = "force-dynamic";

export default async function SolverRunsPage() {
  const runs = await prisma.solverRun.findMany({
    orderBy: { startedAt: "desc" },
    take: 50,
  });

  const data = runs.map((run) => ({
    key: run.id,
    status: run.status,
    inputPath: run.inputPath,
    outputPath: run.outputPath,
    startedAt: run.startedAt.toISOString(),
  }));

  return <SolverRunsView data={data} />;
}
