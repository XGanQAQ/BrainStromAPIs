## 项目概述
这一系统应该是一个Web应用，允许用户创建、保存、查看和管理灵感文件。系统应具备用户注册与登录功能，灵感文件的创建、编辑、删除和查看功能。

## 部署说明
### 一、下载项目源码/可执行文件
Github代码仓库：
https://github.com/XGanQAQ/BrainStromAPIs
下载源码或者下载Release.
### 二、配置程序
1. 使用内存数据库（默认）
内存数据库无法持久化保存数据。
2．使用实体数据库
使用实体数据库需要进行简单的配置。
（1）首先修改项目配置文件。
找到appsetting.json这个配置文件
如果你需要使用实体数据库（SQL Server），请将IsUseMemoryDatabase改为false,
再给ConnectionString改成你数据库的连接字符串
（2）ORM映射到实体数据库（迁移数据库）
①在项目源代码文件中迁移
使用ORM为数据库创建表单，在项目文件下，运行如下指令
dotnet ef migrations add InitialCreate
dotnet ef database update
②手动创建对应表单
参考数据库设计在数据库中创建对应表单
③项目源文件生产sql语句
在开发环境中，使用 dotnet ef migrations script 生成迁移脚本。
dotnet ef migrations script -o migration.sql
这会生成一个包含所有迁移的 SQL 脚本文件（migration.sql），你可以将它手动执行到生产数据库中。
### 三、运行可执行文件
打开项目可执行文件的BrainStromAPIs.exe

### 四、使用浏览器访问
在浏览器输入你的服务器地址（注意你开放的端口号），即可访问到登录界面