"use client";

import { Button, Card, Col, Row, Tag, Typography } from "antd";

const { Paragraph, Text } = Typography;

export function DashboardView() {
  return (
    <Row gutter={[16, 16]}>
      <Col span={24}>
        <Card title="Dashboard">
          <Paragraph>
            Управляющий слой платформы генерации расписаний. Solver изолирован
            и вызывается только через JSON-файлы.
          </Paragraph>
          <Tag color="blue">Web + Prisma</Tag>
          <Tag color="gold">Solver stub</Tag>
          <Tag>Contracts v0.1</Tag>
          <div style={{ marginTop: 16 }}>
            <Button type="primary" disabled>
              Запустить solver (скоро)
            </Button>
          </div>
          <Paragraph type="secondary" style={{ marginTop: 16 }}>
            <Text type="secondary">
              TODO: экспорт normalized input, запуск CLI, импорт output в
              SolverRun.
            </Text>
          </Paragraph>
        </Card>
      </Col>
      <Col xs={24} md={12}>
        <Card title="Контур данных" size="small">
          <ul>
            <li>Загрузка файлов — заглушка</li>
            <li>Data audit — заглушка</li>
            <li>Manual review — заглушка</li>
          </ul>
        </Card>
      </Col>
      <Col xs={24} md={12}>
        <Card title="Solver" size="small">
          <ul>
            <li>CP-SAT — не реализован</li>
            <li>CLI stub — готов в apps/solver</li>
            <li>БД в solver — запрещена</li>
          </ul>
        </Card>
      </Col>
    </Row>
  );
}
