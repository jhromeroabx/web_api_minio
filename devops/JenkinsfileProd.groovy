pipeline {
    agent { label 'server4you-vps' }

    environment {
        GIT_REPO           = 'https://github.com/jhromeroabx/web_api_minio.git'
        GIT_BRANCH         = 'main'
        SONAR_PROJECT_KEY  = 'miniowebapi'
        SONAR_HOST_URL     = 'https://sonarqubeloasi.share.zrok.io'
        LOCAL_REPO_PATH    = "/home/jhosep-epc/Documentos/PROYECTOS/web_api_minio"
        COMPOSE_PROJECT_NAME = 'web-api-minio'
        REGISTRY_IMAGE = 'ghcr.io/jhromeroabx/web_api_minio'
        ENV_BACKUP_PATH   = '/opt/jenkins/envs/prod/.env_backup_web_api_minio_prod'
        ASPNETCORE_ENVIRONMENT = 'Production'
        IMAGE_TAG = 'prod'
    }

    stages {
        stage('Verificar/Clonar repositorio') {
            steps {
                script {
                    if (!fileExists("${env.LOCAL_REPO_PATH}/.git")) {
                        echo '📁 Clonando repositorio...'
                        withCredentials([
                            usernamePassword(
                                credentialsId: 'github-token',
                                usernameVariable: 'GIT_USERNAME',
                                passwordVariable: 'GIT_PASSWORD'
                            )
                        ]) {
                            sh """
                                git clone -b ${env.GIT_BRANCH} https://\$GIT_USERNAME:\$GIT_PASSWORD@${env.GIT_REPO.replace('https://','')} ${env.LOCAL_REPO_PATH}
                            """
                        }
                    }
                    dir("${env.LOCAL_REPO_PATH}") {
                        withCredentials([
                            usernamePassword(
                                credentialsId: 'github-token',
                                usernameVariable: 'GIT_USERNAME',
                                passwordVariable: 'GIT_PASSWORD'
                            )
                        ]) {
                            sh '''
                                set -eu
                                echo "🔐 Verificando respaldo del .env..."
                                test -s "$ENV_BACKUP_PATH"

                                echo "📥 Actualizando repositorio..."

                                git remote set-url origin \
                                    "https://$GIT_USERNAME:$GIT_PASSWORD@${GIT_REPO#https://}"

                                git fetch origin "$GIT_BRANCH"

                                git checkout -B "$GIT_BRANCH" \
                                    "origin/$GIT_BRANCH"

                                echo "🔧 Restaurando variables de entorno..."

                                cp "$ENV_BACKUP_PATH" .env
                                chmod 600 .env

                                echo "✅ Repositorio actualizado"
                            '''
                        }
                    }
                }
            }
        }

        stage('Cleanup Docker Proyecto') {
            steps {
                dir("${env.LOCAL_REPO_PATH}") {
                    sh '''
                        set -eu
                        echo "🧹 Verificando recursos Docker del proyecto..."
                        docker-compose -p "$COMPOSE_PROJECT_NAME" ps -a

                        echo "🧹 Eliminando contenedores detenidos del proyecto..."
                        docker-compose -p "$COMPOSE_PROJECT_NAME" rm -f

                        echo "✅ Cleanup del proyecto completado"
                    '''
                }
            }
        }

        stage('Levantar App con Docker Compose') {
            steps {
                dir("${env.LOCAL_REPO_PATH}") {
                    withCredentials([
                        usernamePassword(
                            credentialsId: 'github-token',
                            usernameVariable: 'GIT_USERNAME',
                            passwordVariable: 'GIT_PASSWORD'
                        )
                    ]) {
                        sh '''
                            set -eu
                            export DOCKER_CONFIG="$(mktemp -d)"
                            trap 'rm -rf "$DOCKER_CONFIG"' EXIT

                            echo "🔐 Autenticando en GitHub Container Registry..."
                            echo "$GIT_PASSWORD" | docker login ghcr.io \
                                -u "$GIT_USERNAME" \
                                --password-stdin

                            echo "📦 Imagen a desplegar: $REGISTRY_IMAGE:$IMAGE_TAG"

                            echo "⬇️ Descargando imagen desde GHCR..."
                            docker-compose -p "$COMPOSE_PROJECT_NAME" pull

                            echo "🚀 Levantando API MinIO..."
                            docker-compose -p "$COMPOSE_PROJECT_NAME" up -d \
                                --no-build \
                                --remove-orphans

                            echo "📋 Estado de los contenedores..."
                            docker-compose -p "$COMPOSE_PROJECT_NAME" ps

                            echo "✅ Despliegue finalizado"
                        '''
                    }
                }
            }
        }
    }
}
