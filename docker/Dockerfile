FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build_server
COPY ./ .
ARG GIT_REF
ARG GIT_REF_TYPE
ARG GIT_COMMIT_SHA
ARG PROJECT_URL
ARG REPO_URL
ARG REPO
ENV GIT_REF=$GIT_REF
ENV GIT_REF_TYPE=$GIT_REF_TYPE
ENV GIT_COMMIT_SHA=$GIT_COMMIT_SHA
ENV PROJECT_URL=$PROJECT_URL
ENV REPO_URL=$REPO_URL
ENV REPO=$REPO
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release --no-build -o /out ArkProjects.EHentai.MetricsCollector


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS app
WORKDIR /homes/app
ARG GIT_REF
ARG GIT_REF_TYPE
ARG GIT_COMMIT_SHA
ARG PROJECT_URL
ARG REPO_URL
LABEL image.git.ref=$GIT_REF
LABEL image.git.ref_type=$GIT_REF_TYPE
LABEL image.git.commit_sha=GIT_COMMIT_SHA
LABEL image.project_url=$PROJECT_URL
LABEL image.repo_url=$REPO_URL
COPY --from=build_server /out /homes/app
EXPOSE 8080
ENTRYPOINT ["dotnet", "ArkProjects.EHentai.MetricsCollector.dll"]
