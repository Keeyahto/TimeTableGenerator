"use client";

import { Card, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";

export type SolverRunRow = {
  key: string;
  status: string;
  inputPath: string;
  outputPath: string;
  startedAt: string;
};

const columns: ColumnsType<SolverRunRow> = [
  {
    title: "Статус",
    dataIndex: "status",
    key: "status",
    render: (status: string) => <Tag color="gold">{status}</Tag>,
  },
  { title: "Input", dataIndex: "inputPath", key: "inputPath" },
  { title: "Output", dataIndex: "outputPath", key: "outputPath" },
  { title: "Старт", dataIndex: "startedAt", key: "startedAt" },
];

export function SolverRunsView({ data }: { data: SolverRunRow[] }) {
  return (
    <Card title="Запуски solver">
      {data.length === 0 ? (
        <Tag color="warning">
          Нет запусков — вызов CLI из UI пока не реализован
        </Tag>
      ) : null}
      <Table columns={columns} dataSource={data} pagination={false} />
    </Card>
  );
}
