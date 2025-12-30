-- 创建数据库
CREATE DATABASE IF NOT EXISTS zbosql_demo;

-- 连接到数据库
\c zbosql_demo;

-- 创建产品表
CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    product_name VARCHAR(200) NOT NULL,
    price DECIMAL(10, 2) NOT NULL DEFAULT 0.00,
    stock_quantity INTEGER NOT NULL DEFAULT 0,
    category VARCHAR(100),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP
);

-- 插入示例数据
INSERT INTO products (product_name, price, stock_quantity, category) VALUES
('笔记本电脑', 5999.99, 50, '电子产品'),
('无线鼠标', 99.99, 200, '电子产品'),
('机械键盘', 299.99, 100, '电子产品'),
('咖啡杯', 29.99, 500, '日用品'),
('运动鞋', 399.99, 80, '服装'),
('T恤', 89.99, 300, '服装'),
('牛仔裤', 199.99, 150, '服装'),
('智能手表', 1299.99, 60, '电子产品'),
('保温杯', 59.99, 400, '日用品'),
('蓝牙耳机', 199.99, 120, '电子产品')
ON CONFLICT DO NOTHING;
