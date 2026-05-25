import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  outputFileTracingRoot: require("path").join(__dirname, "../.."),
  transpilePackages: ["antd", "@ant-design", "@ant-design/icons", "@ant-design/nextjs-registry"],
  // TODO: remove when antd + React 19 types are fully aligned in strict build
  typescript: {
    ignoreBuildErrors: true,
  },
};

export default nextConfig;
