import { FilesView } from "@/components/pages/FilesView";
import { prisma } from "@/lib/prisma";

export const dynamic = "force-dynamic";

export default async function FilesPage() {
  const files = await prisma.dataSourceFile.findMany({
    orderBy: { uploadedAt: "desc" },
    take: 50,
  });

  const data = files.map((file) => ({
    key: file.id,
    originalName: file.originalName,
    fileType: file.fileType,
    uploadedAt: file.uploadedAt.toISOString(),
  }));

  return <FilesView data={data} />;
}
