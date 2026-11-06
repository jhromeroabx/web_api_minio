pipeline {
    agent { label 'docker-agent' }

    environment {
        GIT_REPO           = 'https://github.com/jhromeroabx/web_api_minio.git'
        GIT_BRANCH         = 'develope'
        //SONAR_PROJECT_KEY  = 'web-api-minio'
        LOCAL_REPO_PATH    = "/home/diego-epc/Documentos/PROYECTOS/web_api_minio"
        COMPOSE_PROJECT_NAME = 'web-api-minio'
        REGISTRY_IMAGE = 'ghcr.io/jhromeroabx/web_api_minio'
        ENV_BACKUP_PATH   = '/opt/jenkins/envs/dev/.env_backup_web_api_minio_dev'
        ASPNETCORE_ENVIRONMENT = 'Development'
        IMAGE_TAG = 'dev'
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
                                git remote set-url origin "$GIT_REPO"

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

        // stage SonarQube comentado
        // stage('Análisis SonarQube') {
        //     steps {
        //         withSonarQubeEnv('SonarQube Loasi') {
        //             dir("${env.LOCAL_REPO_PATH}") {
        //                                     sh '''
        //                                             set -e

        //                                             export DOTNET_TOOLS_DIR="$HOME/.dotnet/tools"
        //                                             export PATH="$PATH:$DOTNET_TOOLS_DIR"

        //                                             echo "🧹 Limpiando artefactos previos de pruebas y cobertura"
        //                                             rm -rf tests/web_api_users.Tests/TestResults

        //                                             echo "🔧 Instalando/actualizando dotnet-sonarscanner"
        //                                             ORIGINAL_DIR="$PWD"
        //                                             cd "$HOME"
        //                                             dotnet tool update dotnet-sonarscanner --tool-path "$DOTNET_TOOLS_DIR" || \
        //                                             dotnet tool install dotnet-sonarscanner --tool-path "$DOTNET_TOOLS_DIR"
        //                                             cd "$ORIGINAL_DIR"

        //                                             echo "📦 Restaurando dependencias .NET"
        //                                             dotnet restore web_api_users.sln

        //                                             SONAR_PROPS_BACKUP=""
        //                                             trap 'if [ -n "$SONAR_PROPS_BACKUP" ] && [ -f "$SONAR_PROPS_BACKUP" ]; then mv "$SONAR_PROPS_BACKUP" sonar-project.properties; fi' EXIT
        //                                             if [ -f sonar-project.properties ]; then
        //                                                 echo "📄 Desactivando temporalmente sonar-project.properties para SonarScanner for .NET"
        //                                                 SONAR_PROPS_BACKUP="sonar-project.properties.jenkins.bak"
        //                                                 mv sonar-project.properties "$SONAR_PROPS_BACKUP"
        //                                             fi

        //                                             echo "🚀 Iniciando análisis SonarQube"
        //                                             dotnet-sonarscanner begin \
        //                                                 /k:"${SONAR_PROJECT_KEY}" \
        //                                                 /d:sonar.host.url="$SONAR_HOST_URL" \
        //                                                 /d:sonar.token="$SONAR_AUTH_TOKEN" \
        //                                                 /d:sonar.exclusions="**/bin/**,**/obj/**" \
        //                                                 /d:sonar.coverage.exclusions="**/Properties/Resources.Designer.cs" \
        //                                                 /d:sonar.cs.opencover.reportsPaths="tests/web_api_users.Tests/TestResults/Coverage/coverage.opencover.xml" \
        //                                                 /d:sonar.cs.vstest.reportsPaths="tests/web_api_users.Tests/TestResults/*.trx"

        //                                             echo "🏗️ Compilando solución"
        //                                             dotnet build web_api_users.sln --no-restore

        //                                             echo "🧪 Ejecutando pruebas con cobertura OpenCover"
        //                                             dotnet test tests/web_api_users.Tests/web_api_users.Tests.csproj \
        //                                                 --no-build \
        //                                                 --logger "trx" \
        //                                                 --results-directory tests/web_api_users.Tests/TestResults \
        //                                                 /p:CollectCoverage=true

        //                                             test -s tests/web_api_users.Tests/TestResults/Coverage/coverage.opencover.xml

        //                                             echo "✅ Cerrando análisis SonarQube"
        //                                             dotnet-sonarscanner end /d:sonar.token="$SONAR_AUTH_TOKEN"

        //                                             if [ -n "$SONAR_PROPS_BACKUP" ] && [ -f "$SONAR_PROPS_BACKUP" ]; then
        //                                                 mv "$SONAR_PROPS_BACKUP" sonar-project.properties
        //                                             fi
        //                                     '''
        //             }
        //         }
        //     }
        // }
        
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