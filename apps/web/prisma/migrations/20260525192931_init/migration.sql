-- CreateTable
CREATE TABLE "DataSourceFile" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "originalName" TEXT NOT NULL,
    "storagePath" TEXT NOT NULL,
    "fileType" TEXT NOT NULL,
    "uploadedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "notes" TEXT
);

-- CreateTable
CREATE TABLE "DataAuditRun" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "sourceFileId" TEXT NOT NULL,
    "status" TEXT NOT NULL,
    "reportJson" TEXT NOT NULL,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "DataAuditRun_sourceFileId_fkey" FOREIGN KEY ("sourceFileId") REFERENCES "DataSourceFile" ("id") ON DELETE CASCADE ON UPDATE CASCADE
);

-- CreateTable
CREATE TABLE "ManualDecision" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "entityType" TEXT NOT NULL,
    "sourceValue" TEXT NOT NULL,
    "canonicalValue" TEXT NOT NULL,
    "decisionStatus" TEXT NOT NULL,
    "comment" TEXT,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- CreateTable
CREATE TABLE "SolverRun" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "inputPath" TEXT NOT NULL,
    "outputPath" TEXT NOT NULL,
    "status" TEXT NOT NULL,
    "startedAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "finishedAt" DATETIME,
    "summaryJson" TEXT
);

-- CreateTable
CREATE TABLE "SolverArtifact" (
    "id" TEXT NOT NULL PRIMARY KEY,
    "solverRunId" TEXT NOT NULL,
    "artifactType" TEXT NOT NULL,
    "path" TEXT NOT NULL,
    "createdAt" DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT "SolverArtifact_solverRunId_fkey" FOREIGN KEY ("solverRunId") REFERENCES "SolverRun" ("id") ON DELETE CASCADE ON UPDATE CASCADE
);
