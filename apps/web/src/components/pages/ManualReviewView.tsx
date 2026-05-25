"use client";

import { Card, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";

export type DecisionRow = {
  key: string;
  entityType: string;
  sourceValue: string;
  canonicalValue: string;
  decisionStatus: string;
};

const columns: ColumnsType<DecisionRow> = [
  { title: "Тип", dataIndex: "entityType", key: "entityType" },
  { title: "Исходное", dataIndex: "sourceValue", key: "sourceValue" },
  { title: "Каноническое", dataIndex: "canonicalValue", key: "canonicalValue" },
  {
    title: "Статус",
    dataIndex: "decisionStatus",
    key: "decisionStatus",
    render: (status: string) => <Tag color="processing">{status}</Tag>,
  },
];

export function ManualReviewView({ data }: { data: DecisionRow[] }) {
  return (
    <Card title="Ручные решения">
      {data.length === 0 ? (
        <Tag>Нет решений — UI редактирования пока не реализован</Tag>
      ) : null}
      <Table columns={columns} dataSource={data} pagination={false} />
    </Card>
  );
}
