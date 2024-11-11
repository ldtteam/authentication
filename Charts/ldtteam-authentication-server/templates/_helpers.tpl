{{/*
Expand the name of the chart.
*/}}
{{- define "ldtteam-authentication-server.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "ldtteam-authentication-server.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "ldtteam-authentication-server.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "ldtteam-authentication-server.labels" -}}
helm.sh/chart: {{ include "ldtteam-authentication-server.chart" . }}
{{ include "ldtteam-authentication-server.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{/*
Selector labels
*/}}
{{- define "ldtteam-authentication-server.selectorLabels" -}}
app.kubernetes.io/name: {{ include "ldtteam-authentication-server.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Create the name of the service account to use
*/}}
{{- define "ldtteam-authentication-server.serviceAccountName" -}}
{{- if .Values.serviceAccount.create }}
{{- default (include "ldtteam-authentication-server.fullname" .) .Values.serviceAccount.name }}
{{- else }}
{{- default "default" .Values.serviceAccount.name }}
{{- end }}
{{- end }}

{{/*
Creates an environment variable for a deployment that is compatible with the Servers Environment Lookup.
Replaces all '.' in .Secret.Key with '__' to be compatible with ASP.Net Core and then Prefixes 'LDTTEAM_AUTH_' to
the key. This is to ensure that the secret is compatible with the Environment Lookup.
*/}}
{{- define "ldtteam-authentication-server.envFromSecret" -}}
- name: LDTTEAM_AUTH_{{ .Secret | replace "." "__" }}
  valueFrom:
    secretKeyRef:
      name: {{ include "ldtteam-authentication-server.fullname" . }}
      key: {{ .Secret }}
{{- end }}