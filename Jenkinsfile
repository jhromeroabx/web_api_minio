pipeline { agent any

environment {
    GIT_REPO           = 'https://github.com/EL-OASIS/web_api_minio.git'
    GIT_BRANCH         = 'develope'
    SONAR_PROJECT_KEY  = 'miniowebapi'
    SONAR_HOST_URL     = 'https://sonarqubeloasi.share.zrok.io'
    LOCAL_REPO_PATH    = "/home/diego-epc/Documentos/PROYECTOS/web_api_minio"
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
                    withCredentials([usernamePassword(credentialsId: 'github-j', usernameVariable: 'GIT_USERNAME', passwordVariable: 'GIT_PASSWORD')]) {
                        sh """
                            git clone -b ${env.GIT_BRANCH} https://\$GIT_USERNAME:\$GIT_PASSWORD@github.com/EL-OASIS/web_api_minio.git ${repoPath}
                        """
                    }
                } else {
                    echo '📁 Repositorio válido detectado. Sincronizando cambios...'
                    withCredentials([usernamePassword(credentialsId: 'github-j', usernameVariable: 'GIT_USERNAME', passwordVariable: 'GIT_PASSWORD')]) {
                        dir(repoPath) {
                            sh """
                                echo "🔄 Limpiando repositorio..."
                                git reset --hard
                                git clean -fd --exclude=.env  # Corrige uso de --exclude

                                echo "📥 Sincronizando con remoto..."
                                git remote set-url origin https://\$GIT_USERNAME:\$GIT_PASSWORD@github.com/EL-OASIS/web_api_minio.git
                                git fetch --all --force --prune

                                echo "🔄 Actualizando rama ${env.GIT_BRANCH}..."
                                git checkout ${env.GIT_BRANCH} --force 2>/dev/null || git checkout -b ${env.GIT_BRANCH} origin/${env.GIT_BRANCH} --force
                                git pull origin ${env.GIT_BRANCH} --force

                                echo "✅ Sincronización completada:"
                                git status
                            """
                        }
                    }
                }
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

                        echo "🧼 Limpiando recursos docker-compose..."
                        docker-compose down || true

                        echo "🔨 Recompilando imagen desde cero..."
                        docker-compose build --no-cache

                        echo "🚀 Levantando contenedor actualizado..."
                        docker-compose up -d

                        echo "📋 Estado de contenedores:"
                        docker-compose ps
                    """
                }
            }
        }
    }
}

}

