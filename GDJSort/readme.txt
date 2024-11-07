1,运行生成最新的编译后的网站
npm run build
2，编译docker，用nginx，命名image为reactapp
docker build -t reactapp .
3，运行镜像，并映射本地端口
docker run --name some-nginx -d -p 8080:80 reactapp 

4，保存该image
docker save -o reactapp.tar reactapp
5，加载image
docker load -i reactapp.tar



python服务器构筑
1，增加from waitress import serve 
2，增加以下代码，来启动服务器
if __name__ == '__main__':
    serve(app, host='0.0.0.0', port=8000)
3，依赖中，增加waitress
4，构建
docker  docker build -t tuangouapi .
5,运行
docker run --name tuangouapi -d -p 8080:80 tuangouapi 

docker save -o tuangouapi.tar tuangouapi
docker load -i tuangouapi.tar


Dockerfile文件

# 使用官方的 Python 基础镜像
FROM python:3.12-slim

# 设置工作目录
WORKDIR /app

# 复制当前目录内容到工作目录
COPY . /app

# 安装依赖
RUN pip install --no-cache-dir -r requirements.txt

# 暴露应用程序运行的端口
EXPOSE 8000

# 运行 Flask 应用程序
CMD ["python", "app.py"]