-- 创建测试表
CREATE TABLE IF NOT EXISTS test_users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(200)
);

-- 插入测试数据
INSERT INTO test_users (username, email) VALUES
('user1', 'user1@example.com'),
('user2', 'user2@example.com'),
('user3', 'user3@example.com')
ON CONFLICT (id) DO NOTHING;
