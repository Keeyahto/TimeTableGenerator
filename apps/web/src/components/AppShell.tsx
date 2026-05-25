"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Layout, Menu, Typography } from "antd";
import type { MenuProps } from "antd";

const { Header, Sider, Content } = Layout;
const { Title } = Typography;

const menuItems: MenuProps["items"] = [
  { key: "/", label: <Link href="/">Dashboard</Link> },
  { key: "/files", label: <Link href="/files">Файлы</Link> },
  { key: "/audit", label: <Link href="/audit">Audit</Link> },
  {
    key: "/manual-review",
    label: <Link href="/manual-review">Ручная проверка</Link>,
  },
  {
    key: "/solver-runs",
    label: <Link href="/solver-runs">Запуски solver</Link>,
  },
];

export function AppShell({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const selectedKey =
    menuItems?.find((item) => item?.key === pathname)?.key?.toString() ?? "/";

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <Sider breakpoint="lg" collapsedWidth="0" theme="light">
        <div style={{ padding: "16px" }}>
          <Title level={5} style={{ margin: 0 }}>
            Schedule Platform
          </Title>
        </div>
        <Menu mode="inline" selectedKeys={[selectedKey]} items={menuItems} />
      </Sider>
      <Layout>
        <Header
          style={{
            background: "#fff",
            padding: "0 24px",
            borderBottom: "1px solid #f0f0f0",
          }}
        >
          <Title level={4} style={{ margin: "16px 0" }}>
            Управление данными и запусками solver
          </Title>
        </Header>
        <Content style={{ margin: 24 }}>{children}</Content>
      </Layout>
    </Layout>
  );
}
