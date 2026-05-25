"use client";

import { Button, Card, Table, Tag } from "antd";
import type { ColumnsType } from "antd/es/table";

export type FileRow = {
  key: string;
  originalName: string;
  fileType: string;
  uploadedAt: string;
};

const columns: ColumnsType<FileRow> = [
  { title: "Имя", dataIndex: "originalName", key: "originalName" },
  { title: "Тип", dataIndex: "fileType", key: "fileType" },
  { title: "Загружен", dataIndex: "uploadedAt", key: "uploadedAt" },
];

export function FilesView({ data }: { data: FileRow[] }) {
  return (
    <Card
      title="Файлы источников"
      extra={
        <Button disabled type="primary">
          Загрузить файл
        </Button>
      }
    >
      {data.length === 0 ? (
        <Tag>Нет записей — загрузка файлов пока не реализована</Tag>
      ) : null}
      <Table columns={columns} dataSource={data} pagination={false} />
    </Card>
  );
}
