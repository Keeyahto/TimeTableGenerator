"use client";

import { Card, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";

export type AuditRow = {
  key: string;
  sourceFileId: string;
  status: string;
  createdAt: string;
};

const columns: ColumnsType<AuditRow> = [
  { title: "Source file", dataIndex: "sourceFileId", key: "sourceFileId" },
  {
    title: "Статус",
    dataIndex: "status",
    key: "status",
    render: (status: string) => <Tag>{status}</Tag>,
  },
  { title: "Создан", dataIndex: "createdAt", key: "createdAt" },
];

export function AuditView({ data }: { data: AuditRow[] }) {
  return (
    <Card title="Data audit runs">
      {data.length === 0 ? (
        <Tag color="default">Заглушка: audit pipeline не подключён</Tag>
      ) : null}
      <Table columns={columns} dataSource={data} pagination={false} />
    </Card>
  );
}
