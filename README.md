# Installation

> Prerequisites: Install K8, Docker, Colima, Kind, Kubectl, and Helm.

1. Build the web application image using the Dockerfile located in the project's root directory.
2. Create the K8 cluster.
3. Load the web application image into the created cluster.
4. Load the `IdentityServer4Auth` application image into the created cluster.
5. Verify that the exposed port is 5000.
6. Apply the manifest from the `prod-pod.yaml` file to the cluster.
7. Grant execution permissions to the `install-monitoring.sh` file using `chmod +x` for installing the monitoring tools on the cluster (don't forget to verify that `kubectl` is connected to the cluster created in step 2 when running the script).

## Deployment Commands

### Rebuild the image

`docker build --no-cache -t reservationplatform-web:latest .`

### Recreate the cluster

`kind create cluster --name reservation-platform`
`kind delete cluster --name reservation-platform`

### Load image into Kubernetes

`kind load docker-image reservationplatform-web:latest --name reservation-platform`

### Apply or delete the manifest to the cluster

`kubectl apply -f prod-pod.yaml --context kind-reservation-platform`
`kubectl delete -f prod-pod.yaml --context kind-reservation-platform`

### Get the pods

`kubectl get pods --context kind-reservation-platform`

### Delete images

`docker rmi -f 19ded3ca5d58 635238e4ad1e ceedf3e5c7f3`

### Check logs of a pod

`kubectl logs <pod-name> -c web --context kind-<cluster-name>`
`kubectl describe pod <pod-name> --context kind-<cluster-name>`

### Verify Zookeeper connection

`kubectl exec -it <zookeeper-pod-name> -- nc -zv localhost 2181`

### Delete a pod

`kubectl delete <pod-name>`

### Check which cluster you are connected to

`kubectl config current-context`
