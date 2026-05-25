import { AuditView } from "@/components/pages/AuditView";
import { prisma } from "@/lib/prisma";

export const dynamic = "force-dynamic";

export default async function AuditPage() {
  const runs = await prisma.dataAuditRun.findMany({
    orderBy: { createdAt: "desc" },
    take: 50,
  });

  const data = runs.map((run) => ({
    key: run.id,
    sourceFileId: run.sourceFileId,
    status: run.status,
    createdAt: run.createdAt.toISOString(),
  }));

  return <AuditView data={data} />;
}
