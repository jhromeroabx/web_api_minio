pipeline {
    agent { label 'docker-agent' }

environment {
    GIT_REPO           = 'https://github.com/jhromeroabx/web_api_minio.git'
    GIT_BRANCH         = 'develope'
    SONAR_PROJECT_KEY  = 'miniowebapi'
    SONAR_HOST_URL     = 'https://sonarqubeloasi.share.zrok.io'
    LOCAL_REPO_PATH    = "/home/diego-epc/Documentos/PROYECTOS/web_api_minio"
    COMPOSE_PROJECT_NAME = 'web-api-minio'
    ENV_BACKUP_PATH   = '/opt/jenkins/envs/dev/.env_backup_web_api_minio_dev'
    ASPNETCORE_ENVIRONMENT = 'Development'
}

stages {
    stage('Verificar/Clonar repositorio') {
        steps {
            script {
                def repoPath = env.LOCAL_REPO_PATH
                def isGitRepo = fileExists("${repoPath}/.git")

                if (!fileExists(repoPath) || !isGitRepo) {
                    echo '📁 No es un repositorio válido. Re-clonando desde cero...'
                    sh "rm -rf ${repoPath}"
                    withCredentials([usernamePassword(credentialsId: 'github-token', usernameVariable: 'GIT_USERNAME', passwordVariable: 'GIT_PASSWORD')]) {
                        sh """
                            git clone -b ${env.GIT_BRANCH} https://\$GIT_USERNAME:\$GIT_PASSWORD@${env.GIT_REPO.replace('https://','')} ${env.LOCAL_REPO_PATH}
                        """
                    }
                } else {
                    echo '📁 Repositorio válido detectado. Sincronizando cambios...'
                    withCredentials([usernamePassword(credentialsId: 'github-token', usernameVariable: 'GIT_USERNAME', passwordVariable: 'GIT_PASSWORD')]) {
                        dir(repoPath) {
                            sh """
                                echo "🔐 Verificando respaldo del .env..."
                                if [ ! -s "${env.ENV_BACKUP_PATH}" ]; then
                                    echo "❌ El respaldo no existe o está vacío: ${env.ENV_BACKUP_PATH}"
                                    exit 1
                                fi

                                echo "🔄 Limpiando repositorio..."
                                git reset --hard
                                git clean -fd --exclude=.env  # Corrige uso de --exclude

                                echo "📥 Sincronizando con remoto..."
                                git remote set-url origin https://\$GIT_USERNAME:\$GIT_PASSWORD@${env.GIT_REPO.replace('https://','')}
                                git fetch --all --force --prune

                                echo "🔄 Actualizando rama ${env.GIT_BRANCH}..."
                                git checkout ${env.GIT_BRANCH} --force 2>/dev/null || git checkout -b ${env.GIT_BRANCH} origin/${env.GIT_BRANCH} --force
                                git pull origin ${env.GIT_BRANCH} --force

                                    echo "🔧 Copiando .env de producción..."
                                    cp "${env.ENV_BACKUP_PATH}" .env
                                    chmod 600 .env

                                    echo "✅ Sincronización completada:"
                                    git status
                                    ls -la .env
                            """
                        }
                    }
                }
            }
        }
    }

    stage('Cleanup Docker Proyecto') {
        steps {
            dir("${env.LOCAL_REPO_PATH}") {
                sh '''
                    echo "🧹 Cleanup compose project: $COMPOSE_PROJECT_NAME"
                    docker-compose -p "$COMPOSE_PROJECT_NAME" down --rmi local -v --remove-orphans || true
                    echo "Cleanup proyecto listo"
                '''
            }
        }
    }

    stage('Levantar App con Docker Compose') {
        steps {
            script {
                dir("${env.LOCAL_REPO_PATH}") {
                    def containerName = "miniowebapi"

                    sh """
                        echo "🧹 Eliminando contenedor en uso si existe..."
                        docker rm -f ${containerName} || true

                        echo "🧼 Limpiando recursos docker-compose (solo del proyecto)..."
                        docker-compose -p "\$COMPOSE_PROJECT_NAME" down --remove-orphans || true

                        echo "🔨 Recompilando imagen desde cero..."
                        docker-compose -p "\$COMPOSE_PROJECT_NAME" build

                        echo "🚀 Levantando contenedor actualizado..."
                        docker-compose -p "\$COMPOSE_PROJECT_NAME" up -d

                        echo "📋 Estado de contenedores:"
                        docker-compose -p "\$COMPOSE_PROJECT_NAME" ps
                    """
                }
            }
        }
    }
}

}
