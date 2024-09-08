#!/bin/bash

# Nombre del namespace donde se desplegarán Loki, Promtail y Grafana
NAMESPACE="monitoring"

# Añadir los repositorios de Helm para Grafana y actualizar
helm repo add grafana https://grafana.github.io/helm-charts
helm repo update

# Crear un namespace para los componentes de monitoreo si no existe
kubectl create namespace $NAMESPACE

# Instalar Loki y Promtail en el namespace especificado
helm install loki grafana/loki-stack --namespace $NAMESPACE

# Verificar que los pods de Loki, Promtail y Grafana estén corriendo
kubectl get pods -n $NAMESPACE

# Verificar que los servicios de Loki y Grafana estén expuestos
kubectl get svc -n $NAMESPACE

# Mostrar un mensaje de éxito
echo "Loki, Promtail y Grafana han sido instalados en el namespace '$NAMESPACE'."

# Opcional: Mostrar las instrucciones para acceder a Grafana
GRAFANA_POD=$(kubectl get pods -n $NAMESPACE -l "app.kubernetes.io/name=grafana" -o jsonpath="{.items[0].metadata.name}")
kubectl port-forward -n $NAMESPACE $GRAFANA_POD 3000:3000 &

echo "Puedes acceder a Grafana en http://localhost:3000"
echo "Las credenciales por defecto son admin/admin"
