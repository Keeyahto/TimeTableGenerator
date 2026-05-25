import { ManualReviewView } from "@/components/pages/ManualReviewView";
import { prisma } from "@/lib/prisma";

export const dynamic = "force-dynamic";

export default async function ManualReviewPage() {
  const decisions = await prisma.manualDecision.findMany({
    orderBy: { createdAt: "desc" },
    take: 50,
  });

  const data = decisions.map((item) => ({
    key: item.id,
    entityType: item.entityType,
    sourceValue: item.sourceValue,
    canonicalValue: item.canonicalValue,
    decisionStatus: item.decisionStatus,
  }));

  return <ManualReviewView data={data} />;
}
