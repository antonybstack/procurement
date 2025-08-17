ssh into linux server (digital ocean)

```bash
ssh -i ~/.ssh/ssh-oracle.key root@24.199.78.31
```

```bash
cd /opt/procurement
```

### optional disk cleanup
```bash
docker system prune -a -f --volumes
docker volume rm procurement_postgres_data
```

### stop and remove all containers
```bash
docker stop $(docker ps -a -q)
docker rm $(docker ps -a -q)
docker rmi $(docker images -q)
```

### list containers

```bash
docker ps -a
```

### pull latest code

```bash
git fetch --all
git pull
```

### build database
``` bash
./start-db.sh
```

### build and start api
``` bash
./start-api.sh
```

### build and start frontend
``` bash
./start-frontend.sh
```

### check logs
``` bash
docker logs procurement_api
```
