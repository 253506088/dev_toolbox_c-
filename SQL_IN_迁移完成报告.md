# SQL IN 功能迁移完成

## 修改清单

### 新增文件
1. `src/DevToolbox.Tools.Text/SqlInTool.cs` - 工具定义
2. `src/DevToolbox.Tools.Text/Views/SqlInView.xaml` - 界面布局
3. `src/DevToolbox.Tools.Text/Views/SqlInView.xaml.cs` - 业务逻辑

### 修改文件
1. `src/DevToolbox.App/App.xaml.cs` - 添加工具注册
   - 第56行：`services.AddTransient<ITool, SqlInTool>();`

2. `src/DevToolbox.App/MainWindow.xaml.cs` - 添加视图路由
   - 第44行：`"sql-in" => new SqlInView(),`

## 功能说明

### 界面布局
- 采用左右分栏设计，与 JSON 格式化工具保持一致
- 左侧：输入列表数据（每行一个值）
- 右侧：输出 SQL IN 语句

### 核心功能
1. **多行文本转换**：将每行文本转换为 SQL IN 语句中的一个值
2. **引号类型选择**：
   - 单引号 `'` （默认，适用于字符串）
   - 双引号 `"` （适用于某些数据库）
   - 无引号（适用于数字或标识符）
3. **自动处理**：
   - 过滤空行
   - 引号转义（值中包含引号时自动双写）
4. **实时转换**：切换引号类型时自动重新转换

### 快捷键
- **F4**：转换
- **Ctrl+Shift+C**：复制输出
- **Ctrl+滚轮**：调整字体大小

### 示例

**输入**（左侧）：
```
apple
banana
orange
grape
```

**输出**（右侧，单引号模式）：
```sql
('apple', 'banana', 'orange', 'grape')
```

**完整 SQL 使用示例**：
```sql
SELECT * FROM products WHERE name IN ('apple', 'banana', 'orange', 'grape');
```

## 技术实现

- 使用 AvalonEdit 编辑器组件
- 右侧输出区域支持 SQL 语法高亮
- 字体大小持久化存储（与其他工具共享配置）
- 遵循项目设计规范（颜色、按钮样式等）

## 测试建议

1. 测试不同引号类型的转换
2. 测试包含特殊字符（如引号）的值
3. 测试空行处理
4. 测试大量数据的性能
5. 测试快捷键功能
